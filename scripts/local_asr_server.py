#!/usr/bin/env python3
from __future__ import annotations

import cgi
import gc
import json
import os
import shutil
import subprocess
import sys
import tempfile
import threading
import time
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "scripts"))

from asr_postprocess import normalize_asr_text  # noqa: E402


DEFAULT_HOST = "127.0.0.1"
DEFAULT_PORT = 5091
DEFAULT_MODEL = "turbo"
DEFAULT_MODEL_LABEL = "openai/whisper-large-v3-turbo"
DEFAULT_BACKEND = "faster-whisper"
TARGET_SAMPLE_RATE = 16000


def json_response(handler: BaseHTTPRequestHandler, status: int, payload: dict[str, Any]) -> None:
    body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
    handler.send_response(status)
    handler.send_header("Content-Type", "application/json; charset=utf-8")
    handler.send_header("Content-Length", str(len(body)))
    handler.end_headers()
    handler.wfile.write(body)


def convert_to_wav(audio_path: Path) -> Path:
    ffmpeg = shutil.which("ffmpeg") or "/opt/homebrew/bin/ffmpeg"
    if not Path(ffmpeg).exists():
        raise RuntimeError("ffmpeg is required to decode browser audio uploads.")

    wav_file = tempfile.NamedTemporaryFile(prefix="ct-asr-decoded-", suffix=".wav", delete=False)
    wav_path = Path(wav_file.name)
    wav_file.close()
    command = [
        ffmpeg,
        "-y",
        "-hide_banner",
        "-loglevel",
        "error",
        "-i",
        str(audio_path),
        "-ac",
        "1",
        "-ar",
        str(TARGET_SAMPLE_RATE),
        "-f",
        "wav",
        str(wav_path),
    ]
    completed = subprocess.run(command, capture_output=True, text=True, check=False)
    if completed.returncode != 0:
        details = (completed.stderr or completed.stdout or "unknown ffmpeg error").strip()
        raise ValueError(f"Audio decode failed: {details}")
    return wav_path


class LocalAsrRuntime:
    def __init__(self) -> None:
        self.model = os.getenv("CT_ASR_MODEL", DEFAULT_MODEL)
        self.model_label = os.getenv("CT_ASR_MODEL_LABEL", DEFAULT_MODEL_LABEL)
        self.backend = os.getenv("CT_ASR_BACKEND", DEFAULT_BACKEND)
        self.device = os.getenv("CT_ASR_DEVICE", "cpu")
        self.compute_type = os.getenv("CT_ASR_COMPUTE_TYPE", "float16")
        self.beam_size = int(os.getenv("CT_ASR_BEAM_SIZE", "1"))
        self.ollama_unload_url = os.getenv("CT_ASR_OLLAMA_UNLOAD_URL", "").strip()
        self.ollama_model = os.getenv("CT_ASR_OLLAMA_MODEL", "").strip()
        self.release_gpu_after_request = os.getenv("CT_ASR_RELEASE_GPU_AFTER_REQUEST", "false").lower() in {
            "1", "true", "yes"
        }
        self._mlx_whisper: Any | None = None
        self._faster_whisper_model: Any | None = None
        self._transformers_pipe: Any | None = None
        self._transformers_model: Any | None = None
        self._transformers_processor: Any | None = None

    def health(self) -> dict[str, Any]:
        return {
            "status": "ok",
            "provider": self.backend,
            "model": self.model_label,
            "model_id": self.model,
            "device": self.device,
            "compute_type": self.compute_type,
            "beam_size": self.beam_size,
            "loaded": (
                self._mlx_whisper is not None
                or self._faster_whisper_model is not None
                or self._transformers_pipe is not None
            ),
            "weights_cached": self._transformers_model is not None,
        }

    def preload_cpu_weights(self) -> None:
        """Keep Whisper weights in system RAM without reserving the shared GPU."""
        if self.backend != "transformers" or self._transformers_model is not None:
            return

        import torch
        from transformers import AutoModelForSpeechSeq2Seq, AutoProcessor

        dtype = torch.float16 if self.device in {"cuda", "mps", "auto"} else torch.float32
        self._transformers_processor = AutoProcessor.from_pretrained(self.model)
        self._transformers_model = AutoModelForSpeechSeq2Seq.from_pretrained(
            self.model,
            torch_dtype=dtype,
            low_cpu_mem_usage=True,
            use_safetensors=True,
        )

    def transcribe(self, audio_path: Path) -> dict[str, Any]:
        try:
            if self.backend == "transformers":
                return self._transcribe_transformers(audio_path)
            if self.backend == "mlx":
                return self._transcribe_mlx(audio_path)
            if self.backend == "faster-whisper":
                return self._transcribe_faster_whisper(audio_path)
            raise ValueError(f"Unsupported CT_ASR_BACKEND={self.backend!r}. Use transformers, faster-whisper or mlx.")
        finally:
            if self.release_gpu_after_request:
                self._release_gpu()

    def _unload_ollama(self) -> None:
        """Release a competing local LLM before loading Whisper on a shared GPU."""
        if not self.ollama_unload_url or not self.ollama_model:
            return

        payload = json.dumps({"model": self.ollama_model, "keep_alive": 0}).encode("utf-8")
        request = urllib.request.Request(
            self.ollama_unload_url,
            data=payload,
            headers={"Content-Type": "application/json"},
            method="POST",
        )
        try:
            with urllib.request.urlopen(request, timeout=10):
                pass
        except Exception as exc:  # noqa: BLE001
            raise RuntimeError(f"Unable to release the local LLM GPU allocation: {exc}") from exc

        status_url = self.ollama_unload_url.rsplit("/api/", maxsplit=1)[0] + "/api/ps"
        deadline = time.monotonic() + 10
        while time.monotonic() < deadline:
            try:
                with urllib.request.urlopen(status_url, timeout=2) as response:
                    payload = json.loads(response.read().decode("utf-8"))
                models = payload.get("models", [])
                if not any(item.get("name") == self.ollama_model for item in models):
                    # Ollama removes the runner from /api/ps just before the CUDA
                    # allocator releases its final buffers.
                    time.sleep(0.5)
                    return
            except Exception:  # noqa: BLE001
                # Retry briefly: Ollama may be between runner states.
                pass
            time.sleep(0.1)

        raise RuntimeError("Timed out waiting for the local LLM to release the GPU.")

    def _release_gpu(self) -> None:
        if self.backend != "transformers" or self.device not in {"cuda", "auto"}:
            return

        self._transformers_pipe = None
        gc.collect()
        try:
            import torch

            if torch.cuda.is_available():
                if self._transformers_model is not None:
                    self._transformers_model.to("cpu")
                torch.cuda.empty_cache()
        except Exception:  # noqa: BLE001
            # Releasing is an optimization, not a reason to turn a successful
            # transcription into an HTTP error.
            pass

    def _transcribe_mlx(self, audio_path: Path) -> dict[str, Any]:
        if self._mlx_whisper is None:
            import mlx_whisper

            self._mlx_whisper = mlx_whisper

        start = time.perf_counter()
        result = self._mlx_whisper.transcribe(
            str(audio_path),
            path_or_hf_repo=self.model,
            language="ru",
        )
        latency = time.perf_counter() - start
        text = str(result.get("text") or "").strip()
        normalized = normalize_asr_text(text)
        return {
            "text": text,
            "normalized_text": normalized,
            "normalizedText": normalized,
            "model": self.model_label,
            "model_id": self.model,
            "backend": self.backend,
            "latency_seconds": round(latency, 3),
            "latencySeconds": round(latency, 3),
        }

    def _transcribe_faster_whisper(self, audio_path: Path) -> dict[str, Any]:
        if self._faster_whisper_model is None:
            from faster_whisper import WhisperModel

            device = self.device
            if device == "auto":
                device = "cuda" if shutil.which("nvidia-smi") else "cpu"

            if device == "cuda":
                self._unload_ollama()

            self._faster_whisper_model = WhisperModel(
                self.model,
                device=device,
                compute_type=self.compute_type,
            )

        start = time.perf_counter()
        segments, _info = self._faster_whisper_model.transcribe(
            str(audio_path),
            language="ru",
            beam_size=self.beam_size,
            vad_filter=True,
        )
        text = " ".join(segment.text.strip() for segment in segments).strip()
        latency = time.perf_counter() - start
        normalized = normalize_asr_text(text)
        return {
            "text": text,
            "normalized_text": normalized,
            "normalizedText": normalized,
            "model": self.model_label,
            "model_id": self.model,
            "backend": self.backend,
            "device": self.device,
            "compute_type": self.compute_type,
            "beam_size": self.beam_size,
            "latency_seconds": round(latency, 3),
            "latencySeconds": round(latency, 3),
        }

    def _transcribe_transformers(self, audio_path: Path) -> dict[str, Any]:
        if self._transformers_pipe is None:
            import torch
            from transformers import pipeline

            device = self.device
            if device == "auto":
                if torch.cuda.is_available():
                    device = "cuda"
                elif torch.backends.mps.is_available():
                    device = "mps"
                else:
                    device = "cpu"
            if device == "cuda":
                self._unload_ollama()
            self.preload_cpu_weights()
            model = self._transformers_model
            processor = self._transformers_processor
            if model is None or processor is None:
                raise RuntimeError("Whisper weights were not initialized.")

            dtype = torch.float16 if device in {"cuda", "mps"} else torch.float32
            pipeline_device = -1
            if device == "cuda":
                model = model.to("cuda")
                pipeline_device = 0
            elif device == "mps":
                model = model.to("mps")

            self._transformers_pipe = pipeline(
                "automatic-speech-recognition",
                model=model,
                tokenizer=processor.tokenizer,
                feature_extractor=processor.feature_extractor,
                torch_dtype=dtype,
                device=pipeline_device,
            )

        start = time.perf_counter()
        result = self._transformers_pipe(
            str(audio_path),
            generate_kwargs={"language": "russian", "task": "transcribe"},
        )
        latency = time.perf_counter() - start
        text = str(result.get("text") or "").strip()
        normalized = normalize_asr_text(text)
        return {
            "text": text,
            "normalized_text": normalized,
            "normalizedText": normalized,
            "model": self.model_label,
            "model_id": self.model,
            "backend": self.backend,
            "device": self.device,
            "latency_seconds": round(latency, 3),
            "latencySeconds": round(latency, 3),
        }


RUNTIME = LocalAsrRuntime()
RUNTIME_LOCK = threading.Lock()


class Handler(BaseHTTPRequestHandler):
    server_version = "CattleTrackLocalAsr/1.0"

    def do_GET(self) -> None:  # noqa: N802
        if self.path == "/health":
            json_response(self, 200, RUNTIME.health())
            return
        json_response(self, 404, {"error": "not_found"})

    def do_POST(self) -> None:  # noqa: N802
        if self.path != "/transcribe":
            json_response(self, 404, {"error": "not_found"})
            return

        try:
            form = cgi.FieldStorage(
                fp=self.rfile,
                headers=self.headers,
                environ={
                    "REQUEST_METHOD": "POST",
                    "CONTENT_TYPE": self.headers.get("Content-Type", ""),
                    "CONTENT_LENGTH": self.headers.get("Content-Length", "0"),
                },
            )
            file_field = form["file"] if "file" in form else None
            if file_field is None or not getattr(file_field, "file", None):
                json_response(self, 400, {"error": "file field is required"})
                return

            suffix = Path(getattr(file_field, "filename", "") or "voice.webm").suffix or ".webm"
            with tempfile.NamedTemporaryFile(prefix="ct-asr-", suffix=suffix, delete=False) as tmp:
                tmp.write(file_field.file.read())
                tmp_path = Path(tmp.name)

            wav_path: Path | None = None
            try:
                wav_path = convert_to_wav(tmp_path)
                # A 12 GB GPU cannot keep Whisper large-v3 and the selected 32B
                # LLM resident together. Serialize access to the shared model.
                with RUNTIME_LOCK:
                    result = RUNTIME.transcribe(wav_path)
                json_response(self, 200, result)
            finally:
                tmp_path.unlink(missing_ok=True)
                if wav_path is not None:
                    wav_path.unlink(missing_ok=True)
        except Exception as exc:  # noqa: BLE001
            json_response(self, 500, {"error": f"{type(exc).__name__}: {exc}"})

    def log_message(self, format: str, *args: Any) -> None:
        sys.stderr.write("%s - - [%s] %s\n" % (self.address_string(), self.log_date_time_string(), format % args))


def main() -> int:
    host = os.getenv("CT_ASR_HOST", DEFAULT_HOST)
    port = int(os.getenv("CT_ASR_PORT", str(DEFAULT_PORT)))
    if os.getenv("CT_ASR_PRELOAD_CPU", "false").lower() in {"1", "true", "yes"}:
        RUNTIME.preload_cpu_weights()
    server = ThreadingHTTPServer((host, port), Handler)
    print(json.dumps({"event": "asr_server_started", "host": host, "port": port, **RUNTIME.health()}, ensure_ascii=False), flush=True)
    server.serve_forever()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
