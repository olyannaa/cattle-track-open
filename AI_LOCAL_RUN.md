# AI local run

This is the local runtime path for the CattleTrack AI assistant.

## Models

- LLM: `qwen3.5:9b` via Ollama OpenAI-compatible API, constrained JSON output with `/no_think`.
- ASR target: `openai/whisper-large-v3-turbo` via the local ASR HTTP service.

Do not use `mlx-community/whisper-tiny-mlx` as the product ASR model. It is only useful as a fast wiring smoke profile.

## Start services

Run Redis and Ollama first:

```bash
brew services start redis
ollama serve
```

The browser records voice as `webm/opus`. Keep `ffmpeg` available in `PATH`; the local ASR service decodes every upload to mono 16 kHz wav before passing it to Whisper.

Install ASR Python dependencies in a local virtual environment when setting up a fresh machine:

```bash
python3 -m venv .venv-asr
. .venv-asr/bin/activate
python -m pip install \
  faster-whisper soundfile legacy-cgi
```

Make sure Qwen3.5 is available:

```bash
ollama list
```

Start ASR:

```bash
cd <repo-root>
PATH="/opt/homebrew/bin:$PATH" \
CT_ASR_BACKEND=faster-whisper \
CT_ASR_MODEL=turbo \
CT_ASR_MODEL_LABEL=openai/whisper-large-v3-turbo \
CT_ASR_DEVICE=cpu \
CT_ASR_COMPUTE_TYPE=int8 \
CT_ASR_BEAM_SIZE=1 \
python scripts/local_asr_server.py
```

CPU ASR is a local fallback. For faster demos, configure `AiAssistant__Asr__Endpoint` to a GPU ASR service you control.

Start backend with secrets only in environment variables, not in tracked files:

```bash
./scripts/run-local-backend.sh
```

The script loads `.env` and optional ignored `.env.local`. For a host Redis
service, put `REDIS_CONFIGURATION=127.0.0.1:6379` in `.env.local`; Docker
Compose continues to use its own `redis:6379` hostname.

Equivalent explicit configuration:

```bash
ASPNETCORE_ENVIRONMENT=Development \
ASPNETCORE_URLS=http://localhost:5088 \
ConnectionStrings__PostgresDB='<local secret>' \
RedisCacheOptions__Configuration=localhost:6379 \
RedisCacheOptions__InstanceName=LinksCachinglocal \
TelegramBot__ApiKey='<local secret>' \
Enviroment__BaseFrontUrl=http://localhost:3000 \
Enviroment__LocalAdminId='<local id>' \
Enviroment__OrgAdminId='<local id>' \
AiAssistant__Llm__Provider=ollama \
AiAssistant__Llm__Endpoint=http://localhost:11434/v1 \
AiAssistant__Llm__Model='qwen3.5:9b' \
AiAssistant__Llm__OutputMode=constrained-json \
AiAssistant__Llm__MaxTokens=256 \
AiAssistant__Llm__TimeoutSeconds=45 \
AiAssistant__Asr__Provider=local-http \
AiAssistant__Asr__Endpoint=http://127.0.0.1:5091 \
AiAssistant__Asr__Model=openai/whisper-large-v3-turbo \
AiAssistant__Asr__TimeoutSeconds=30 \
dotnet run --project backend/CAT/CAT.csproj
```

Start frontend:

```bash
cd frontend
VITE_API_URL=http://localhost:5088/api/ npm run dev -- --host 0.0.0.0
```

Open:

```text
http://localhost:3000/app/
```

## Smoke checks

Text AI:

```bash
curl -sS -b /tmp/cattle-track-cookies.txt \
  -H 'Content-Type: application/json' \
  -H 'organizationId: <organization id>' \
  -X POST http://localhost:5088/api/AiAssistant/text \
  -d '{"text":"покажи список групп","clientRequestId":"smoke-text"}'
```

Voice AI:

```bash
curl -sS -b /tmp/cattle-track-cookies.txt \
  -H 'organizationId: <organization id>' \
  -F 'audio=@/path/to/sample.wav;type=audio/wav' \
  -F 'clientRequestId=smoke-voice' \
  http://localhost:5088/api/AiAssistant/voice
```

Browser-format voice smoke:

```bash
ffmpeg -y -hide_banner -loglevel error \
  -i /path/to/sample.wav \
  -c:a libopus /tmp/cattle-track-sample.webm

curl -sS -b /tmp/cattle-track-cookies.txt \
  -H 'organizationId: <organization id>' \
  -F 'audio=@/tmp/cattle-track-sample.webm;type=audio/webm' \
  -F 'clientRequestId=smoke-voice-webm' \
  http://localhost:5088/api/AiAssistant/voice
```

Expected voice path:

```text
audio -> openai/whisper-large-v3-turbo -> ASR postprocess -> Qwen3.5 constrained JSON -> backend resolver/tool
```

## Known local trade-off

CPU ASR is a fallback. Production-like demos should use a GPU ASR endpoint and Qwen3.5-9B with constrained JSON output.
