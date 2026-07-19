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


def result_text(result: Any) -> str:
    if isinstance(result, str):
        return result
    if isinstance(result, list):
        parts = []
        for item in result:
            parts.append(str(getattr(item, "text", item)))
        return " ".join(part.strip() for part in parts if part.strip())
    return str(getattr(result, "text", result))


def main() -> int:
    parser = argparse.ArgumentParser(description="Run T-one ASR benchmark.")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--model-label", default="t-one")
    parser.add_argument("--limit", type=int)
    args = parser.parse_args()

    from tone import StreamingCTCPipeline, read_audio

    print(json.dumps({
        "event": "load_model",
        "model": args.model_label,
        "python": sys.version.split()[0],
        "platform": platform.platform(),
    }, ensure_ascii=False), flush=True)
    pipeline = StreamingCTCPipeline.from_hugging_face()

    rows = load_jsonl(args.manifest)
    if args.limit:
        rows = rows[:args.limit]
    args.output.parent.mkdir(parents=True, exist_ok=True)

    with args.output.open("w", encoding="utf-8") as out:
        for index, row in enumerate(rows, start=1):
            audio_path = ROOT / row["audio_path"]
            record = {
                "id": row["id"],
                "model": args.model_label,
                "model_id": "voicekit-team/T-one",
                "audio_path": row["audio_path"],
            }
            try:
                audio = read_audio(str(audio_path))
                start = time.perf_counter()
                result = pipeline.forward_offline(audio)
                latency = time.perf_counter() - start
                record.update({
                    "prediction": result_text(result).strip(),
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
