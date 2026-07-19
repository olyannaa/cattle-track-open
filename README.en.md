# Cattle Track Open

[Русский](README.md) | **English**

Cattle Track: a fullstack livestock-management application with a Russian text/voice AI assistant for safe event entry.

## Contents

- `backend/CAT` - ASP.NET Core 8 API, PostgreSQL/Redis integration, business validation.
- `frontend` - React 19 + TypeScript + Vite + Ant Design application.
- `backend/CAT.Tests` - xUnit tests for the backend and AI assistant.
- `ai-contracts` - JSON Schema contracts for AI tools.
- `backend/testdata` - synthetic demo schema/seed data for local AI scenario checks.
- `docs/ai` - AI architecture, tool catalog, security findings, and implementation roadmap.
- `agent-work`, `docs/ai-development`, `agents.md` - AI-assisted development workflow and agent rules.

Runtime settings, datasets, and infrastructure parameters are connected through environment variables and external services.

## AI Assistant

The assistant uses a safe draft/confirm flow:

- read tools: animal search, animal card, parents, weight history, groups, cows for pregnancy check;
- write tools: weight, daily action, insemination;
- backend ASR-only voice path;
- constrained JSON output and runtime JSON Schema validation;
- exact DB resolver within the organization, without silent tag replacement;
- ambiguity UI for animal selection;
- idempotent confirm;
- AI action audit trail.

## Quick Start

```bash
cp .env.example .env
docker compose -f docker-compose.yml -f docker-compose.demo.yml up --build
```

Services:

- frontend: `http://localhost:3000/app/`
- API: `http://localhost:5001`
- PostgreSQL demo DB: `localhost:5433`

Run checks:

```bash
./scripts/verify-demo-db.sh
dotnet test backend/CAT.Tests/CAT.Tests.csproj
cd frontend && npm ci && npm run lint && npm run build
python3 scripts/validate_ai_contract_schemas.py
```

Full local verification:

```bash
./scripts/verify-all.sh
```
