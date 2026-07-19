#!/usr/bin/env python3
import json
import re
from collections import Counter
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
TOOL_CALLING_DIR = ROOT / "datasets" / "tool_calling"
ASR_DIR = ROOT / "datasets" / "asr"

TARGET_BY_STRATUM = {
    "single-read": 14,
    "multi-hop-read": 10,
    "single-write": 18,
    "batch-write": 14,
    "adversarial-ambiguous": 8,
    "no-tool": 8,
}

VOICE_PROFILES = [
    {"id": "voice_f_01", "gender": "female", "age_band": "adult", "style": "neutral"},
    {"id": "voice_m_01", "gender": "male", "age_band": "adult", "style": "neutral"},
    {"id": "voice_f_02", "gender": "female", "age_band": "adult", "style": "fast"},
    {"id": "voice_m_02", "gender": "male", "age_band": "adult", "style": "low"},
]

NOISE_PROFILES = [
    {"id": "quiet", "description": "тихая запись без фонового шума", "snr_db": None},
    {"id": "farm_light", "description": "легкий фермерский фон", "snr_db": 20},
    {"id": "farm_medium", "description": "средний фермерский фон", "snr_db": 12},
]

NUMBER_WORDS = {
    "ноль", "один", "одна", "два", "две", "три", "четыре", "пять", "шесть",
    "семь", "восемь", "девять", "десять", "двадцать", "тридцать", "сорок",
    "пятьдесят", "шестьдесят", "семьдесят", "восемьдесят", "девяносто",
    "сто", "двести", "триста", "четыреста", "пятьсот", "шестьсот",
    "семьсот", "восемьсот", "девятьсот", "тысяча", "тысячи",
}

PROPER_NOUNS = {
    "A-17", "A17", "B12", "E-55", "E-77", "E-99", "E-9", "E-10", "RFID",
    "Иванов", "Бурый", "Ивермек", "Бруцел", "Зорька", "Молодняк",
    "Карантин", "Основное", "Производители", "Сухостой",
}

DATE_WORDS = {
    "сегодня", "вчера", "завтра", "июля", "июнь", "июне", "неделю", "месяц",
}

TOKEN_RE = re.compile(r"[A-Za-zА-Яа-яЁё]+(?:-[A-Za-zА-Яа-яЁё0-9]+)?|\d+(?:[.,]\d+)?")


def read_tool_calling_rows() -> list[dict[str, Any]]:
    rows = []
    for path in sorted(TOOL_CALLING_DIR.glob("*.jsonl")):
        with path.open("r", encoding="utf-8") as f:
            for line in f:
                rows.append(json.loads(line))
    return rows


def classify_token(token: str) -> str:
    normalized = token.strip(".,!?;:").replace(",", ".")
    lower = normalized.lower()
    if re.fullmatch(r"\d+(?:\.\d+)?", normalized):
        return "number"
    if lower in NUMBER_WORDS:
        return "number"
    if normalized in PROPER_NOUNS:
        return "proper_noun"
    if re.search(r"[A-Za-z]", normalized) and re.search(r"\d", normalized):
        return "proper_noun"
    return "other"


def infer_entities(row: dict[str, Any], tokens: list[dict[str, Any]]) -> list[dict[str, Any]]:
    entities = []
    seen = set()

    for token in tokens:
        text = token["text"]
        normalized = text.replace(",", ".")
        lower = text.lower()
        if re.fullmatch(r"\d+(?:\.\d+)?", normalized) or lower in NUMBER_WORDS:
            kind = "number"
        elif text in PROPER_NOUNS:
            kind = "proper_noun"
        elif lower in DATE_WORDS:
            kind = "date"
        else:
            continue
        key = (kind, text, token["index"])
        if key not in seen:
            entities.append({"kind": kind, "text": text, "token_indices": [token["index"]]})
            seen.add(key)

    for call in row["golden"]["tool_calls"]:
        args = call.get("arguments", {})
        for key in ("tag",):
            if args.get(key):
                entities.append({"kind": "animal_tag", "text": str(args[key]), "source": "tool_args"})
        for item in args.get("items", []):
            for tag_key in ("tag",):
                if item.get(tag_key):
                    entities.append({"kind": "animal_tag", "text": str(item[tag_key]), "source": "tool_args"})
            for tag_key in ("cow_tags", "bull_tags"):
                for tag in item.get(tag_key, []):
                    entities.append({"kind": "animal_tag", "text": str(tag), "source": "tool_args"})
            if item.get("medicine"):
                entities.append({"kind": "medicine", "text": str(item["medicine"]), "source": "tool_args"})
            if item.get("date"):
                entities.append({"kind": "date", "text": str(item["date"]), "source": "tool_args"})
            if item.get("new_group_id"):
                entities.append({"kind": "group", "text": str(item["new_group_id"]), "source": "tool_args"})

    return entities


def tokenize(text: str) -> list[dict[str, Any]]:
    tokens = []
    for i, match in enumerate(TOKEN_RE.finditer(text)):
        token = match.group(0)
        tokens.append({
            "index": i,
            "text": token,
            "category": classify_token(token),
            "start_char": match.start(),
            "end_char": match.end(),
        })
    return tokens


def choose_rows(rows: list[dict[str, Any]]) -> list[dict[str, Any]]:
    selected = []
    by_stratum = {key: [] for key in TARGET_BY_STRATUM}
    for row in rows:
        if row["stratum"] in by_stratum:
            by_stratum[row["stratum"]].append(row)

    for stratum, count in TARGET_BY_STRATUM.items():
        candidates = by_stratum[stratum]
        if len(candidates) < count:
            raise RuntimeError(f"Not enough rows for {stratum}: need {count}, got {len(candidates)}")
        selected.extend(candidates[:count])
    return selected


def main() -> None:
    rows = choose_rows(read_tool_calling_rows())
    ASR_DIR.mkdir(parents=True, exist_ok=True)
    (ASR_DIR / "audio").mkdir(exist_ok=True)

    manifest = []
    for i, row in enumerate(rows, start=1):
        tokens = tokenize(row["utterance"])
        voice = VOICE_PROFILES[(i - 1) % len(VOICE_PROFILES)]
        noise = NOISE_PROFILES[(i - 1) % len(NOISE_PROFILES)]
        asr_id = f"ctai-asr-{i:03d}"
        item = {
            "id": asr_id,
            "source_id": row["id"],
            "split": row["split"],
            "source_stratum": row["stratum"],
            "language": "ru",
            "golden_transcript": row["utterance"],
            "normalized_transcript": row["utterance"].lower(),
            "audio_path": f"datasets/asr/audio/{asr_id}.wav",
            "voice_profile": voice,
            "noise_profile": noise,
            "tokens": tokens,
            "entities": infer_entities(row, tokens),
            "review": {"status": "pending", "notes": ""},
        }
        manifest.append(item)

    with (ASR_DIR / "manifest.jsonl").open("w", encoding="utf-8") as f:
        for item in manifest:
            f.write(json.dumps(item, ensure_ascii=False, sort_keys=True) + "\n")

    summary = {
        "total": len(manifest),
        "by_source_stratum": dict(sorted(Counter(item["source_stratum"] for item in manifest).items())),
        "by_noise_profile": dict(sorted(Counter(item["noise_profile"]["id"] for item in manifest).items())),
        "by_voice_profile": dict(sorted(Counter(item["voice_profile"]["id"] for item in manifest).items())),
    }
    (ASR_DIR / "summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
