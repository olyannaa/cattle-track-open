#!/usr/bin/env python3
"""Benchmark Silero TTS v5 latency and real-time factor for CattleTrack prompts."""

from __future__ import annotations

import argparse
import json
import platform
import statistics
import time
from pathlib import Path
from typing import Any

import soundfile as sf
import torch


DEFAULT_TEXTS = [
    "Нашла двух животных с биркой 523. Уточните по дате рождения или группе.",
    "Черновик готов. Осеменение коровы 1432 за девятое июля, тип искусственное.",
    "Не могу сохранить: для естественного осеменения нужен бык.",
    "Вес 420 килограммов для 1432 записан в черновик. Подтвердить?",
    "Часть batch невалидна: животное 0000 не найдено, по бирке 523 есть несколько совпадений.",
]


def synthesize(model: Any, text: str, speaker: str, sample_rate: int) -> torch.Tensor:
    return model.apply_tts(
        text=text,
        speaker=speaker,
        sample_rate=sample_rate,
        put_accent=True,
        put_yo=True,
    )


def percentile(values: list[float], p: float) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    index = min(len(ordered) - 1, max(0, round((len(ordered) - 1) * p)))
    return ordered[index]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--language", default="ru")
    parser.add_argument("--model-id", default="v5_ru")
    parser.add_argument("--sample-rate", type=int, default=48000)
    parser.add_argument("--speaker", default="baya")
    parser.add_argument("--runs", type=int, default=3)
    parser.add_argument("--warmup", type=int, default=1)
    parser.add_argument("--out-json", type=Path, required=True)
    parser.add_argument("--audio-dir", type=Path)
    args = parser.parse_args()

    torch.set_num_threads(1)
    started_load = time.perf_counter()
    model, example_text = torch.hub.load(
        repo_or_dir="snakers4/silero-models",
        model="silero_tts",
        language=args.language,
        speaker=args.model_id,
    )
    model.to(torch.device("cpu"))
    load_seconds = time.perf_counter() - started_load

    for _ in range(args.warmup):
        synthesize(model, DEFAULT_TEXTS[0], args.speaker, args.sample_rate)

    if args.audio_dir:
        args.audio_dir.mkdir(parents=True, exist_ok=True)

    rows = []
    for text_index, text in enumerate(DEFAULT_TEXTS, 1):
        for run_index in range(args.runs):
            started = time.perf_counter()
            audio = synthesize(model, text, args.speaker, args.sample_rate)
            latency_seconds = time.perf_counter() - started
            samples = int(audio.numel())
            audio_seconds = samples / args.sample_rate
            rtf = latency_seconds / audio_seconds if audio_seconds else None
            audio_path = None
            if args.audio_dir and run_index == 0:
                audio_path = args.audio_dir / f"silero-v5-ru-{text_index:02d}.wav"
                sf.write(audio_path, audio.detach().cpu().numpy(), args.sample_rate)
            rows.append(
                {
                    "text_index": text_index,
                    "run_index": run_index,
                    "text": text,
                    "chars": len(text),
                    "latency_seconds": round(latency_seconds, 4),
                    "audio_seconds": round(audio_seconds, 4),
                    "rtf": round(rtf, 4) if rtf is not None else None,
                    "audio_path": str(audio_path) if audio_path else None,
                }
            )

    latencies = [row["latency_seconds"] for row in rows]
    rtfs = [row["rtf"] for row in rows if row["rtf"] is not None]
    report = {
        "model": "silero_tts",
        "language": args.language,
        "model_id": args.model_id,
        "speaker": args.speaker,
        "sample_rate": args.sample_rate,
        "torch_version": torch.__version__,
        "platform": platform.platform(),
        "processor": platform.processor(),
        "load_seconds": round(load_seconds, 4),
        "summary": {
            "runs": len(rows),
            "latency_seconds_mean": round(statistics.mean(latencies), 4),
            "latency_seconds_median": round(statistics.median(latencies), 4),
            "latency_seconds_p95": round(percentile(latencies, 0.95), 4),
            "rtf_mean": round(statistics.mean(rtfs), 4),
            "rtf_median": round(statistics.median(rtfs), 4),
            "rtf_p95": round(percentile(rtfs, 0.95), 4),
        },
        "rows": rows,
        "example_text_from_model": example_text,
    }
    args.out_json.parent.mkdir(parents=True, exist_ok=True)
    args.out_json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
