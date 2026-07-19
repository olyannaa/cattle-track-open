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


def transcribe(model: "Any", audio_path: Path, beam_size: int) -> str:
    segments, _ = model.transcribe(
        str(audio_path),
        language="ru",
        task="transcribe",
        beam_size=beam_size,
        condition_on_previous_text=False,
        vad_filter=False,
    )
    return " ".join(segment.text.strip() for segment in segments).strip()


def main() -> int:
    parser = argparse.ArgumentParser(description="Run Whisper through the faster-whisper runtime.")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--model-id", default="turbo")
    parser.add_argument("--model-label", default=None)
    parser.add_argument("--device", choices=["cpu", "cuda"], default="cuda")
    parser.add_argument("--compute-type", default="float16")
    parser.add_argument("--beam-size", type=int, default=1)
    parser.add_argument("--limit", type=int)
    parser.add_argument("--warmup", type=int, default=1)
    args = parser.parse_args()

    from faster_whisper import WhisperModel

    model_label = args.model_label or f"{args.model_id} (faster-whisper/{args.compute_type})"
    print(json.dumps({
        "event": "load_model",
        "model_id": args.model_id,
        "device": args.device,
        "compute_type": args.compute_type,
        "beam_size": args.beam_size,
        "python": sys.version.split()[0],
        "platform": platform.platform(),
    }, ensure_ascii=False), flush=True)
    model = WhisperModel(args.model_id, device=args.device, compute_type=args.compute_type)

    rows = load_jsonl(args.manifest)
    if args.limit:
        rows = rows[:args.limit]
    args.output.parent.mkdir(parents=True, exist_ok=True)

    for row in rows[: max(0, min(args.warmup, len(rows)))]:
        transcribe(model, ROOT / row["audio_path"], args.beam_size)

    with args.output.open("w", encoding="utf-8") as out:
        for index, row in enumerate(rows, start=1):
            audio_path = ROOT / row["audio_path"]
            record = {
                "id": row["id"],
                "model": model_label,
                "model_id": args.model_id,
                "runtime": "faster-whisper",
                "device": args.device,
                "compute_type": args.compute_type,
                "beam_size": args.beam_size,
                "audio_path": row["audio_path"],
            }
            try:
                start = time.perf_counter()
                prediction = transcribe(model, audio_path, args.beam_size)
                record.update({
                    "prediction": prediction,
                    "latency_sec": time.perf_counter() - start,
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
