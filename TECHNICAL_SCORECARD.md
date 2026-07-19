# Technical Scorecard

This file maps review criteria to repository evidence and verification commands.

## Fullstack Development

| Evidence | Location / command |
|---|---|
| Backend API | `backend/CAT` |
| Frontend app | `frontend` |
| Docker delivery | `docker-compose.yml`, `docker-compose.demo.yml`, `backend/CAT/Dockerfile`, `frontend/Dockerfile` |
| CI gates | `.github/workflows/ci.yml` |
| Backend tests | `dotnet test backend/CAT.Tests/CAT.Tests.csproj` |
| Frontend lint/build | `cd frontend && npm run lint && npm run build` |
| Demo DB seed | `backend/testdata`, `./scripts/verify-demo-db.sh` |

## AI Engineering

| Evidence | Location / command |
|---|---|
| AI architecture | `docs/ai/01-architecture-spec.md` |
| Tool catalog | `docs/ai/00-tool-catalog.md` |
| JSON Schema contracts | `ai-contracts/schemas/v1/tools` |
| Schema validation | `python3 scripts/validate_ai_contract_schemas.py` |
| Entity resolution contract | `ai-contracts/ENTITY_RESOLUTION.md` |
| AI audit design | `docs/ai-assistant-audit.md` |
| AI tests | `backend/CAT.Tests/Ai` |

## AI Usage During Development

| Evidence | Location |
|---|---|
| AI-assisted development process | `docs/ai-development/AI_USAGE.md` |
| Agent task log | `docs/ai-development/AGENT_TASK_LOG.md` |
| Human review protocol | `docs/ai-development/HUMAN_REVIEW_PROTOCOL.md` |
| Agent handoff guide | `agents.md` |

## Final Verification

```bash
./scripts/verify-all.sh
```
