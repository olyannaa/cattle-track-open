#!/usr/bin/env python3
import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "datasets" / "asr" / "manifest.jsonl"

TOKEN_RE = re.compile(r"[A-Za-zА-Яа-яЁё]+(?:-[A-Za-zА-Яа-яЁё0-9]+)?|\d+(?:[.,]\d+)?")


def normalize(text: str) -> str:
    text = text.lower().replace("ё", "е").replace(",", ".")
    return " ".join(TOKEN_RE.findall(text))


def tokens(text: str) -> list[str]:
    normalized = normalize(text)
    return normalized.split() if normalized else []


def levenshtein(ref: list[str], hyp: list[str]) -> int:
    previous = list(range(len(hyp) + 1))
    for i, ref_token in enumerate(ref, start=1):
        current = [i]
        for j, hyp_token in enumerate(hyp, start=1):
            cost = 0 if ref_token == hyp_token else 1
            current.append(min(
                previous[j] + 1,
                current[j - 1] + 1,
                previous[j - 1] + cost,
            ))
        previous = current
    return previous[-1]


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows = []
    with path.open("r", encoding="utf-8") as f:
        for line_no, line in enumerate(f, start=1):
            try:
                rows.append(json.loads(line))
            except json.JSONDecodeError as exc:
                raise SystemExit(f"{path}:{line_no}: invalid JSON: {exc}") from exc
    return rows


def entity_reference_tokens(row: dict[str, Any]) -> list[str]:
    values = []
    for token in row.get("tokens", []):
        if token.get("category") in {"number", "proper_noun"}:
            values.extend(tokens(token["text"]))
    return values


def animal_tag_values(row: dict[str, Any]) -> list[str]:
    values = []
    golden_tokens = set(tokens(row["golden_transcript"]))
    for entity in row.get("entities", []):
        if entity.get("kind") == "animal_tag":
            entity_tokens = tokens(entity["text"])
            if entity_tokens and all(token in golden_tokens for token in entity_tokens):
                values.extend(entity_tokens)
    return sorted(set(values))


def main() -> int:
    parser = argparse.ArgumentParser(description="Evaluate ASR predictions for Cattle Track ASR dataset.")
    parser.add_argument("predictions", type=Path, help="JSONL with fields: id, prediction")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    args = parser.parse_args()

    manifest = {row["id"]: row for row in load_jsonl(args.manifest)}
    predictions = load_jsonl(args.predictions)

    total_ref = total_err = 0
    entity_ref = entity_err = 0
    tag_total = tag_exact = 0
    exact_total = exact_match = 0
    missing = []

    for pred in predictions:
        row = manifest.get(pred.get("id"))
        if not row:
            missing.append(pred.get("id"))
            continue

        ref_tokens = tokens(row["golden_transcript"])
        hyp_tokens = tokens(pred.get("prediction", ""))
        total_err += levenshtein(ref_tokens, hyp_tokens)
        total_ref += len(ref_tokens)

        ref_entity_tokens = entity_reference_tokens(row)
        hyp_joined = set(hyp_tokens)
        entity_err += sum(1 for token in ref_entity_tokens if normalize(token) not in hyp_joined)
        entity_ref += len(ref_entity_tokens)

        tags = animal_tag_values(row)
        if tags:
            tag_total += 1
            if all(tag in hyp_joined for tag in tags):
                tag_exact += 1

        exact_total += 1
        if normalize(row["golden_transcript"]) == normalize(pred.get("prediction", "")):
            exact_match += 1

    result = {
        "items_evaluated": exact_total,
        "missing_prediction_ids": missing,
        "wer": total_err / total_ref if total_ref else None,
        "entity_token_error_rate": entity_err / entity_ref if entity_ref else None,
        "animal_tag_exact_match": tag_exact / tag_total if tag_total else None,
        "utterance_exact_match": exact_match / exact_total if exact_total else None,
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 1 if missing else 0


if __name__ == "__main__":
    sys.exit(main())
