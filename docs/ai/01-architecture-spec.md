# Архитектурная спецификация AI-контура Cattle-Track

Что и как строим. Список инструментов — в `00-tool-catalog.md` (единственный источник истины, здесь не дублируется). Код: ветка `origin/CAT/1.2.1/main` в `cattle-track-backend` и `cattle_track_frontend`.

## 1. Что это

Детерминированный агент внутри Cattle-Track, который отвечает на вопросы о поголовье и выполняет команды на запись событий (текстом и голосом), не выходя за рамки существующей бизнес-логики системы. Не RAG-чат-бот. Ядро — надёжное сведение речи/текста к вызову инструмента из фиксированного набора с провалидированными аргументами и обязательным подтверждением перед любой записью.

## 2. Поток данных

```
[Текст] или [Голос → лёгкая ASR → детерминированный postprocessor → словарь хозяйства]
   → Agent Loop (LLM + tool schemas, bounded ReAct, ≤8 итераций)
      ├─ READ-тул  → выполнить сразу → результат в LLM → ответ
      │             (0/2+ совпадений → disambiguation)
      └─ WRITE-тул → entity resolution → бизнес-валидация → draft в Redis (TTL)
                    → preview пользователю → confirm → реальный сервис → аудит-лог
```

## 3. Agent Loop

Один плоский bounded ReAct-цикл. Без router/multi-agent, без графового фреймворка.

```
MAX_ITERATIONS = 8
MAX_CLARIFICATIONS_PER_ENTITY = 2

run(user_message, session):
    for i in range(MAX_ITERATIONS):
        resp = llm.call(SYSTEM_PROMPT, TOOLS_SCHEMA, session.history)   # constrained output (§6)
        audit.log_llm_turn(...)
        if resp.is_final_answer(): return resp.text
        call = resp.tool_call
        if session.is_duplicate(call): return abort_message           # анти-луп
        if call.name in READ:
            result = execute(call)
            if result.ambiguous or result.not_found:
                r = disambiguate(call, result, session)
                if r.needs_user_reply: return r.clarifying_question
            session.history.append(tool_result(call, result))
        else:  # WRITE
            draft = DraftStore.create(call, session)                   # НЕ пишет в БД
            return render_preview(draft)
    return iteration_limit_message
```

### 3.1 Disambiguation
- 0 совпадений → одна попытка переспроса → стоп;
- 1 → используется сразу;
- 2–5 → список с различающими полями (гурт, дата рождения, статус);
- >5 → просьба сузить критерий.
- Лимит: 2 уточнения на сущность, 2 суммарно на запрос.

### 3.2 Propose-then-commit (только WRITE)
Черновик — в Redis по confirm-токену с TTL (переиспользуется существующий паттерн `bot/start`→`bot/confirm`, новую таблицу не создаём). Commit — отдельный код-путь без LLM:

```
POST /agent/drafts/{id}/confirm
  if draft.status != PENDING or expired: 410
  resolved, unresolved = split_by_resolution(draft.items)
  if unresolved and not user_confirmed_partial:
      return preview_with_warning(resolved, unresolved)   # три корзины, не тихий skip
  for item in confirmed_resolved:
      revalidate_preconditions(item)
      execute_write(item, idempotency_key=item.key)        # реальный сервис
  audit.log_commit(...)
  return per_item_report(...)                              # по мотивам HTTP 207
```

Батч с частичным резолвингом («переведи быков X и Y», один не найден): три корзины — резолвлено / неоднозначно / не найдено. Никогда не пропускать тихо и не блокировать весь батч из-за одной ошибки.

## 4. Разрешение сущностей
- Животное по бирке — точное совпадение в рамках `OrganizationId` (как `GetAnimalIdByTag`); fuzzy — только в слое коррекции ASR (§6.2), не в БД.
- Препарат — резолвинг имени в `Medicine.Id`; не найден → предложить создать или оставить текстом.

## 5. Валидационный слой

Правила, скрытые в сервисном коде (не в DTO-атрибутах), дублируются в JSON Schema и отдельном валидаторе. Полный перечень каскадов и enum'ов — в `00-tool-catalog.md` (колонка «Примечание», разделы 3–5). Дополнительно (в backend отсутствует, добавляем в AI-слое): диапазон веса, дата не в будущем / не раньше рождения, защита от дубля события, проверка суммы процентов кормления = 100%, org-scope для delete-операций групп.

Два обязательных барьера перед подтверждением: (1) проверка JSON Schema/enum, при необходимости поддержанная constrained decoding выбранного runtime, (2) детерминированный бизнес-валидатор. Один retry к LLM допустим только для исправимой ошибки структуры и только в общем latency-бюджете. Затем — человек.

## 6. Компоненты и production-ограничения

Финальный выбор исследования от 2026-07-13: ASR — `openai/whisper-large-v3-turbo` через `faster-whisper 1.2.1` FP16; LLM — `Qwen3.5-9B Q4_K_M` через Ollama и constrained JSON Schema; fallback — `Qwen3.5-4B Q4_K_M`. Backend по-прежнему использует сменные HTTP adapters, а бизнес-валидатор остаётся источником истины.

Native Ollama tool API не используется в production path: в финальном прогоне он давал воспроизводимые HTTP 500. Строгая JSON Schema обязательна внутри bounded agent loop. Выбор моделей закрыт, но rollout выполняется только после batch normalizer, backend-driven clarification, state-based Postgres test и UI E2E.

Целевая среда: Ryzen 7 7700, 61 GiB RAM, RTX 5070 12 GiB VRAM. ASR и LLM должны постоянно помещаться вместе, без выгрузки одной модели ради каждого запроса.

| Слой | Production shortlist | Baseline / ablation |
|---|---|---|
| ASR | Whisper-large-v3-turbo / faster-whisper FP16 | GigaAM-v3 исключён по качеству бирок; Whisper-large-v3 остаётся historical quality ceiling |
| Коррекция сущностей | детерминированный postprocessor + словарь хозяйства | LLM-коррекция только для неоднозначного случая и только после latency/FPR ablation |
| LLM tool-calling | Qwen3.5-9B Q4_K_M; Qwen3.5-4B Q4_K_M как fallback | T-lite-it-2.1, T-pro-it-2.1 и старые Qwen остаются historical baselines |
| Structured output | Ollama constrained JSON Schema + schema validator + business validator | native Ollama tool API исключён из-за HTTP 500 |
| TTS | Silero v5 на CPU, опциональный hands-busy режим | без TTS |

Production latency gates:

- ASR p95 после остановки записи ≤1.5 с;
- полный LLM tool-call p95 ≤5 с;
- voice-to-preview p95 ≤8 с;
- обычный запрос не вызывает unload/reload ASR или LLM;
- при статистически близком качестве выбирается меньшая и более быстрая модель.

## 7. Аудит
Расширяем существующий `UserActionQueue`/`IUserActionService` (`Channel<T>` + `BackgroundService`, уже в проекте) новыми `actionType`: `ai_llm_turn`, `ai_tool_call`, `ai_draft_created`, `ai_clarification`, `ai_commit`, `ai_loop_guard`. В `AdditionalInfo` (jsonb): исходный текст, намерение, аргументы, confidence, правки пользователя.

## 8. Интеграция с кодом

**Backend**: `Controllers/AiAssistantController.cs` → `IAiAssistantService`/`AiAssistantService` → `PostgresContext.AiAssistant.cs` (partial, по образцу `PostgresContext.LogUserAction.cs`). Все эндпоинты: `[Authorize] + [OrgValidationTypeFilter(checkOrg: true)]`, `OrganizationId` прокидывается явно (глобального query filter в проекте нет). LLM/ASR-клиенты — singleton-обёртки по образцу `MinioS3Service`.

**Frontend**: модуль `src/app/ai-event-input/` + RTK Query сервис `src/app-service/services/aiAssistant.ts`. Результат парсинга не рисует новые поля, а маппится в `form.setFieldsValue(...)` существующих форм (`FormAddWeight`, `FormAddTreatment`, `InseminationForm`). Неоднозначная бирка — через существующие `InputSearch`/`SelectFilters`. Лог диалога — по паттерну `Events.tsx` (`Collapse`).

## 9. Ограничения реализации
- Мультитенантность и авторизация — строго как в проекте (вручную по `OrganizationId`, доступ через `OrgValidationTypeFilter`).
- Фон при необходимости — `Channel<T>` + `BackgroundService`, не новые очереди (Hangfire/Kafka).
- Не делаем: дообучение своих ASR/LLM, LangGraph/подобное, self-consistency, MCP-сервер (все — с обоснованием в `02-research-plan.md` и `03-domain-gaps.md`).
- Не считаем модель выбранной только по WER/tool accuracy: обязательны latency, VRAM, cold/warm и end-to-end измерения на целевом сервере.
- Не передаём LLM UUID, idempotency keys и вычисляемые backend-поля как задачу генерации; это ответственность resolver/backend.
- Session history ограничивается релевантными сообщениями и измеряется на 1/4/8/12 репликах, чтобы память диалога не разрушала TTFT.

## 10. MVP-скоуп
Строим и бенчмаркаем инструменты с меткой `[MVP]` из `00-tool-catalog.md` (6 read + 3 write). Полный каталог реализуется тем же паттерном по мере времени. На защите: полный каталог — как охват продукта, бенчмарк — на MVP-подмножестве. Не пытаться довести и измерить все ~55 тулов за 2 недели.
