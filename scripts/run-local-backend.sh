#!/usr/bin/env zsh
set -euo pipefail

cd "$(dirname "$0")/.."
source .env

# .env.local is ignored by git and keeps machine-specific database credentials
# out of command history and process arguments.
if [[ -f .env.local ]]; then
  source .env.local
fi

exec env \
  ASPNETCORE_URLS="http://127.0.0.1:${AI_BACKEND_PORT:-5088}" \
  ConnectionStrings__PostgresDB="$POSTGRES_CONNECTION_STRING" \
  RedisCacheOptions__Configuration="$REDIS_CONFIGURATION" \
  RedisCacheOptions__InstanceName="$REDIS_INSTANCE_NAME" \
  AiAssistant__Llm__Provider=ollama \
  AiAssistant__Llm__Endpoint="${AI_LLM_ENDPOINT:-http://127.0.0.1:11435/v1}" \
  AiAssistant__Llm__Model="${AI_LLM_MODEL:-qwen3.5:9b}" \
  AiAssistant__Llm__OutputMode="${AI_LLM_OUTPUT_MODE:-constrained-json}" \
  AiAssistant__Llm__MaxTokens="${AI_LLM_MAX_TOKENS:-256}" \
  AiAssistant__Asr__Provider=local-http \
  AiAssistant__Asr__Endpoint="${AI_ASR_ENDPOINT:-http://127.0.0.1:5092}" \
  AiAssistant__Asr__Model="${AI_ASR_MODEL:-openai/whisper-large-v3-turbo}" \
  dotnet backend/CAT/bin/Debug/net8.0/CAT.dll
