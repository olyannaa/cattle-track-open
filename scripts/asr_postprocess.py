#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

UNITS = {
    "ноль": 0,
    "один": 1,
    "одна": 1,
    "одно": 1,
    "два": 2,
    "две": 2,
    "три": 3,
    "четыре": 4,
    "пять": 5,
    "шесть": 6,
    "семь": 7,
    "восемь": 8,
    "девять": 9,
}

TEENS = {
    "десять": 10,
    "одиннадцать": 11,
    "двенадцать": 12,
    "тринадцать": 13,
    "четырнадцать": 14,
    "пятнадцать": 15,
    "шестнадцать": 16,
    "семнадцать": 17,
    "восемнадцать": 18,
    "девятнадцать": 19,
}

TENS = {
    "двадцать": 20,
    "тридцать": 30,
    "сорок": 40,
    "пятьдесят": 50,
    "шестьдесят": 60,
    "семьдесят": 70,
    "восемьдесят": 80,
    "девяносто": 90,
}

HUNDREDS = {
    "сто": 100,
    "двести": 200,
    "триста": 300,
    "четыреста": 400,
    "пятьсот": 500,
    "шестьсот": 600,
    "семьсот": 700,
    "восемьсот": 800,
    "девятьсот": 900,
}

THOUSANDS = {"тысяча", "тысячи", "тысяч"}
NUMBER_WORDS = set(UNITS) | set(TEENS) | set(TENS) | set(HUNDREDS) | THOUSANDS

CYR_TO_LAT_CODE = str.maketrans({
    "А": "A",
    "а": "A",
    "В": "B",
    "в": "B",
    "Е": "E",
    "е": "E",
    "К": "K",
    "к": "K",
    "М": "M",
    "м": "M",
    "Н": "H",
    "н": "H",
    "О": "O",
    "о": "O",
    "Р": "P",
    "р": "P",
    "С": "C",
    "с": "C",
    "Т": "T",
    "т": "T",
    "У": "Y",
    "у": "Y",
    "Х": "X",
    "х": "X",
})

TOKEN_RE = re.compile(r"[A-Za-zА-Яа-яЁё]+|\d+(?:[.,]\d+)?|[^\w\s]", re.UNICODE)
CODE_WITH_SPACES_RE = re.compile(r"\b([AaEeАаЕе])\s*[- ]\s*(\d{1,4})\b")
CODE_COMPACT_RE = re.compile(r"\b([AaEeАаЕе])[- ]?(\d{1,4})\b")
MULTISPACE_RE = re.compile(r"\s+")


def parse_number_words(words: list[str], start: int) -> tuple[int | None, int]:
    total = 0
    current = 0
    index = start
    consumed = 0

    while index < len(words):
        word = words[index].lower().replace("ё", "е")
        if word in HUNDREDS:
            current += HUNDREDS[word]
        elif word in TENS:
            current += TENS[word]
        elif word in TEENS:
            current += TEENS[word]
        elif word in UNITS:
            current += UNITS[word]
        elif word in THOUSANDS:
            total += (current or 1) * 1000
            current = 0
        else:
            break
        index += 1
        consumed += 1

    if consumed == 0:
        return None, 0
    return total + current, consumed


def normalize_code_token(token: str) -> str:
    if not any(char.isdigit() for char in token):
        return token
    normalized = token.translate(CYR_TO_LAT_CODE)
    normalized = normalized.replace(",", ".")
    normalized = re.sub(r"^([A-Z])[- ]?(\d+)$", r"\1-\2", normalized)
    return normalized


def normalize_number_words(text: str) -> str:
    tokens = TOKEN_RE.findall(text)
    result: list[str] = []
    index = 0

    while index < len(tokens):
        token = tokens[index]
        normalized = token.lower().replace("ё", "е")
        if normalized in NUMBER_WORDS:
            words = []
            scan = index
            while scan < len(tokens) and tokens[scan].lower().replace("ё", "е") in NUMBER_WORDS:
                words.append(tokens[scan])
                scan += 1
            value, consumed = parse_number_words(words, 0)
            if value is not None and consumed:
                result.append(str(value))
                index += consumed
                continue
        result.append(token)
        index += 1

    return join_tokens(result)


def join_tokens(tokens: list[str]) -> str:
    text = " ".join(tokens)
    text = re.sub(r"\s+([,.!?;:])", r"\1", text)
    text = re.sub(r"([([{])\s+", r"\1", text)
    text = re.sub(r"\s+([)\]}])", r"\1", text)
    return text


def normalize_asr_text(text: str) -> str:
    text = text.strip().replace("ё", "е")
    text = text.replace("–", "-").replace("—", "-").replace("−", "-")
    text = CODE_WITH_SPACES_RE.sub(lambda match: f"{match.group(1).translate(CYR_TO_LAT_CODE).upper()}-{match.group(2)}", text)
    text = normalize_number_words(text)

    raw_tokens = TOKEN_RE.findall(text)
    tokens = [normalize_code_token(token) for token in raw_tokens]
    text = join_tokens(tokens)
    text = re.sub(r"\b([AaEe])\s*-\s*(\d{1,4})\b", r"\1-\2", text)
    text = CODE_COMPACT_RE.sub(lambda match: f"{match.group(1).translate(CYR_TO_LAT_CODE).upper()}-{match.group(2)}", text)
    text = MULTISPACE_RE.sub(" ", text).strip()
    return text


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows = []
    with path.open("r", encoding="utf-8") as f:
        for line_no, line in enumerate(f, start=1):
            try:
                rows.append(json.loads(line))
            except json.JSONDecodeError as exc:
                raise SystemExit(f"{path}:{line_no}: invalid JSON: {exc}") from exc
    return rows


def main() -> int:
    parser = argparse.ArgumentParser(description="Normalize ASR predictions for CattleTrack entity resolution.")
    parser.add_argument("predictions", type=Path, help="Input JSONL with a prediction field.")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--field", default="prediction")
    parser.add_argument("--target-field", default="normalized_prediction")
    args = parser.parse_args()

    rows = load_jsonl(args.predictions)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8") as out:
        for row in rows:
            source = str(row.get(args.field, ""))
            row[args.target_field] = normalize_asr_text(source)
            out.write(json.dumps(row, ensure_ascii=False) + "\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
