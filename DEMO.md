# Demo Guide

This guide is a lightweight technical demo path for reviewers.

## Static Verification

```bash
python3 scripts/validate_ai_contract_schemas.py
dotnet test backend/CAT.Tests/CAT.Tests.csproj
cd frontend && npm ci && npm run lint && npm run build
```

## Local Services

```bash
cp .env.example .env
docker compose -f docker-compose.yml -f docker-compose.demo.yml up --build
```

Frontend: `http://localhost:3000/app/`  
Backend API: `http://localhost:5001`

## Review Scenarios

Use the synthetic demo organization from `backend/testdata`.

1. Read-only: `покажи группы`.
2. Read-only entity: `покажи карточку коровы 523`.
3. Ambiguity: request duplicate tag `523` and select one candidate.
4. Write draft: `запиши вес 421 кг корове 523 сегодня ручное взвешивание`.
5. Voice: send a browser/webm recording to `/api/AiAssistant/voice` with a configured ASR endpoint.

Expected behavior:

- read tools execute immediately;
- ambiguous entity asks for selection;
- write tools create a preview;
- no database write happens before confirm;
- commit returns an item-level report.

