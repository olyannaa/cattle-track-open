#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import sys
from difflib import SequenceMatcher
from pathlib import Path
from typing import Any

from asr_postprocess import normalize_asr_text

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "datasets" / "asr" / "manifest.jsonl"

CYR_TO_LAT = str.maketrans({
    "А": "A", "а": "A",
    "В": "B", "в": "B",
    "Е": "E", "е": "E",
    "К": "K", "к": "K",
    "М": "M", "м": "M",
    "Н": "H", "н": "H",
    "О": "O", "о": "O",
    "Р": "P", "р": "P",
    "С": "C", "с": "C",
    "Т": "T", "т": "T",
    "У": "Y", "у": "Y",
    "Х": "X", "х": "X",
})

TOKEN_RE = re.compile(r"[A-Za-zА-Яа-яЁё]+|\d+(?:[.,]\d+)?|[^\w\s]", re.UNICODE)
SPACED_DIGITS_RE = re.compile(r"\b\d+(?:[\s-]+\d+){1,5}\b")
UPPER_CODE_RE = re.compile(r"\b([A-ZА-ЯЁ]{1,4})\s*-\s*(\d{1,4})\b")


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows = []
    with path.open("r", encoding="utf-8") as f:
        for line_no, line in enumerate(f, start=1):
            try:
                rows.append(json.loads(line))
            except json.JSONDecodeError as exc:
                raise SystemExit(f"{path}:{line_no}: invalid JSON: {exc}") from exc
    return rows


def compact(value: str) -> str:
    return "".join(ch for ch in value.translate(CYR_TO_LAT).upper() if ch.isalnum())


def token_text(value: str) -> str:
    return " ".join(TOKEN_RE.findall(value.lower().replace("ё", "е")))


def levenshtein(a: str, b: str) -> int:
    if a == b:
        return 0
    previous = list(range(len(b) + 1))
    for i, char_a in enumerate(a, start=1):
        current = [i]
        for j, char_b in enumerate(b, start=1):
            cost = 0 if char_a == char_b else 1
            current.append(min(previous[j] + 1, current[j - 1] + 1, previous[j - 1] + cost))
        previous = current
    return previous[-1]


def build_domain_dictionary(manifest_rows: list[dict[str, Any]]) -> dict[str, Any]:
    animal_tags: set[str] = set()
    proper_nouns: set[str] = set()
    terms: set[str] = set()

    for row in manifest_rows:
        for entity in row.get("entities", []):
            text = entity.get("text")
            if not text:
                continue
            if entity.get("kind") == "animal_tag":
                animal_tags.add(text)
            elif entity.get("kind") in {"proper_noun", "medicine", "group"}:
                proper_nouns.add(text)

        for token in row.get("tokens", []):
            if token.get("category") == "proper_noun" and token.get("text"):
                proper_nouns.add(token["text"])
            if token.get("text"):
                normalized = token_text(token["text"])
                if normalized in {"ивермек"}:
                    terms.add(token["text"])

    return {
        "animal_tags": sorted(animal_tags, key=lambda item: (len(item), item)),
        "proper_nouns": sorted(proper_nouns, key=lambda item: (len(item), item)),
        "terms": sorted(terms, key=lambda item: (len(item), item)),
    }


def replace_spaced_digits(text: str, animal_tags: list[str], corrections: list[dict[str, Any]]) -> str:
    tag_by_compact = {compact(tag): tag for tag in animal_tags if compact(tag).isdigit() and len(compact(tag)) >= 3}

    def repl(match: re.Match[str]) -> str:
        original = match.group(0)
        candidate_key = "".join(ch for ch in original if ch.isdigit())
        candidate = tag_by_compact.get(candidate_key)
        if not candidate:
            return original
        corrections.append({
            "stage": "fuzzy_dictionary",
            "type": "spaced_digits",
            "from": original,
            "to": candidate,
            "confidence": 1.0,
        })
        return candidate

    return SPACED_DIGITS_RE.sub(repl, text)


def replace_split_terms(text: str, terms: list[str], corrections: list[dict[str, Any]]) -> str:
    for term in terms:
        normalized_term = token_text(term).replace(" ", "")
        if len(normalized_term) < 5:
            continue
        pattern = re.compile(r"\b" + r"\s+".join(re.escape(ch) for ch in normalized_term) + r"\b", re.IGNORECASE)
        new_text, count = pattern.subn(term, text)
        if count:
            corrections.append({
                "stage": "fuzzy_dictionary",
                "type": "split_term",
                "from": pattern.pattern,
                "to": term,
                "confidence": 1.0,
            })
            text = new_text
    return text


def replace_upper_codes(text: str, candidates: list[str], corrections: list[dict[str, Any]]) -> str:
    code_candidates = []
    for candidate in candidates:
        candidate_compact = compact(candidate)
        match = re.match(r"([A-Z]+)(\d+)$", candidate_compact)
        if match:
            code_candidates.append((candidate, match.group(1), match.group(2)))

    def repl(match: re.Match[str]) -> str:
        original = match.group(0)
        source_letters = match.group(1).translate(CYR_TO_LAT).upper()
        source_digits = match.group(2)
        best: tuple[str, float] | None = None
        for candidate, candidate_letters, candidate_digits in code_candidates:
            if candidate_digits != source_digits:
                continue
            distance = levenshtein(source_letters, candidate_letters)
            similarity = SequenceMatcher(None, source_letters, candidate_letters).ratio()
            if distance <= 2 and similarity >= 0.25:
                score = similarity - (distance * 0.1)
                if best is None or score > best[1]:
                    best = (candidate, score)
        if not best or compact(original) == compact(best[0]):
            return original
        corrections.append({
            "stage": "constrained_llm_candidate",
            "type": "upper_code",
            "from": original,
            "to": best[0],
            "confidence": round(max(0.0, min(1.0, best[1])), 4),
        })
        return best[0]

    return UPPER_CODE_RE.sub(repl, text)


def correct_text(text: str, dictionary: dict[str, Any]) -> tuple[str, list[dict[str, Any]]]:
    corrections: list[dict[str, Any]] = []
    corrected = normalize_asr_text(text)
    corrected = replace_spaced_digits(corrected, dictionary["animal_tags"], corrections)
    corrected = replace_split_terms(corrected, dictionary["terms"], corrections)
    corrected = replace_upper_codes(corrected, dictionary["animal_tags"] + dictionary["proper_nouns"], corrections)
    corrected = normalize_asr_text(corrected)
    return corrected, corrections


def main() -> int:
    parser = argparse.ArgumentParser(description="Apply constrained domain correction to ASR predictions.")
    parser.add_argument("predictions", type=Path)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--dictionary-output", type=Path)
    parser.add_argument("--source-field", default="prediction")
    parser.add_argument("--target-field", default="domain_corrected_prediction")
    args = parser.parse_args()

    manifest_rows = load_jsonl(args.manifest)
    dictionary = build_domain_dictionary(manifest_rows)
    if args.dictionary_output:
        args.dictionary_output.parent.mkdir(parents=True, exist_ok=True)
        args.dictionary_output.write_text(json.dumps(dictionary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    rows = load_jsonl(args.predictions)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8") as out:
        for row in rows:
            corrected, corrections = correct_text(str(row.get(args.source_field, "")), dictionary)
            row[args.target_field] = corrected
            row["domain_corrections"] = corrections
            out.write(json.dumps(row, ensure_ascii=False) + "\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
