#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "datasets" / "asr" / "manifest.jsonl"

sys.path.insert(0, str((ROOT / "scripts").resolve()))
from asr_benchmark_metrics import compact_alnum, load_jsonl  # noqa: E402


def animal_tags(row: dict[str, Any]) -> list[str]:
    result = []
    seen = set()
    for entity in row.get("entities", []):
        if entity.get("kind") == "animal_tag" and entity.get("text"):
            compact = compact_alnum(entity["text"])
            if compact and compact not in seen:
                seen.add(compact)
                result.append(entity["text"])
    return result


def expected_domain_entities(row: dict[str, Any]) -> set[str]:
    values = set()
    for entity in row.get("entities", []):
        text = entity.get("text")
        if text:
            values.add(text)
    return values


def tags_present(text: str, tags: list[str]) -> set[str]:
    compact_text = compact_alnum(text)
    return {tag for tag in tags if compact_alnum(tag) in compact_text}


def main() -> int:
    parser = argparse.ArgumentParser(description="Evaluate ASR domain correction false positive rate.")
    parser.add_argument("corrected_predictions", type=Path)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--raw-field", default="prediction")
    parser.add_argument("--corrected-field", default="domain_corrected_prediction")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()

    manifest = {row["id"]: row for row in load_jsonl(args.manifest)}
    rows = load_jsonl(args.corrected_predictions)

    total_rows = 0
    changed_rows = 0
    correction_operations = 0
    false_positive_rows = []
    fixed_tag_rows = []
    worsened_tag_rows = []

    known_tags = sorted({tag for row in manifest.values() for tag in animal_tags(row)})

    for row in rows:
        item = manifest.get(row.get("id"))
        if not item:
            continue
        total_rows += 1
        raw_text = str(row.get(args.raw_field, ""))
        corrected_text = str(row.get(args.corrected_field, ""))
        corrections = row.get("domain_corrections") or []
        if raw_text != corrected_text:
            changed_rows += 1
        correction_operations += len(corrections)

        golden_tags = animal_tags(item)
        raw_present = tags_present(raw_text, golden_tags)
        corrected_present = tags_present(corrected_text, golden_tags)

        if corrected_present > raw_present:
            fixed_tag_rows.append(row["id"])
        if not corrected_present.issuperset(raw_present):
            worsened_tag_rows.append(row["id"])

        raw_known = tags_present(raw_text, known_tags)
        corrected_known = tags_present(corrected_text, known_tags)
        expected_entities = expected_domain_entities(item)
        introduced_non_golden = corrected_known - raw_known - expected_entities
        if introduced_non_golden or not corrected_present.issuperset(raw_present):
            false_positive_rows.append({
                "id": row["id"],
                "introduced_non_golden_tags": sorted(introduced_non_golden),
                "raw_present_golden_tags": sorted(raw_present),
                "corrected_present_golden_tags": sorted(corrected_present),
                "raw": raw_text,
                "corrected": corrected_text,
                "corrections": corrections,
            })

    result = {
        "items": total_rows,
        "changed_rows": changed_rows,
        "correction_operations": correction_operations,
        "fixed_tag_rows": fixed_tag_rows,
        "worsened_tag_rows": worsened_tag_rows,
        "false_positive_rows": false_positive_rows,
        "false_positive_rate_by_changed_row": len(false_positive_rows) / changed_rows if changed_rows else 0.0,
        "false_positive_rate_by_operation": len(false_positive_rows) / correction_operations if correction_operations else 0.0,
    }

    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
