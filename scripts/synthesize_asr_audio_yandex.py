#!/usr/bin/env python3
import argparse
import json
import math
import os
import random
import struct
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import wave
from pathlib import Path
from typing import Any, Optional

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "datasets" / "asr" / "manifest.jsonl"
ENDPOINT = "https://tts.api.cloud.yandex.net/speech/v1/tts:synthesize"
SAMPLE_RATE = 48000

VOICE_BY_PROFILE = {
    "voice_f_01": "alena",
    "voice_m_01": "filipp",
    "voice_f_02": "jane",
    "voice_m_02": "ermil",
}


def read_manifest(path: Path) -> list[dict[str, Any]]:
    rows = []
    with path.open("r", encoding="utf-8") as f:
        for line in f:
            rows.append(json.loads(line))
    return rows


def normalize_for_tts(text: str) -> str:
    replacements = {
        "A-17": "а семнадцать",
        "A 17": "а семнадцать",
        "A17": "а семнадцать",
        "B12": "бэ двенадцать",
        "E-55": "е пятьдесят пять",
        "E-77": "е семьдесят семь",
        "E-99": "е девяносто девять",
        "E-9": "е девять",
        "E-10": "е десять",
        "RFID": "эр эф ай ди",
        "518.5": "пятьсот восемнадцать и пять",
        "419.7": "четыреста девятнадцать и семь",
    }
    for src, dst in replacements.items():
        text = text.replace(src, dst)
    return text


def synthesize_lpcm(api_key: str, text: str, voice: str, speed: str) -> bytes:
    data = urllib.parse.urlencode({
        "text": text,
        "lang": "ru-RU",
        "voice": voice,
        "speed": speed,
        "format": "lpcm",
        "sampleRateHertz": str(SAMPLE_RATE),
    }).encode("utf-8")
    request = urllib.request.Request(
        ENDPOINT,
        data=data,
        headers={"Authorization": f"Api-Key {api_key}"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            return response.read()
    except urllib.error.HTTPError as exc:
        body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"Yandex TTS HTTP {exc.code}: {body}") from exc


def pcm16_samples(data: bytes) -> list[int]:
    count = len(data) // 2
    return list(struct.unpack("<" + "h" * count, data[:count * 2]))


def samples_to_pcm16(samples: list[int]) -> bytes:
    clipped = [max(-32768, min(32767, int(value))) for value in samples]
    return struct.pack("<" + "h" * len(clipped), *clipped)


def rms(samples: list[int]) -> float:
    if not samples:
        return 0.0
    return math.sqrt(sum(sample * sample for sample in samples) / len(samples))


def add_farm_noise(samples: list[int], noise_id: str, snr_db: Optional[int], seed: int) -> list[int]:
    if noise_id == "quiet" or snr_db is None:
        return samples

    rng = random.Random(seed)
    speech_rms = max(rms(samples), 1.0)
    noise_rms_target = speech_rms / (10 ** (snr_db / 20))
    noise = []
    phase_50 = rng.random() * math.tau
    phase_120 = rng.random() * math.tau

    for i in range(len(samples)):
        t = i / SAMPLE_RATE
        hum = math.sin(math.tau * 50 * t + phase_50) * 0.45
        motor = math.sin(math.tau * 120 * t + phase_120) * 0.2
        broadband = rng.uniform(-1.0, 1.0) * 0.35
        clank = 0.0
        if i % int(SAMPLE_RATE * 1.7) < 90:
            clank = rng.uniform(-1.0, 1.0) * 1.6
        low_moo = 0.0
        if noise_id == "farm_medium":
            low_moo = math.sin(math.tau * 140 * t) * 0.25 * (0.5 + 0.5 * math.sin(math.tau * 0.7 * t))
        noise.append(hum + motor + broadband + clank + low_moo)

    current_rms = max(math.sqrt(sum(x * x for x in noise) / len(noise)), 1e-6)
    scale = noise_rms_target / current_rms
    return [sample + noise_value * scale for sample, noise_value in zip(samples, noise)]


def write_wav(path: Path, samples: list[int]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with wave.open(str(path), "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(SAMPLE_RATE)
        wav.writeframes(samples_to_pcm16(samples))


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate ASR WAV files with Yandex SpeechKit.")
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--limit", type=int)
    parser.add_argument("--force", action="store_true")
    parser.add_argument("--sleep", type=float, default=0.15)
    args = parser.parse_args()

    api_key = os.environ.get("YANDEX_API_KEY")
    if not api_key:
        print("YANDEX_API_KEY is required", file=sys.stderr)
        return 2

    rows = read_manifest(args.manifest)
    if args.limit:
        rows = rows[:args.limit]

    generated = 0
    skipped = 0
    errors = []

    for index, row in enumerate(rows, start=1):
        out_path = ROOT / row["audio_path"]
        if out_path.exists() and not args.force:
            skipped += 1
            continue

        voice_id = row["voice_profile"]["id"]
        voice = VOICE_BY_PROFILE.get(voice_id, "alena")
        speed = "1.12" if row["voice_profile"].get("style") == "fast" else "1.0"
        tts_text = normalize_for_tts(row["golden_transcript"])

        try:
            raw = synthesize_lpcm(api_key, tts_text, voice, speed)
            samples = pcm16_samples(raw)
            samples = add_farm_noise(
                samples,
                row["noise_profile"]["id"],
                row["noise_profile"].get("snr_db"),
                seed=index,
            )
            write_wav(out_path, samples)
            generated += 1
            print(f"[{index}/{len(rows)}] generated {out_path.relative_to(ROOT)} voice={voice} noise={row['noise_profile']['id']}")
            time.sleep(args.sleep)
        except Exception as exc:
            errors.append((row["id"], str(exc)))
            print(f"[{index}/{len(rows)}] ERROR {row['id']}: {exc}", file=sys.stderr)
            break

    summary = {"generated": generated, "skipped": skipped, "errors": errors}
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
