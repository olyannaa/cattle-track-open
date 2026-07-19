#!/usr/bin/env python3
"""Run NVIDIA Canary ASR on the CattleTrack manifest."""

from __future__ import annotations

import argparse
import json
import platform
import sys
import time
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "datasets" / "asr" / "manifest.jsonl"


def read_jsonl(path: Path) -> list[dict[str, Any]]:
    with path.open(encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def transcription_text(value: Any) -> str:
    if hasattr(value, "text"):
        return str(value.text).strip()
    if isinstance(value, dict) and "text" in value:
        return str(value["text"]).strip()
    return str(value).strip()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--model-id", default="nvidia/canary-1b-v2")
    parser.add_argument("--model-label", default="canary-1b-v2")
    parser.add_argument("--limit", type=int)
    parser.add_argument("--warmup", type=int, default=1)
    args = parser.parse_args()

    from nemo.collections.asr.models import ASRModel

    print(
        json.dumps(
            {
                "event": "load_model",
                "model_id": args.model_id,
                "python": sys.version.split()[0],
                "platform": platform.platform(),
            },
            ensure_ascii=False,
        ),
        flush=True,
    )
    model = ASRModel.from_pretrained(model_name=args.model_id)
    rows = read_jsonl(args.manifest)
    if args.limit:
        rows = rows[: args.limit]

    for row in rows[: min(args.warmup, len(rows))]:
        model.transcribe([str(ROOT / row["audio_path"])], source_lang="ru", target_lang="ru")

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8") as output:
        for row in rows:
            started = time.perf_counter()
            try:
                result = model.transcribe(
                    [str(ROOT / row["audio_path"])],
                    source_lang="ru",
                    target_lang="ru",
                )
                prediction = transcription_text(result[0])
                record = {
                    "id": row["id"],
                    "model": args.model_label,
                    "model_id": args.model_id,
                    "prediction": prediction,
                    "latency_sec": time.perf_counter() - started,
                    "error": None,
                }
            except Exception as exc:  # keep a long benchmark resumable
                record = {
                    "id": row["id"],
                    "model": args.model_label,
                    "model_id": args.model_id,
                    "prediction": "",
                    "latency_sec": None,
                    "error": f"{type(exc).__name__}: {exc}",
                }
            output.write(json.dumps(record, ensure_ascii=False) + "\n")
            output.flush()


if __name__ == "__main__":
    main()
