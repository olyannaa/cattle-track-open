# Cattle Track Open

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![React](https://img.shields.io/badge/React-19-61DAFB?logo=react&logoColor=111111)
![TypeScript](https://img.shields.io/badge/TypeScript-5.7-3178C6?logo=typescript&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-4169E1?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-ready-2496ED?logo=docker&logoColor=white)

**Русский** | [English](README.en.md)

Cattle Track: fullstack-приложение для учета животных и хозяйственных событий с AI-ассистентом для русскоязычных текстовых и голосовых команд.

## Что внутри

- `backend/CAT` - ASP.NET Core 8 API, PostgreSQL/Redis integration, бизнес-валидация.
- `frontend` - React 19 + TypeScript + Vite + Ant Design application.
- `backend/CAT.Tests` - xUnit tests, включая AI loop, schema gate, resolver и write safety.
- `ai-contracts` - JSON Schema контракты AI tools.
- `backend/testdata` - синтетическая demo schema/seed для локальной проверки AI-сценариев.
- `docs/ai` - архитектура AI-контура, каталог tools, security findings и roadmap.
- `agent-work`, `docs/ai-development`, `agents.md` - процесс AI-assisted разработки и правила работы агентов.

Runtime-настройки, датасеты и инфраструктурные параметры подключаются через environment variables и внешние сервисы.

## AI-ассистент

MVP ассистента работает через безопасный draft/confirm flow:

- read tools: поиск животного, карточка, родители, история веса, список групп, коровы для проверки стельности;
- write tools: вес, ежедневное действие, осеменение;
- ASR-only voice path на backend стороне;
- constrained JSON output и runtime JSON Schema validation;
- exact DB resolver внутри организации, без молчаливой подмены бирок;
- ambiguity UI для выбора животного;
- idempotency для повторного confirm;
- audit trail AI-действий.

Ключевые документы:

- [docs/ai/01-architecture-spec.md](docs/ai/01-architecture-spec.md)
- [docs/ai/00-tool-catalog.md](docs/ai/00-tool-catalog.md)
- [ai-contracts/ENTITY_RESOLUTION.md](ai-contracts/ENTITY_RESOLUTION.md)
- [docs/ai-development/AI_USAGE.md](docs/ai-development/AI_USAGE.md)
- [agent-work/README.md](agent-work/README.md)

## Быстрый старт

### Полностью локальный demo-запуск

```bash
cp .env.example .env
docker compose -f docker-compose.yml -f docker-compose.demo.yml up --build
```

Сервисы:

- frontend: `http://localhost:3000/app/`
- API: `http://localhost:5001`
- PostgreSQL demo DB: `localhost:5433`

Demo DB использует только синтетические данные из `backend/testdata`.

### Проверка demo seed

```bash
./scripts/verify-demo-db.sh
```

### Локальная разработка

Backend:

```bash
dotnet test backend/CAT.Tests/CAT.Tests.csproj
```

Frontend:

```bash
cd frontend
npm ci
npm run lint
npm run build
```

AI contracts:

```bash
python3 scripts/validate_ai_contract_schemas.py
```

Полная локальная проверка:

```bash
./scripts/verify-all.sh
```

## Переменные окружения

Все реальные секреты должны передаваться только через environment variables. В репозитории есть только шаблон `.env.example`.

| Переменная | Назначение |
| --- | --- |
| `POSTGRES_CONNECTION_STRING` | строка подключения PostgreSQL для обычного compose-запуска |
| `TELEGRAM_BOT_API_KEY` | токен Telegram-бота, если используется |
| `FRONTEND_PUBLIC_URL` | публичный URL frontend |
| `VITE_API_URL` | URL backend API для frontend |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT` | локальная demo-БД |
| `AI_LLM_PROVIDER`, `AI_LLM_ENDPOINT`, `AI_LLM_MODEL` | LLM runtime wiring |
| `AI_ASR_PROVIDER`, `AI_ASR_ENDPOINT`, `AI_ASR_MODEL` | ASR runtime wiring |

## Качество

В проекте поддерживаются:

- backend unit tests для AI-loop, schema gate, resolver и write safety;
- frontend lint/build gate;
- JSON Schema validation для AI tools;
- Docker build для backend;
- demo seed для воспроизводимой локальной проверки.
