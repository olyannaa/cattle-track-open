#!/usr/bin/env python3
import argparse
import json
import platform
import sys
import time
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "datasets" / "asr" / "manifest.jsonl"


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
    parser = argparse.ArgumentParser(description="Run GigaAM ASR benchmark.")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--model-name", default="v3_e2e_rnnt")
    parser.add_argument("--model-label", default=None)
    parser.add_argument("--limit", type=int)
    args = parser.parse_args()

    import gigaam

    model_label = args.model_label or f"gigaam/{args.model_name}"
    print(json.dumps({
        "event": "load_model",
        "model_name": args.model_name,
        "python": sys.version.split()[0],
        "platform": platform.platform(),
    }, ensure_ascii=False), flush=True)
    model = gigaam.load_model(args.model_name)

    rows = load_jsonl(args.manifest)
    if args.limit:
        rows = rows[:args.limit]
    args.output.parent.mkdir(parents=True, exist_ok=True)

    with args.output.open("w", encoding="utf-8") as out:
        for index, row in enumerate(rows, start=1):
            audio_path = ROOT / row["audio_path"]
            record = {
                "id": row["id"],
                "model": model_label,
                "model_id": args.model_name,
                "audio_path": row["audio_path"],
            }
            try:
                start = time.perf_counter()
                result = model.transcribe(str(audio_path))
                latency = time.perf_counter() - start
                if hasattr(result, "text"):
                    result = result.text
                record.update({
                    "prediction": str(result).strip(),
                    "latency_sec": latency,
                    "error": None,
                })
            except Exception as exc:  # noqa: BLE001
                record.update({
                    "prediction": "",
                    "latency_sec": None,
                    "error": f"{type(exc).__name__}: {exc}",
                })
            out.write(json.dumps(record, ensure_ascii=False) + "\n")
            out.flush()
            print(json.dumps({
                "event": "transcribed",
                "index": index,
                "total": len(rows),
                "id": row["id"],
                "latency_sec": record["latency_sec"],
                "error": record["error"],
            }, ensure_ascii=False), flush=True)

    return 0


if __name__ == "__main__":
    sys.exit(main())
