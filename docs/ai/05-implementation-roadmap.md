# Roadmap переделки Cattle-Track под детерминированный AI-контур

Документ описывает порядок работ по превращению `cattle-track-fullstack` в систему с детерминированным агентом, описанным в `01-architecture-spec.md`, с исследованиями из `02-research-plan.md` и каталогом инструментов из `00-tool-catalog.md`.

Цель roadmap: не просто "прикрутить чат", а поэтапно построить проверяемый AI-контур, который:
- отвечает на вопросы о хозяйстве через фиксированный набор read-тулов;
- выполняет write-команды только через draft/preview/confirm;
- не обходит существующую авторизацию, мультитенантность и бизнес-логику;
- имеет измеримые результаты по ASR, tool-calling, structured output и валидатору;
- обеспечивает измеримый production-бюджет скорости и совместное размещение ASR+LLM на целевом сервере;
- честно разделяет реализованный AI-контур, полный каталог тулов и будущие доменные доработки.

## 0. Исходные документы и границы

### Входные документы

- `00-tool-catalog.md` — единый источник истины по инструментам агента.
- `01-architecture-spec.md` — архитектура agent loop, draft/confirm, validation, audit, интеграция backend/frontend.
- `02-research-plan.md` — программа исследований и критерии выбора ASR/LLM/валидатора.
- `03-domain-gaps.md` — честный список того, чего в системе пока нет и что не надо выдавать за готовые тулы.
- `04-security-findings.md` — security-находки, которые надо учитывать отдельно от AI-проекта.

### Главные ограничения

- Не строим RAG-бота.
- Не строим multi-agent/router/LangGraph.
- Не делаем MCP-сервер внутри продукта.
- Не дообучаем свои ASR/LLM в рамках текущего этапа.
- Не фиксируем конкретную модель до quality + latency + resource + end-to-end экспериментов на RTX 5070.
- Не используем 32B-модель в production на 12 GiB VRAM, если она требует регулярной выгрузки ASR/LLM.
- Не даем LLM прямой путь к записи в БД.
- Не добавляем административные тулы: организации, сотрудники, роли, приглашения, аутентификация.
- Все операции строго в рамках `OrganizationId`.

### Ключевой AI-контур

Ключевой AI-контур реализуется и бенчмаркается первым. Полный каталог остается как архитектурный охват продукта.

Read:
- `find_animal`
- `get_animal_card`
- `get_animal_parents`
- `get_weight_history`
- `get_pregnancies_to_check`
- `list_groups`

Write:
- `create_weight`
- `create_daily_action`
- `create_insemination`

## 1. Фаза ориентации и инвентаризации проекта

Цель: зафиксировать реальное состояние `cattle-track-fullstack`, чтобы будущая архитектура опиралась на код, а не на предположения.

### 1.1 Backend-аудит

Работы:
- найти фактические контроллеры, DTO, сервисы и EF-модели для ключевых инструментов;
- проверить текущие маршруты, request/response-контракты и enum-значения;
- выписать бизнес-правила, которые живут внутри сервисов, а не в DTO;
- отдельно проверить места, где write-операции вызывают каскады: движение животного, выбытие, осеменение, беременность, daily actions;
- проверить существующий паттерн `bot/start` -> `bot/confirm`, если он используется для Redis draft/confirm;
- проверить `UserActionQueue`, `IUserActionService`, `BackgroundService` и формат `AdditionalInfo`.

Результаты:
- таблица соответствия `tool -> endpoint -> DTO -> service -> hidden rules`;
- список мест, где AI-слой должен переиспользовать существующий сервис;
- список правил, которые нужно продублировать в AI-валидаторе;
- список мест, где backend надо расширить до начала agent work.

### 1.2 Frontend-аудит

Работы:
- найти существующие формы: `FormAddWeight`, `FormAddTreatment`, `InseminationForm`;
- проверить, как сейчас устроены `InputSearch`, `SelectFilters`, `Events.tsx`, `Collapse`, RTK Query services;
- понять, какие формы можно заполнять через `form.setFieldsValue(...)`;
- определить, где должен жить новый модуль `src/app/ai-event-input/`;
- проверить существующие UX-паттерны подтверждения, ошибок, модалок, loading-состояний.

Результаты:
- карта `AI draft -> existing form fields`;
- список компонентов, которые можно переиспользовать;
- список недостающих UI-состояний: preview, ambiguous, not found, partial batch, confirm expired, commit report.

### 1.3 Security baseline

Работы:
- подтвердить находки из `04-security-findings.md` на текущей ветке;
- отдельно завести задачи на дефекты, не связанные напрямую с AI;
- до реализации delete-тулов убедиться, что org-check есть либо в backend, либо в AI-сервисе.

Результаты:
- зафиксированный security backlog;
- решение: что чинится до релиза, что не блокирует AI-контур, что запрещено оборачивать в тулы.

## 2. Фаза проектирования AI-контракта

Цель: описать машинные контракты так, чтобы LLM работала только с фиксированными схемами, а бизнес-логика оставалась детерминированной.

### 2.1 Tool schemas

Работы:
- для ключевых инструментов описать JSON Schema аргументов;
- для enum'ов добавить escape-значение или безопасный fallback, как указано в архитектуре;
- определить обязательные и условные поля;
- для batch-write явно описать массивы, частичные ошибки и idempotency keys;
- завести версионирование схем.

Результаты:
- каталог JSON Schema для ключевых инструментов;
- документ с правилами совместимости схем;
- тесты на валидность схем.

### 2.2 Нормализация доменных сущностей

Работы:
- описать единый формат `AnimalRef`, `GroupRef`, `MedicineRef`, `DateRef`;
- решить, какие поля приходят от LLM, а какие резолвятся backend-слоем;
- зафиксировать правило: бирка ищется точным совпадением в рамках `OrganizationId`;
- fuzzy-match оставить только для коррекции ASR, не для поиска в БД;
- описать формат disambiguation-кандидатов.

Результаты:
- спецификация entity resolution;
- единые response-модели для `resolved`, `ambiguous`, `not_found`;
- лимиты уточнений: 2 на сущность, 2 суммарно на запрос.

### 2.3 Валидационный слой

Работы:
- вынести бизнес-валидацию write-тулов в отдельный AI validator;
- продублировать скрытые правила из backend-сервисов;
- добавить AI-only sanity checks: вес, дата, дубль события, проценты кормления, org-scope;
- определить один retry к LLM после ошибки валидации;
- описать финальное поведение после retry: показать человеку понятную ошибку/preview.

Результаты:
- `AiToolValidator` или аналогичный сервис;
- набор unit-тестов на валидные и невалидные аргументы;
- таблица `rule -> source -> test`.

## 3. Фаза данных и датасета

Цель: собрать собственный доменный датасет, на котором можно честно выбирать ASR/LLM и защищать решения.

### 3.1 Общий датасет команд и вопросов

Объем: примерно 170-220 текстовых примеров для LLM tool-calling.

Страты:
- single-read: 35-40;
- нет подходящего тула: 10-12;
- multi-hop read: 20-25;
- single-write с отменой/правкой перед подтверждением: 35-40;
- batch-write, включая частично невалидные: 20-25;
- adversarial/неоднозначность: 15-20;
- fault-injection: 30-40.

Работы:
- собрать реальные формулировки на русском языке;
- включить разговорные, короткие, ошибочные и неоднозначные команды;
- добавить варианты с бирками, кличками, группами, препаратами, датами;
- для каждого примера разметить golden tool call, аргументы, ожидаемый результат;
- для write-кейсов описать ожидаемое состояние БД после commit;
- отделить train/dev/test по смыслу, даже если дообучения нет.

Результаты:
- private tool-calling dataset;
- private fault-injection dataset;
- README датасета: формат, страты, ограничения, правила разметки.

### 3.2 ASR-датасет

Объем: 60-80 фраз из общего датасета.

Работы:
- записать несколько голосов;
- записать варианты в тишине и с фермерским шумом;
- сделать golden-транскрипт;
- разметить слова категориями `number`, `proper_noun`, `other`;
- включить бирки, клички, препараты, даты и группы.

Результаты:
- аудиофайлы;
- golden-транскрипты;
- разметка entity-level;
- скрипт расчета WER/entity WER/exact-match бирки.

### 3.3 Тестовая база

Работы:
- подготовить seed-данные для организации, животных, групп, весов, событий, осеменений;
- обеспечить повторяемость тестов;
- сделать фикстуры для ambiguity: 0 совпадений, 1 совпадение, 2-5 совпадений, больше 5;
- сделать фикстуры для частично невалидного batch.

Результаты:
- reproducible seed;
- отдельная тестовая организация;
- инструкции запуска интеграционных и state-based тестов.

## 4. Фаза исследований

Цель: выбрать компоненты AI-контура на своих данных, а не на рекламных или сторонних бенчмарках.

### 4.1 Исследование ASR

Кандидаты:
- GigaAM-v3 e2e RNNT/CTC;
- Whisper-large-v3-turbo;
- Canary-1B-v2;
- Whisper-large-v3 только как quality ceiling.

Метрики:
- общий WER;
- entity-level WER по `number`, `proper_noun`, `other`;
- exact-match бирки целиком;
- latency p50/p95/max, RTF, cold start;
- VRAM/RAM peak;
- latency при одновременно загруженной LLM;
- end-to-end `stop recording -> transcript -> preview`.

Результаты:
- таблица результатов с доверительными интервалами;
- решение по ASR, runtime и режиму загрузки;
- список ошибок, критичных для сельхоз-домена.

Gate: ASR p95 ≤1.5 с после остановки записи и совместное размещение с LLM без unload/reload.

### 4.2 Исследование доменной коррекции ASR

Сравнение:
- сырой транскрипт лучшей ASR;
- транскрипт + детерминированный postprocessor;
- postprocessor + fuzzy-match по словарю хозяйства;
- LLM-коррекция только для неоднозначных сущностей как отдельная ablation.

Метрики:
- entity-level WER;
- exact-match бирки;
- false positive rate коррекции.
- добавленная latency p50/p95.

Решение:
- детерминированный postprocessor остаётся при значимом приросте и низком FPR;
- LLM-коррекция не попадает в hot path, если нарушает voice-to-preview budget.

### 4.3 Исследование LLM tool-calling

Кандидаты:
- T-lite-it-2.1 8B Q5_K_M;
- Qwen3.5-4B Q5/Q4 в text-only runtime;
- Qwen3.5-9B Q5/Q4 в text-only runtime;
- Ministral-3-8B-Instruct-2512 как второй приоритет;
- T-pro-it-2.1/Qwen3-32B только как historical quality/latency baseline.

Метрики:
- tool selection accuracy;
- argument exact-match;
- state-based success для write;
- strict/partial batch success;
- pass@1 и pass^3;
- Wilson confidence intervals.
- multi-turn carry-over success;
- safe clarification accuracy;
- TTFT и полный tool-call p50/p95/max;
- tokens/s, VRAM/RAM peak, cold load;
- text-to-preview и voice-to-preview;
- совместная работа с загруженной ASR.

Результаты:
- таблица по всем стратам;
- error analysis;
- выбор основной модели;
- выбор quantization, context limit и runner;
- список классов запросов, которые надо уточнять у пользователя.

Gate: полный tool-call p95 ≤5 с; state-based write success не хуже лучшего кандидата более чем на 5 п.п.; модель и ASR постоянно помещаются в 12 GiB VRAM.

### 4.4 Исследование constrained output и валидатора

Ablation:
- свободная генерация;
- native tool calling;
- constrained output через фактически выбранный runner (Ollama/llama.cpp, SGLang или vLLM);
- constrained output + бизнес-валидатор.

Метрики:
- catch rate испорченных write-вызовов;
- false positive rate;
- confusion matrix.
- schema-valid rate и добавленная latency p50/p95.

Результаты:
- доказательство пользы constrained decoding;
- доказательство пользы бизнес-валидатора;
- набор regression-тестов из найденных ошибок.

Решение: `vLLM + XGrammar` больше не считается заранее обязательной связкой. Остаётся runner, который проходит одновременно schema-valid, latency и resource gates на целевом сервере.

### 4.5 Исследование TTS

Кандидат:
- Silero v5.

Работы:
- измерить latency/RTF на целевом железе;
- проверить сценарий hands-busy;
- сформулировать trade-off: когда TTS включать, когда достаточно текста.

Результаты:
- короткий технический отчет;
- решение: включать ли TTS в demo или оставить как опциональный режим.

Текущий статус: Silero v5 уже прошёл локальный warm benchmark; повторить smoke на Ryzen 7 7700 и держать TTS на CPU, не занимая VRAM.

### 4.6 Совместный production benchmark

Работы:
- держать ASR и LLM загруженными одновременно;
- прогнать текстовые, голосовые, multi-turn, ambiguity и write-preview сценарии через реальные backend endpoints;
- измерить p50/p95/max по этапам ASR, LLM, resolver/validator и общий end-to-end;
- проверить два последовательных и два параллельных запроса;
- проверить рост latency на истории 1/4/8/12 сообщений;
- зафиксировать GPU/RAM peak, cold recovery и timeout rate.

Результаты:
- выбранная связка `ASR + LLM + quantization + runner + параметры`;
- startup/deployment config;
- rollback на текущие baseline-модели;
- доказательство, что обычный запрос не вызывает выгрузку модели.

## 5. Фаза backend AI-контура

Цель: реализовать серверный AI-контур для ключевых инструментов без прямой записи LLM в БД.

### 5.1 Каркас AI-модуля

Работы:
- добавить `AiAssistantController`;
- добавить `IAiAssistantService` / `AiAssistantService`;
- добавить partial `PostgresContext.AiAssistant.cs`, если нужны AI-specific queries;
- подключить `[Authorize]` и `[OrgValidationTypeFilter(checkOrg: true)]`;
- явно прокидывать `OrganizationId` во все операции;
- добавить конфигурацию LLM/ASR-клиентов по паттерну существующих singleton-сервисов.

Результаты:
- endpoint для текстового запроса;
- endpoint для draft confirm;
- базовая инфраструктура с DI и конфигом.

### 5.2 Agent Loop

Работы:
- реализовать bounded ReAct loop с `MAX_ITERATIONS = 8`;
- добавить anti-loop guard на повторный tool call;
- добавить session history;
- добавить constrained output интеграцию;
- добавить обработку final answer/tool call/tool result;
- добавить единый формат ошибок.

Результаты:
- agent loop работает на mock LLM;
- тесты на limit, duplicate call, final answer, failed tool.

### 5.3 Read-тулы AI-контура

Работы по тулам:
- `find_animal`: точный поиск бирки в рамках организации, disambiguation;
- `get_animal_card`: получение карточки животного;
- `get_animal_parents`: multi-hop read бирка -> родители -> бирки родителей;
- `get_weight_history`: история веса;
- `get_pregnancies_to_check`: список коров для диагностики стельности;
- `list_groups`: список групп.

Результаты:
- read-тулы выполняются сразу;
- ambiguity/not found не маскируются;
- ответы адаптированы для текста и голоса.

### 5.4 Write-тулы AI-контура

Работы по тулам:
- `create_weight`: вес, дата, метод, проверка веса и даты;
- `create_daily_action`: batch по `tags[]`, enum type, medicine/dose/group transitions;
- `create_insemination`: batch по `cow_tags[]`, тип осеменения, быки/партия спермы, каскад беременности.

Общие работы:
- entity resolution до создания draft;
- split batch на `resolved`, `ambiguous`, `not_found`;
- AI-validator до preview;
- draft в Redis с TTL;
- preview без записи в БД;
- confirm endpoint без LLM;
- повторная валидация перед commit;
- idempotency key на item;
- per-item report по мотивам HTTP 207.

Результаты:
- write-операции невозможны без confirm;
- частично невалидный batch не пропадает тихо;
- commit идет только через реальные сервисы backend.

### 5.5 Audit

Работы:
- расширить `UserActionQueue` / `IUserActionService`;
- добавить action types:
  - `ai_llm_turn`;
  - `ai_tool_call`;
  - `ai_draft_created`;
  - `ai_clarification`;
  - `ai_commit`;
  - `ai_loop_guard`;
- описать JSON в `AdditionalInfo`;
- скрывать или минимизировать чувствительные данные, если они появятся.

Результаты:
- полный след agent loop;
- полный след draft/confirm/commit;
- возможность разбирать ошибки после demo и экспериментов.

## 6. Фаза frontend AI-контура

Цель: добавить пользовательский AI-ввод без переписывания существующих форм.

### 6.1 API layer

Работы:
- добавить `src/app-service/services/aiAssistant.ts`;
- описать RTK Query endpoints:
  - send text;
  - upload/send voice, если голос входит в текущий scope;
  - confirm draft;
  - cancel draft;
  - fetch draft preview, если нужно;
- типизировать ответы: final answer, clarification, preview, commit report, error.

Результаты:
- frontend умеет общаться с AI backend;
- ошибки backend отображаются контролируемо.

### 6.2 Модуль `ai-event-input`

Работы:
- создать `src/app/ai-event-input/`;
- добавить поле текстового ввода;
- добавить voice input только после готовности ASR-части;
- показать историю диалога по паттерну `Events.tsx` / `Collapse`;
- добавить состояния loading, clarification, preview, confirm, expired, partial success.

Результаты:
- пользователь может задать вопрос или команду;
- read-ответ показывается сразу;
- write-команда показывает preview и требует подтверждения.

### 6.3 Интеграция с существующими формами

Работы:
- маппить результат парсинга в `form.setFieldsValue(...)`;
- не создавать параллельные формы для веса/лечения/осеменения;
- для неоднозначной бирки использовать существующие `InputSearch`/`SelectFilters`;
- показать предупреждения по partial batch.

Результаты:
- AI помогает заполнить существующие формы;
- пользователь видит привычные поля и может поправить значения перед confirm.

## 7. Фаза тестирования качества и надежности

Цель: доказать, что AI-контур не ломает учетную систему и не пишет данные без контроля.

### 7.1 Unit-тесты

Покрыть:
- JSON Schema;
- entity resolution;
- business validator;
- idempotency key generation;
- draft TTL/status transitions;
- disambiguation limits;
- enum/cascade rules.

### 7.2 Integration-тесты backend

Покрыть:
- read-тулы на seed-базе;
- write preview без записи;
- confirm с записью;
- expired draft;
- duplicate confirm;
- partial batch;
- org-scope;
- audit events.

### 7.3 State-based тесты

Покрыть:
- после `create_weight` в БД есть корректное взвешивание;
- после `create_daily_action` есть событие и выполнен нужный каскад;
- после `create_insemination` создана нужная репродуктивная запись и связанное состояние;
- при невалидных аргументах состояние БД не меняется.

### 7.4 E2E-тесты frontend

Покрыть:
- read-вопрос;
- single write preview -> confirm;
- write preview -> cancel;
- ambiguous animal;
- partial batch;
- expired draft;
- backend validation error.

## 8. Фаза отчета и защиты решений

Цель: подготовить доказательную базу, почему архитектура и компоненты выбраны именно так.

Работы:
- собрать результаты исследований в едином формате: гипотеза -> метод -> таблица -> решение -> ограничения;
- показать, что сторонние бенчмарки использовались только для выбора кандидатов;
- отдельно показать вклад constrained output и валидатора;
- отдельно показать ограничения выборки;
- подготовить error analysis;
- подготовить demo-сценарии.

Результаты:
- исследовательский отчет;
- таблицы с доверительными интервалами;
- список ограничений;
- demo script.

## 9. Расширение после базового контура

Цель: постепенно покрыть полный каталог из `00-tool-catalog.md` тем же паттерном.

Порядок расширения:
1. Животные и веса: `get_animal_events`, `list_animals`, `count_animals`, `get_last_weight`, `get_weight_statistics`.
2. Daily actions и препараты: `list_daily_actions`, `count_daily_actions`, medicine CRUD, delete/update только после проверки org-scope.
3. Репродукция: pregnancy diagnosis, calving, cow/bull lists.
4. Группы и идентификация: create/edit/delete group, group types, identification fields.
5. Кормление: read-аналитика, компоненты, рационы, назначение рациона, record feeding.
6. KPI/аналитика: агрегации и ссылки на графики.

Правило расширения:
- каждый новый write-тул проходит через schema -> validator -> draft -> preview -> confirm -> commit -> audit;
- каждый новый read-тул проходит через schema -> org-scope -> disambiguation, если есть сущности;
- каждый новый тул получает тесты и минимум несколько dataset examples.

## 10. Разрез работ по тулам

### Ключевой AI-контур read-тулы

| Тул | Этапы работ | Датасет | Тесты |
|---|---|---|---|
| `find_animal` | schema, resolver, ambiguity UI, audit | single-read, ambiguity, ASR бирки | 0/1/2-5/>5 совпадений |
| `get_animal_card` | schema, endpoint adapter, response summary | single-read | org-scope, not found |
| `get_animal_parents` | multi-hop orchestration, summary | multi-hop read | нет родителей, один родитель, оба родителя |
| `get_weight_history` | adapter, voice-friendly summary | single-read | пустая/полная история |
| `get_pregnancies_to_check` | adapter, summary/list mode | single-read | пустой список, несколько записей |
| `list_groups` | adapter, group summary | single-read | группы разных типов |

### Ключевой AI-контур write-тулы

| Тул | Этапы работ | Датасет | Тесты |
|---|---|---|---|
| `create_weight` | schema, tag resolver, validator, draft, confirm, commit | single-write, fault-injection | вес <=0, дата в будущем, дубль |
| `create_daily_action` | schema, batch resolver, enum cascade, partial preview, commit | batch-write, adversarial, fault-injection | перевод, выбытие, препарат, partial batch |
| `create_insemination` | schema, cow/bull resolver, validator, draft, commit | single-write, batch-write | тип осеменения, неизвестная корова, каскад беременности |

### Дальнейшие группы тулов

| Группа | Когда делать | Особые риски |
|---|---|---|
| Medicine CRUD | после daily action flow | дубли имен, свободный текст vs Guid |
| Reproduction full | после `create_insemination` | каскады беременности/отела |
| Feeding | отдельной фазой | проценты 0-100, сумма 100%, неочевидные конверсии |
| Group delete/edit | после security fixes | org-scope, блокировки удаления |
| KPI analytics | после read-тулов | часть запросов надо отвечать ссылкой на график |

## 11. Параллельные треки

### Трек A: Research

- датасет;
- ASR;
- коррекция;
- LLM tool-calling;
- constrained output;
- validator ablation;
- TTS.
- resource smoke и отсев кандидатов;
- совместный ASR+LLM benchmark;
- multi-turn benchmark;
- end-to-end latency decomposition.

### Трек B: Backend

- contracts;
- agent loop;
- tool executor;
- validators;
- draft/confirm;
- audit;
- tests.

### Трек C: Frontend

- RTK Query;
- AI input module;
- preview/confirm UI;
- integration with existing forms;
- E2E.

### Трек D: Security and product boundaries

- security fixes from `04-security-findings.md`;
- explicit "not a tool" list from `03-domain-gaps.md`;
- documentation of limitations;
- demo wording without overclaiming.

## 12. План повторного model-selection и миграции

Этот блок заменяет старую последовательность model research для уже собранного AI-контура. Backend/frontend-функции и датасеты не пересобираются с нуля: сначала актуализируются evaluation strata, затем меняется только model/runtime слой и связанные prompt/parser/config.

### Этап R1 — подготовка, 1–2 дня

- заморозить текущие результаты как historical baseline;
- добавить multi-turn/session cases в dev/test;
- проверить golden resolver-aware contracts;
- добавить единый сбор TTFT, p95, VRAM/RAM и end-to-end timings;
- зафиксировать версии CUDA/driver/runtime и состояние RTX 5070.

### Этап R2 — быстрый отсев, 1–2 дня

- установить кандидатов рядом с текущими моделями;
- по 10–20 representative cases снять quality smoke, load time, VRAM и p95;
- исключить модели, не помещающиеся вместе или явно проваливающие quality/latency gate;
- выбрать по 2 ASR и 2 LLM финалиста.

**Статус на 2026-07-13:** R1-R3 и финальная model acceptance завершены без переключения рабочей версии приложения. Candidate report и финальная приёмка хранятся во внутреннем experiment archive.

- LLM research winner: `Qwen3.5-9B Q4_K_M` — semantic pass@1 62.5%, test pass^3 73.91%, full loop p95 3.10 с;
- LLM fast/resource fallback: `Qwen3.5-4B Q4_K_M` — pass@1 50.0%, p95 4.40 с;
- `T-lite-it-2.1 Q5_K_M` исключен из model shortlist: pass@1 39.58%, single-write 0/11, no-tool 0/4;
- финальный LLM runtime: `Qwen3.5-9B Q4_K_M` + constrained JSON Schema; pass@1 68.75%, p95 2.256 с, runtime 48/48;
- финальный ASR: `Whisper-large-v3-turbo` + `faster-whisper` FP16; WER 9.09%, бирки 47/47, p95 0.149 с;
- совместный resource gate: 9,186 MiB из 12,227 MiB, запас 3,041 MiB;
- end-to-end 72 кейса: 59.72%, runtime 72/72, p95 LLM loop 2.366 с;
- следующий этап — R4 integration; rollout заблокирован до batch normalizer, deterministic clarification, Postgres state test и Playwright E2E;
- установленные `Qwen3-8B Q4_K_M` и `Qwen3-4B-Instruct-2507 Q5_K_M` остаются только historical controls и не участвуют в выборе production-модели;
- `Canary-1B-v2` не прошел operational gate: checkpoint 6.36 GB и NeMo runtime существенно тяжелее альтернатив;
- пользователь вручную проверяет 10 строк private multi-turn smoke split; текущие constrained 7/10 считаются диагностическими до смены `review.status`.

### Этап R3 — полные повторные эксперименты, 3–4 дня

- полный ASR benchmark с postprocessor/correction ablation;
- LLM dev pass@1, затем pass^3/test для финалистов;
- state-based write и multi-turn evaluation;
- constrained output/validator ablation на лучшей LLM.

**Статус:** завершён 2026-07-13. Выбор моделей зафиксирован в final acceptance report.

### Этап R4 — интеграция и production benchmark, 2–3 дня

- переключаемая конфигурация новых моделей без удаления baseline;
- обновление tool parser/prompt/runtime adapter, если требуется;
- совместный ASR+LLM прогон через приложение;
- Playwright/E2E сценарии voice, continuation, ambiguity, preview/confirm/cancel;
- финальная таблица и решение.

### Этап R5 — rollout, 1 день

- закрепить model/runtime versions и checksums;
- health/readiness и warmup;
- обновить инструкции запуска;
- сохранить rollback config;
- только после приёмки убрать тяжёлые модели из production startup.

## Приложение A. Исторический план первоначальной реализации

### Неделя 1

- День 1: backend/frontend инвентаризация, security baseline.
- День 2: tool schemas AI-контура, entity resolution spec, validator spec.
- День 3-4: сбор и разметка текстового датасета, seed-база.
- День 5: начало ASR-датасета, первые backend-моки agent loop.

### Неделя 2

- День 6: ASR research.
- День 7: ASR correction research.
- День 8-9: LLM tool-calling research.
- День 10: constrained output + validator ablation.

### Неделя 3

- День 11-12: backend AI каркас, read-тулы AI-контура.
- День 13-14: write draft/confirm infrastructure.
- День 15: `create_weight` end-to-end.

### Неделя 4

- День 16-17: `create_daily_action` end-to-end.
- День 18: `create_insemination` end-to-end.
- День 19: audit, integration tests.
- День 20: frontend AI input + preview/confirm.

### Неделя 5

- E2E-сценарии;
- error handling;
- pass^3/stability runs;
- финальные таблицы исследований;
- demo script;
- документация ограничений.

Если срок жестко ограничен двумя неделями, то недели 1-2 становятся research/working prototype, а production-quality frontend/backend polish переносится в следующий этап.

## 13. Definition of Done для текущего контура

AI-контур считается готовым, если:
- все 6 read-тулов работают через agent loop;
- все 3 write-тула создают draft и пишут в БД только после confirm;
- LLM не имеет прямого пути к commit;
- есть disambiguation для 0/2+ совпадений;
- есть partial batch preview;
- есть audit событий agent loop и commit;
- есть unit/integration/state-based тесты по AI-контуру;
- есть датасет и результаты исследований;
- выбранные ASR/LLM/validator решения подтверждены на своих данных;
- ASR+LLM одновременно размещаются на целевом сервере без unload/reload в обычном запросе;
- ASR p95 ≤1.5 с, полный LLM tool-call p95 ≤5 с, voice-to-preview p95 ≤8 с;
- multi-turn кейсы подтверждают перенос сущности и обязательных полей между сообщениями;
- зафиксированы model version, quantization, context, runner, generation parameters и rollback config;
- frontend показывает read answer, clarification, preview, confirm и commit report;
- ограничения из `03-domain-gaps.md` не выдаются за реализованный функционал.

## 14. Главные риски

| Риск | Как контролировать |
|---|---|
| LLM делает неверный write-call | constrained output, validator, draft/confirm, state-based tests |
| Неверная бирка из ASR | entity-level WER, exact-match бирки, correction FPR |
| Тихий пропуск части batch | три корзины: resolved / ambiguous / not_found |
| Утечка между организациями | явный `OrganizationId`, org-scope тесты, security fixes |
| Переоценка качества на маленьком датасете | доверительные интервалы, ограничения, error analysis |
| Высокое качество при неприемлемой скорости | hard latency gates, TTFT/end-to-end p95, ранний resource smoke |
| ASR и LLM по отдельности быстрые, вместе не помещаются | совместный benchmark, VRAM peak и запрет unload/reload в normal path |
| История диалога замедляет каждый следующий запрос | ограниченный context, измерение 1/4/8/12 сообщений, summary только при необходимости |
| Квантизация портит русский tool-calling | сравнение Q4/Q5/Q6 на одном dev split и non-inferiority gate |
| Разрастание scope до 55 тулов | сначала ключевой AI-контур, полный каталог как roadmap |
| Дублирование frontend-форм | `form.setFieldsValue(...)` в существующие формы |
| Скрытые backend-каскады | таблица hidden rules и validator tests |

## 15. Порядок начала работ

Первый практический шаг после утверждения пересмотренного model-selection:

1. Зафиксировать текущие T-pro/Whisper результаты и production timings как baseline.
2. Добавить multi-turn/session evaluation cases без изменения test golden после начала прогонов.
3. Подготовить единый resource/latency harness для RTX 5070.
4. Установить кандидатов рядом с текущими моделями.
5. Провести быстрый отсев R2 и только после него запускать полные дорогие прогоны.
