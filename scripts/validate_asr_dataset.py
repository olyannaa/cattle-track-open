#!/usr/bin/env python3
import json
import sys
import wave
from collections import Counter
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "datasets" / "asr" / "manifest.jsonl"
EXPECTED_REVIEW_STATUSES = {"pending", "approved", "needs_edit", "reject", "duplicate"}
EXPECTED_TOKEN_CATEGORIES = {"number", "proper_noun", "other"}
EXPECTED_NOISE_PROFILES = {"quiet", "farm_light", "farm_medium"}


def load_jsonl(path: Path) -> list[dict[str, Any]]:
    rows = []
    with path.open("r", encoding="utf-8") as f:
        for line_no, line in enumerate(f, start=1):
            try:
                rows.append(json.loads(line))
            except json.JSONDecodeError as exc:
                raise ValueError(f"{path}:{line_no}: invalid JSON: {exc}") from exc
    return rows


def validate_wav(path: Path) -> tuple[float, list[str]]:
    errors = []
    try:
        with wave.open(str(path), "rb") as wav:
            channels = wav.getnchannels()
            sample_width = wav.getsampwidth()
            frame_rate = wav.getframerate()
            frames = wav.getnframes()
            duration = frames / frame_rate if frame_rate else 0.0
    except wave.Error as exc:
        return 0.0, [f"invalid wav: {exc}"]

    if channels != 1:
        errors.append(f"expected mono audio, got {channels} channels")
    if sample_width != 2:
        errors.append(f"expected 16-bit PCM, got sample width {sample_width}")
    if frame_rate != 48000:
        errors.append(f"expected 48000 Hz, got {frame_rate}")
    if duration <= 0:
        errors.append("expected positive duration")
    return duration, errors


def main() -> int:
    errors = []
    ids = set()
    source_strata = Counter()
    noise_profiles = Counter()
    voice_profiles = Counter()
    review_statuses = Counter()
    durations = []

    if not MANIFEST.exists():
        print(f"ASR dataset validation failed:\n- missing manifest {MANIFEST}")
        return 1

    try:
        rows = load_jsonl(MANIFEST)
    except ValueError as exc:
        print(f"ASR dataset validation failed:\n- {exc}")
        return 1

    if len(rows) < 60 or len(rows) > 80:
        errors.append(f"expected 60-80 ASR examples, got {len(rows)}")

    for line_no, row in enumerate(rows, start=1):
        prefix = f"{MANIFEST}:{line_no}"
        row_id = row.get("id")
        if not row_id:
            errors.append(f"{prefix}: missing id")
        elif row_id in ids:
            errors.append(f"{prefix}: duplicate id {row_id}")
        else:
            ids.add(row_id)

        if not isinstance(row.get("golden_transcript"), str) or not row["golden_transcript"].strip():
            errors.append(f"{prefix}: missing golden_transcript")

        audio_path_value = row.get("audio_path")
        if not isinstance(audio_path_value, str) or not audio_path_value:
            errors.append(f"{prefix}: missing audio_path")
        else:
            audio_path = ROOT / audio_path_value
            if not audio_path.exists():
                errors.append(f"{prefix}: missing audio file {audio_path_value}")
            else:
                duration, wav_errors = validate_wav(audio_path)
                durations.append(duration)
                for wav_error in wav_errors:
                    errors.append(f"{prefix}: {wav_error}")

        tokens = row.get("tokens")
        if not isinstance(tokens, list) or not tokens:
            errors.append(f"{prefix}: tokens must be a non-empty list")
        else:
            for token_index, token in enumerate(tokens):
                if not isinstance(token, dict):
                    errors.append(f"{prefix}: tokens[{token_index}] must be an object")
                    continue
                if not isinstance(token.get("text"), str) or not token["text"].strip():
                    errors.append(f"{prefix}: tokens[{token_index}].text is required")
                if token.get("category") not in EXPECTED_TOKEN_CATEGORIES:
                    errors.append(f"{prefix}: tokens[{token_index}].category must be one of {sorted(EXPECTED_TOKEN_CATEGORIES)}")

        review = row.get("review")
        if not isinstance(review, dict) or review.get("status") not in EXPECTED_REVIEW_STATUSES:
            errors.append(f"{prefix}: review.status must be one of {sorted(EXPECTED_REVIEW_STATUSES)}")
        else:
            review_statuses[review["status"]] += 1

        noise_profile = row.get("noise_profile", {})
        noise_profile_id = noise_profile.get("id") if isinstance(noise_profile, dict) else noise_profile
        if noise_profile_id not in EXPECTED_NOISE_PROFILES:
            errors.append(f"{prefix}: unexpected noise_profile {noise_profile!r}")
        else:
            noise_profiles[noise_profile_id] += 1

        voice_profile = row.get("voice_profile", {})
        voice_profile_id = voice_profile.get("id") if isinstance(voice_profile, dict) else voice_profile
        if not isinstance(voice_profile_id, str) or not voice_profile_id:
            errors.append(f"{prefix}: missing voice_profile")
        else:
            voice_profiles[voice_profile_id] += 1

        source_stratum = row.get("source_stratum")
        if not isinstance(source_stratum, str) or not source_stratum:
            errors.append(f"{prefix}: missing source_stratum")
        else:
            source_strata[source_stratum] += 1

    if errors:
        print("ASR dataset validation failed:")
        for error in errors:
            print(f"- {error}")
        return 1

    result = {
        "total": len(rows),
        "review_statuses": dict(sorted(review_statuses.items())),
        "by_source_stratum": dict(sorted(source_strata.items())),
        "by_noise_profile": dict(sorted(noise_profiles.items())),
        "by_voice_profile": dict(sorted(voice_profiles.items())),
        "audio": {
            "duration_sec_min": round(min(durations), 3) if durations else None,
            "duration_sec_max": round(max(durations), 3) if durations else None,
            "duration_sec_total": round(sum(durations), 3),
            "format": "mono PCM 16-bit 48000 Hz",
        },
    }
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
