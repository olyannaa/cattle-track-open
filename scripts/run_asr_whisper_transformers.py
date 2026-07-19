#!/usr/bin/env python3
import argparse
import json
import os
import platform
import sys
import time
import wave
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


def load_wav(path: Path) -> tuple["Any", int]:
    import numpy as np

    with wave.open(str(path), "rb") as wav:
        channels = wav.getnchannels()
        sample_width = wav.getsampwidth()
        sample_rate = wav.getframerate()
        frames = wav.readframes(wav.getnframes())
    if sample_width != 2:
        raise ValueError(f"{path}: expected 16-bit PCM WAV")
    audio = np.frombuffer(frames, dtype=np.int16).astype(np.float32) / 32768.0
    if channels > 1:
        audio = audio.reshape(-1, channels).mean(axis=1)
    return audio, sample_rate


def choose_device(requested: str) -> tuple[str, int]:
    import torch

    if requested == "auto":
        if torch.backends.mps.is_available():
            return "mps", -1
        if torch.cuda.is_available():
            return "cuda", 0
        return "cpu", -1
    if requested == "mps":
        if not torch.backends.mps.is_available():
            raise SystemExit("MPS requested, but torch.backends.mps.is_available() is false")
        return "mps", -1
    if requested == "cuda":
        if not torch.cuda.is_available():
            raise SystemExit("CUDA requested, but torch.cuda.is_available() is false")
        return "cuda", 0
    return "cpu", -1


def main() -> int:
    parser = argparse.ArgumentParser(description="Run Whisper ASR benchmark via Hugging Face Transformers.")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--model-id", default="openai/whisper-large-v3")
    parser.add_argument("--model-label", default=None)
    parser.add_argument("--device", choices=["auto", "cpu", "mps", "cuda"], default="auto")
    parser.add_argument("--limit", type=int)
    parser.add_argument("--warmup", type=int, default=1)
    args = parser.parse_args()

    import torch
    from transformers import AutoModelForSpeechSeq2Seq, AutoProcessor, pipeline

    device_name, pipeline_device = choose_device(args.device)
    dtype = torch.float16 if device_name in {"mps", "cuda"} else torch.float32
    model_label = args.model_label or args.model_id

    print(json.dumps({
        "event": "load_model",
        "model_id": args.model_id,
        "device": device_name,
        "dtype": str(dtype),
        "python": sys.version.split()[0],
        "platform": platform.platform(),
    }, ensure_ascii=False), flush=True)

    processor = AutoProcessor.from_pretrained(args.model_id)
    model = AutoModelForSpeechSeq2Seq.from_pretrained(
        args.model_id,
        torch_dtype=dtype,
        low_cpu_mem_usage=True,
        use_safetensors=True,
    )
    if device_name == "mps":
        model = model.to("mps")
    elif device_name == "cuda":
        model = model.to("cuda")

    pipe = pipeline(
        "automatic-speech-recognition",
        model=model,
        tokenizer=processor.tokenizer,
        feature_extractor=processor.feature_extractor,
        torch_dtype=dtype,
        device=pipeline_device,
    )

    rows = load_jsonl(args.manifest)
    if args.limit:
        rows = rows[:args.limit]
    args.output.parent.mkdir(parents=True, exist_ok=True)

    warmup_rows = rows[: max(0, min(args.warmup, len(rows)))]
    for row in warmup_rows:
        audio, sample_rate = load_wav(ROOT / row["audio_path"])
        pipe({"array": audio, "sampling_rate": sample_rate}, generate_kwargs={"language": "russian", "task": "transcribe"})

    with args.output.open("w", encoding="utf-8") as out:
        for index, row in enumerate(rows, start=1):
            audio_path = ROOT / row["audio_path"]
            record = {
                "id": row["id"],
                "model": model_label,
                "model_id": args.model_id,
                "device": device_name,
                "audio_path": row["audio_path"],
            }
            try:
                audio, sample_rate = load_wav(audio_path)
                start = time.perf_counter()
                result = pipe(
                    {"array": audio, "sampling_rate": sample_rate},
                    generate_kwargs={"language": "russian", "task": "transcribe"},
                )
                latency = time.perf_counter() - start
                record.update({
                    "prediction": result["text"].strip(),
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
    os.environ.setdefault("TOKENIZERS_PARALLELISM", "false")
    sys.exit(main())
