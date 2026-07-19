#!/usr/bin/env python3
"""Generate oracle predictions from golden labels for evaluator smoke tests."""

from __future__ import annotations

import argparse
import glob
import json
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", action="append", default=[])
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--samples", type=int, default=3)
    args = parser.parse_args()

    args.out.parent.mkdir(parents=True, exist_ok=True)
    with args.out.open("w", encoding="utf-8") as output:
        dataset_paths = args.dataset or ["datasets/tool_calling/*.jsonl", "datasets/fault_injection/*.jsonl"]
        for pattern in dataset_paths:
            for path in sorted(glob.glob(pattern)):
                with open(path, encoding="utf-8") as handle:
                    for line in handle:
                        if not line.strip():
                            continue
                        row = json.loads(line)
                        for sample_index in range(args.samples):
                            output.write(
                                json.dumps(
                                    {
                                        "id": row["id"],
                                        "model": "oracle",
                                        "sample_index": sample_index,
                                        "tool_calls": row["golden"]["tool_calls"],
                                    },
                                    ensure_ascii=False,
                                    sort_keys=True,
                                )
                                + "\n"
                            )


if __name__ == "__main__":
    main()
