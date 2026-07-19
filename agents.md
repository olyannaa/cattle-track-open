# Agents Guide

Этот файл предназначен для будущих AI-агентов и разработчиков, которые продолжают работу над Cattle Track Fullstack.

## Проект

Cattle Track Fullstack - приложение для учета животных, хозяйств, групп, вакцинаций, веса, репродуктивных событий, ежедневных задач и AI-ассистента.

Основной git-репозиторий находится в корне checkout:

```bash
<repo-root>
```

Ключевые проектные документы находятся внутри репозитория:

- `docs/ai/00-tool-catalog.md` - каталог AI-инструментов и контрактов.
- `docs/ai/01-architecture-spec.md` - архитектурная спецификация.
- `docs/ai/02-research-plan.md` - исследовательский план.
- `docs/ai/03-domain-gaps.md` - доменные пробелы.
- `docs/ai/04-security-findings.md` - находки по безопасности.
- `docs/ai/05-implementation-roadmap.md` - дорожная карта.
- `TECHNICAL_SCORECARD.md` - карта доказательств для технической оценки.
- `DEMO.md` - технический demo path.

## Технологии

- Backend: ASP.NET Core 8, C#, Entity Framework Core, PostgreSQL, Redis, Swagger.
- Frontend: React 19, TypeScript 5.7, Vite 6, Ant Design, Redux Toolkit.
- Tests: xUnit для backend, `tsc`/Vite build для frontend.
- AI MVP: локальный/удаленный ASR, OpenAI-compatible LLM, JSON tool calling, contract schemas.

## Важные директории

- `backend/CAT` - ASP.NET Core API.
- `backend/CAT.Tests` - backend unit tests.
- `frontend` - React/Vite приложение.
- `frontend/src/app-service/services` - frontend API clients.
- `frontend/src/app/ai-event-input` - UI AI-ввода.
- `backend/CAT/Services/Ai` - backend AI orchestration, LLM/ASR clients, tool execution.
- `ai-contracts` - JSON schemas, validation rules, entity resolution contracts.
- `scripts` - benchmark, validation, ASR/LLM utilities.
- приватные datasets/experiments/deployment artifacts не публикуются в этом open repository.

## Перед началом работы

1. Проверь текущий статус:

```bash
git status --short
```

2. Не откатывай чужие незакоммиченные изменения. В этом репозитории часто есть активная незавершенная работа.
3. Перед изменениями прочитай ближайшие файлы и существующий стиль. Не вводи новую архитектуру без необходимости.
4. Не записывай реальные секреты в git. Используй `.env`, `.env.local`, переменные окружения или секреты CI.

## Локальный запуск

Frontend:

```bash
cd frontend
npm ci
VITE_API_URL=http://localhost:5088/api/ npm run dev -- --host 0.0.0.0
```

Backend:

```bash
./scripts/run-local-backend.sh
```

Альтернативный прямой запуск backend:

```bash
dotnet run --project backend/CAT/CAT.csproj
```

Полный AI MVP local run описан в `AI_MVP_LOCAL_RUN.md`. Production/runtime endpoints задаются только через environment variables.

## Проверки

Backend tests:

```bash
dotnet test backend/CAT.Tests/CAT.Tests.csproj
```

Frontend build:

```bash
cd frontend
npm run build
```

Frontend lint:

```bash
cd frontend
npm run lint
```

AI contract schemas:

```bash
python scripts/validate_ai_contract_schemas.py
```

AI contract schemas:

```bash
./scripts/verify-ai.sh
```

Запускай проверки, соответствующие изменению. Для узких правок достаточно targeted tests; для изменений в shared AI/backend/frontend flow лучше запускать несколько уровней.

## AI Assistant MVP

Ключевой пользовательский сценарий: фермер вводит текстом или голосом команду на русском языке, backend распознает намерение, резолвит сущности и выполняет безопасный read/write tool.

Ориентиры:

- Сохраняй совместимость с JSON schemas в `ai-contracts/schemas/v1`.
- Для write operations должны быть валидация, entity resolution, audit trail и понятный confirmation/report.
- Не доверяй LLM как источнику истины. Backend обязан валидировать tool args и права доступа.
- Для ASR учитывай доменные исправления и формат браузерной записи `webm/opus`.
- Не ломай constrained JSON / tool calling протоколы ради удобства UI.
- При работе с моделями смотри инструкции в `AI_MVP_LOCAL_RUN.md` и AI-документы в `docs/ai/`.

## Backend правила

- Контроллеры держи тонкими; бизнес-логику размещай в `Services`.
- Доступ к БД идет через EF контекст и существующие DAL/query patterns.
- Всегда учитывай `organizationId` и права пользователя.
- Для DTO используй существующие папки `Controllers/DTO`.
- Не добавляй миграции или seed-файлы без явного запроса: локальная PostgreSQL-схема в compose не создается автоматически.
- При изменениях API обновляй frontend service layer и, если нужно, README/AI docs.

## Frontend правила

- Используй существующие Ant Design, Redux Toolkit и service patterns.
- API-вызовы размещай в `frontend/src/app-service/services`.
- Не хардкодь backend URL; используй `VITE_API_URL`.
- Для новых экранов следуй структуре `frontend/src/app/<feature>`.
- UI должен быть рабочим с первого экрана, без лишних landing-page блоков.
- Проверяй, что текст не вылезает из кнопок, таблиц, карточек и модальных окон.

## Документация

При изменении поведения обновляй ближайший документ:

- `README.md` / `README.en.md` - общая разработка и запуск.
- `AI_MVP_LOCAL_RUN.md` - локальный AI runtime.
- `06-ai-assistant-mvp-use-cases.md` - use cases AI-ассистента.
- `docs/ai/*.md` - архитектура, каталог инструментов, research plan и roadmap.
- `docs/ai-development/*.md` - использование AI-инструментов при разработке.
- `ai-contracts/*.md` - контракты, entity resolution, validation rules.

## Git и рабочая директория

- Не используй destructive команды вроде `git reset --hard` или `git checkout --` без явного запроса.
- Если нужно менять файл с чужими незакоммиченными изменениями, сначала внимательно прочитай diff.
- В итоговом ответе всегда указывай, что было изменено и какие проверки запускались.

## Быстрый контекст для следующего агента

Если задача связана с AI:

1. Прочитай `06-ai-assistant-mvp-use-cases.md`.
2. Прочитай `docs/ai/00-tool-catalog.md` и `docs/ai/01-architecture-spec.md`.
3. Прочитай `ai-contracts/AI_VALIDATION_RULES.md`.
4. Проверь backend файлы в `backend/CAT/Services/Ai`.
5. Проверь frontend AI client `frontend/src/app-service/services/aiAssistant.ts`.
6. После изменений запусти релевантные tests/scripts из раздела "Проверки".

Если задача связана с обычным CRUD/UI:

1. Найди похожую страницу в `frontend/src/app`.
2. Найди соответствующий backend controller/service.
3. Сохрани существующий стиль DTO, validation и access checks.
4. Запусти frontend build или backend tests по зоне изменения.
