#!/usr/bin/env python3
"""Run the CattleTrack ASR manifest through a deployed /transcribe endpoint."""

from __future__ import annotations

import argparse
import json
import mimetypes
import time
import urllib.error
import urllib.request
import uuid
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[1]


def read_jsonl(path: Path) -> list[dict[str, Any]]:
    with path.open(encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def multipart_body(path: Path) -> tuple[bytes, str]:
    boundary = f"----cattletrack-{uuid.uuid4().hex}"
    content_type = mimetypes.guess_type(path.name)[0] or "application/octet-stream"
    body = b"\r\n".join(
        [
            f"--{boundary}".encode(),
            f'Content-Disposition: form-data; name="file"; filename="{path.name}"'.encode(),
            f"Content-Type: {content_type}".encode(),
            b"",
            path.read_bytes(),
            f"--{boundary}--".encode(),
            b"",
        ]
    )
    return body, f"multipart/form-data; boundary={boundary}"


def transcribe(url: str, audio_path: Path, timeout: int) -> dict[str, Any]:
    body, content_type = multipart_body(audio_path)
    request = urllib.request.Request(url, data=body, headers={"Content-Type": content_type}, method="POST")
    try:
        with urllib.request.urlopen(request, timeout=timeout) as response:
            return json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        error_body = exc.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"HTTP {exc.code}: {error_body}") from exc


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-url", required=True)
    parser.add_argument("--manifest", type=Path, default=Path("datasets/asr/manifest.jsonl"))
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--model-label", required=True)
    parser.add_argument("--limit", type=int)
    parser.add_argument("--warmup", type=int, default=1)
    parser.add_argument("--timeout", type=int, default=120)
    args = parser.parse_args()

    rows = read_jsonl(args.manifest)
    if args.limit:
        rows = rows[: args.limit]
    url = args.base_url.rstrip("/") + "/transcribe"
    for row in rows[: min(args.warmup, len(rows))]:
        transcribe(url, ROOT / row["audio_path"], args.timeout)

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("w", encoding="utf-8") as output:
        for row in rows:
            started = time.perf_counter()
            try:
                response = transcribe(url, ROOT / row["audio_path"], args.timeout)
                record = {
                    "id": row["id"],
                    "model": args.model_label,
                    "prediction": response.get("text", ""),
                    "raw_text": response.get("raw_text"),
                    "latency_sec": round(time.perf_counter() - started, 4),
                    "server_latency_ms": response.get("latency_ms"),
                    "error": None,
                }
            except Exception as exc:  # keep smoke runs resumable
                record = {
                    "id": row["id"],
                    "model": args.model_label,
                    "prediction": "",
                    "latency_sec": round(time.perf_counter() - started, 4),
                    "error": f"{type(exc).__name__}: {exc}",
                }
            output.write(json.dumps(record, ensure_ascii=False) + "\n")
            output.flush()


if __name__ == "__main__":
    main()
