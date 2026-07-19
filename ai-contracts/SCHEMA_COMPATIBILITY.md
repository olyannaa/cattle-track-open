# Правила совместимости AI tool schemas

Дата: 2026-07-09.

Область: tool-calling контракты Cattle Track AI в `ai-contracts/schemas/v1`.

## 1. Источник истины

Каталог инструментов и основной скоуп: `../00-tool-catalog.md`.

Backend-аудит фактических DTO, сервисов и скрытых правил: `../06-backend-audit.md`.

Эти JSON Schema описывают аргументы AI-тулов, а не публичные HTTP DTO один-в-один. Для write-тулов схема описывает pre-commit draft input; реальная запись идет только после draft preview, подтверждения и повторной deterministic-валидации.

## 2. Версионирование

Версия схемы задается тремя способами:

- каталогом пути: `schemas/v1/...`;
- `$id`, где явно присутствует `/v1/`;
- полем `x-tool-version: "v1"`;
- обязательным аргументом `schema_version: "v1"` внутри tool arguments.

Пока поддерживается только `v1`.

Следующая major-версия (`v2`) нужна, если изменение ломает уже сохраненные drafts, датасет golden-разметки, frontend preview или agent evaluator.

## 3. Совместимые изменения внутри `v1`

Разрешены без смены major-версии:

- добавить новое optional поле;
- расширить `description`, `examples`, `x-invalid-examples`;
- добавить более точный `maximum`, `maxLength` или `pattern`, если это только фиксирует уже существующее backend/validator правило и не ломает валидные реальные команды;
- добавить новый enum value только вместе с сохранением `__unknown`;
- добавить новый report `state`, если старые состояния не меняются;
- добавить новый schema файл для нового тула.

## 4. Breaking changes

Требуют `v2`:

- переименовать поле;
- удалить поле;
- сделать optional поле обязательным;
- сузить enum без fallback;
- поменять тип поля;
- поменять batch semantics с partial report на all-or-nothing;
- убрать или изменить смысл `idempotency_key`;
- поменять формат `schema_version`;
- изменить meaning существующего enum value или report state.

## 5. Enum fallback

Каждый закрытый enum, который генерирует LLM, обязан иметь escape value:

```json
"__unknown"
```

Если модель выбрала `__unknown`, соответствующее raw-поле обязательно:

- `create_weight.method == "__unknown"` -> `method_raw`;
- `create_daily_action.items[].type == "__unknown"` -> `type_raw`;
- `create_insemination.items[].insemination_type == "__unknown"` -> `insemination_type_raw`.

Правило commit:

- `__unknown` никогда не коммитится напрямую в backend;
- AI validator либо маппит raw value на безопасное известное значение, либо возвращает clarification / validation error;
- в draft preview пользователь должен видеть исходный raw text.

## 6. Write flow

Все write-тулы работают только через propose-then-commit:

1. LLM возвращает arguments по JSON Schema.
2. Entity resolver резолвит бирки, группы, препараты и пользователей в рамках `OrganizationId`.
3. Deterministic validator проверяет JSON Schema + бизнес-правила из backend-аудита.
4. Draft сохраняется в Redis с TTL.
5. Пользователь видит preview.
6. Confirm endpoint повторно валидирует preconditions и вызывает существующий backend service.

Ни один write-тул не должен писать в БД на шаге tool-call.

## 7. Batch semantics

Batch write не является all-or-nothing на AI preview уровне.

Обязательные корзины:

- `resolved` - item можно коммитить после подтверждения;
- `ambiguous` - найдено 2-5 кандидатов, нужен выбор;
- `not_found` - сущность не найдена;
- `invalid` - нарушена схема или бизнес-правило;
- `committed` - item успешно записан после confirm;
- `failed` - item прошел preview, но упал при повторной проверке или записи.

Если есть `ambiguous`, `not_found` или `invalid`, UI не должен тихо пропускать их. Нужно показать partial preview и запросить явное подтверждение частичного commit для `resolved`.

Формат отчета: `schemas/v1/common/batch-write-report.schema.json`.

## 8. Idempotency

Каждая write-команда требует idempotency:

- single write: `idempotency_key`;
- batch write: `batch_idempotency_key` + `items[].idempotency_key`.

Ключ должен быть стабильным между retry одного и того же пользовательского действия. Рекомендуемый формат:

```text
voice:<tool>:<date>:<entity-or-batch>:<intent>
```

Backend agent layer должен хранить mapping `idempotency_key -> result` минимум на время жизни draft/commit TTL. Повторный confirm с тем же ключом не должен создавать дубль события.

## 9. Обязательные бизнес-правила вне JSON Schema

JSON Schema ловит форму данных, enum, обязательность и часть условий. Отдельный AI validator обязан дополнительно проверять:

- `OrganizationId` scope для всех animal/group ids;
- дату не в будущем;
- дату события не раньше рождения животного;
- разумный диапазон веса и дубль взвешивания;
- каскады daily actions: движение, выбытие, смена типа, исследования, присвоение идентификатора;
- для осеменения: принадлежность коров/быков организации и каскадное создание pregnancy;
- partial batch policy перед commit;
- подтверждение пользователя перед любой записью.

## 10. Тесты

Локальная проверка схем:

```bash
python3 scripts/validate_ai_contract_schemas.py
```

Тест без внешних зависимостей проверяет:

- JSON parse всех schema-файлов;
- обязательные metadata поля;
- версионирование `v1`;
- наличие `schema_version` в tool args;
- валидность `examples`;
- невалидность `x-invalid-examples`;
- наличие `__unknown` у закрытых enum write-тулов.
