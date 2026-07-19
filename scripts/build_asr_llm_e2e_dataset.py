#!/usr/bin/env python3
"""Build an LLM benchmark dataset from ASR predictions and approved source rows."""

from __future__ import annotations

import argparse
import glob
import json
from pathlib import Path
from typing import Any


def read_jsonl(path: Path) -> list[dict[str, Any]]:
    with path.open(encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", required=True, type=Path)
    parser.add_argument("--predictions", required=True, type=Path)
    parser.add_argument("--source", action="append", default=["datasets/tool_calling/*.jsonl"])
    parser.add_argument("--prediction-field", default="normalized_prediction")
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--control-output", type=Path)
    args = parser.parse_args()

    source_rows = {
        row["id"]: row
        for pattern in args.source
        for filename in sorted(glob.glob(pattern))
        for row in read_jsonl(Path(filename))
    }
    predictions = {row["id"]: row for row in read_jsonl(args.predictions)}
    output_rows = []
    for item in read_jsonl(args.manifest):
        source = source_rows[item["source_id"]]
        prediction = predictions[item["id"]]
        row = json.loads(json.dumps(source, ensure_ascii=False))
        row.update({
            "id": f"e2e-{item['id']}",
            "source_id": item["source_id"],
            "asr_id": item["id"],
            "original_utterance": source["utterance"],
            "utterance": prediction[args.prediction_field],
            "asr_model": prediction.get("model"),
        })
        output_rows.append(row)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8") as handle:
        for row in output_rows:
            handle.write(json.dumps(row, ensure_ascii=False) + "\n")
    if args.control_output:
        args.control_output.parent.mkdir(parents=True, exist_ok=True)
        with args.control_output.open("w", encoding="utf-8") as handle:
            for row in output_rows:
                control = json.loads(json.dumps(row, ensure_ascii=False))
                control["utterance"] = control["original_utterance"]
                handle.write(json.dumps(control, ensure_ascii=False) + "\n")


if __name__ == "__main__":
    main()
