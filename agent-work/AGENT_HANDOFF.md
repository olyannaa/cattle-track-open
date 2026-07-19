# Agent Handoff

## Репозиторий

Рабочий monorepo:

```text
cattle-track-fullstack/
```

Ключевые части:

- `frontend/` - React/Vite приложение;
- `backend/CAT/` - ASP.NET Core API;
- `backend/CAT.Tests/` - xUnit тесты;
- `docs/ai/` - архитектура AI-ассистента и tool catalog;
- `ai-contracts/` - JSON Schema контракты AI-инструментов;
- `scripts/` - проверки и AI benchmark utilities.

## Перед работой

```bash
git status --short
```

Если есть чужие изменения, не откатывать их. Для правок в UI обязательно проверять реальное отображение.

## Частые команды

```bash
docker compose up -d --build
./scripts/verify-all.sh
./scripts/verify-ai.sh
```

Frontend:

```bash
cd frontend
npm run build
```

Backend:

```bash
dotnet test backend/CAT.Tests/CAT.Tests.csproj
```

## Текущий фокус качества

- Репозиторий должен быть понятен внешнему эксперту.
- Локальный запуск должен быть простым через Docker Compose.
- AI-ассистент должен иметь проверяемые контракты, backend validation и аудит.
- UI-правки должны проверяться визуально, особенно навигация и основное приложение.
