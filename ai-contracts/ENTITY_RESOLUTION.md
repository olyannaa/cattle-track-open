# Спецификация нормализации доменных сущностей

Дата: 2026-07-09.

Область: entity resolution для AI-контура Cattle Track. Эта спецификация дополняет tool schemas из `schemas/v1/tools/` и описывает слой между LLM arguments и вызовом backend-сервисов.

## 1. Главный принцип

LLM не выбирает реальные `AnimalId`, `GroupId`, `MedicineId` самостоятельно, если пользователь назвал сущность естественным языком.

LLM возвращает только исходную ссылку пользователя:

- бирку животного;
- название группы;
- название препарата;
- дату или относительное выражение даты.

Backend AI-layer резолвит эти ссылки строго в рамках `OrganizationId` текущей сессии и возвращает один из статусов:

- `resolved`;
- `ambiguous`;
- `not_found`.

Write-команды с `ambiguous` или `not_found` сущностями не коммитятся без уточнения пользователя.

## 2. AnimalRef

### 2.1 Что приходит от LLM

`AnimalRef` от LLM:

```json
{
  "kind": "animal",
  "tag": "523",
  "role_hint": "bull",
  "source_text": "бык с биркой 523"
}
```

Поля:

| Поле | Кто задает | Назначение |
|---|---|---|
| `kind` | LLM | Всегда `animal`. |
| `tag` | LLM | Бирка, как понял пользовательский ввод после ASR-коррекции. |
| `role_hint` | LLM | Optional: `cow`, `bull`, `calf`, `heifer`, `any`, `__unknown`. Это подсказка для ранжирования/показа, не право отфильтровать молча. |
| `source_text` | LLM | Исходный фрагмент пользовательской команды. |

### 2.2 Что резолвит backend

Backend resolver:

1. Берет `OrganizationId` из авторизованной сессии.
2. Ищет животных с `TagNumber == tag` строго точным совпадением в рамках организации.
3. Не использует fuzzy-match в SQL/БД.
4. Если найдено 0 - возвращает `not_found`.
5. Если найдено 1 - возвращает `resolved`.
6. Если найдено 2+ - возвращает `ambiguous`.

Важное доменное правило: одинаковые бирки внутри одной организации допустимы. Это не ошибка данных и не повод выбирать первое животное. Например, если пользователь говорит "осемени быка 523", а в организации есть два животных с биркой `523`, resolver обязан вернуть `ambiguous` и показать различающие поля: дату рождения, тип, статус, группу и дополнительные идентификаторы.

### 2.3 Fuzzy-match

Fuzzy-match разрешен только до resolver-а:

```text
ASR text -> correction by farm dictionary -> exact DB lookup
```

Fuzzy-match запрещен для поиска в БД:

```text
wrong: WHERE tag LIKE ... / trigram / levenshtein
right: WHERE organization_id = ... AND tag_number = exact_tag
```

Причина: fuzzy-поиск может выбрать не то животное в write-команде. Для AI write это недопустимо.

## 3. GroupRef

### 3.1 Что приходит от LLM

```json
{
  "kind": "group",
  "name": "основное стадо",
  "source_text": "в основное стадо"
}
```

LLM задает только `name` и optional `source_text`.

### 3.2 Что резолвит backend

Backend resolver ищет группу в рамках `OrganizationId`.

Результат:

- `resolved`, если найдена одна группа;
- `ambiguous`, если найдено несколько кандидатов с одинаковым/эквивалентным именем;
- `not_found`, если группы нет.

Для `ambiguous` candidates нужно показывать:

- `group_id`;
- `name`;
- `type_name`;
- `location`;
- `description`, если есть.

## 4. MedicineRef

### 4.1 Что приходит от LLM

```json
{
  "kind": "medicine",
  "name": "ивермек",
  "source_text": "ивермек"
}
```

LLM задает только название/фрагмент.

### 4.2 Что резолвит backend

Backend resolver ищет препарат по справочнику организации.

Результат:

- `resolved`, если найден один препарат;
- `ambiguous`, если найдено несколько похожих/эквивалентных записей после нормализации справочника;
- `not_found`, если препарата нет.

Особенность текущего backend: поле `Medicine` в daily actions может быть свободным текстом или строковым Guid препарата. Поэтому `not_found` для препарата не всегда блокирует draft полностью. UI должен показать выбор:

- создать препарат в справочнике;
- оставить как свободный текст, если выбранный backend-сценарий это допускает;
- отменить/уточнить.

Для текущего AI-контура безопасное правило: если write-команда требует препарат и он не найден, draft получает `not_found` и не коммитится без явного выбора пользователя.

## 5. DateRef

### 5.1 Что приходит от LLM

```json
{
  "kind": "date",
  "value": "2026-07-09",
  "source_text": "сегодня"
}
```

LLM должен возвращать ISO date, если дата понятна. Если дата относительная, LLM может заполнить:

```json
{
  "kind": "date",
  "relative": "today",
  "source_text": "сегодня"
}
```

### 5.2 Что нормализует backend

Backend AI-layer нормализует дату относительно timezone фермы/пользователя и текущей даты backend.

Правила:

- `today`, `yesterday`, `tomorrow` превращаются в ISO date только backend-слоем;
- write validator запрещает дату в будущем, если конкретный бизнес-сценарий не разрешает future date;
- write validator запрещает дату события раньше даты рождения животного;
- дата не резолвится через fuzzy-match.

## 6. Единые response-модели

Формальный JSON Schema: `schemas/v1/common/entity-resolution.schema.json`.

### 6.1 Resolved

```json
{
  "schema_version": "v1",
  "entity": "animal",
  "state": "resolved",
  "input": {
    "kind": "animal",
    "tag": "523",
    "role_hint": "bull",
    "source_text": "бык с биркой 523"
  },
  "resolved": {
    "id": "be7d9e62-9163-43fa-98e5-6ce7a2665317",
    "display": "523, бык, 2023-04-18, группа Производители"
  },
  "candidates": [],
  "message": null
}
```

### 6.2 Ambiguous

```json
{
  "schema_version": "v1",
  "entity": "animal",
  "state": "ambiguous",
  "input": {
    "kind": "animal",
    "tag": "523",
    "role_hint": "bull",
    "source_text": "бык с биркой 523"
  },
  "resolved": null,
  "candidates": [
    {
      "id": "be7d9e62-9163-43fa-98e5-6ce7a2665317",
      "display": "523, бык, 2023-04-18, группа Производители",
      "animal": {
        "tag": "523",
        "type": "Бык",
        "status": "Активное",
        "birth_date": "2023-04-18",
        "group_name": "Производители",
        "identifiers": [
          { "name": "RFID", "value": "643001234567890" }
        ]
      }
    }
  ],
  "message": "Найдено несколько животных с биркой 523. Уточните по дате рождения, группе или дополнительному идентификатору."
}
```

### 6.3 Not Found

```json
{
  "schema_version": "v1",
  "entity": "animal",
  "state": "not_found",
  "input": {
    "kind": "animal",
    "tag": "523",
    "source_text": "животное 523"
  },
  "resolved": null,
  "candidates": [],
  "message": "В этой организации не найдено животное с биркой 523."
}
```

## 7. Disambiguation-кандидаты

Для животных candidate обязан содержать максимально различающие поля из доступных:

- `id`;
- `display`;
- `tag`;
- `type`;
- `status`;
- `birth_date`;
- `group_name`;
- `breed`;
- `gender`, если есть;
- `identifiers[]` с дополнительными идентификаторами;
- `mother_tag` / `father_tags`, если это помогает отличить животное.

Для групп:

- `id`;
- `display`;
- `name`;
- `type_name`;
- `location`;
- `description`.

Для препаратов:

- `id`;
- `display`;
- `name`;
- `substance`;
- `factory`;
- `withdrawal_period`.

UI/agent должен показывать 2-5 candidates. Если candidates больше 5, resolver возвращает `ambiguous` с пустым или усеченным списком и просит сузить запрос.

## 8. Лимиты уточнений

Лимиты из архитектуры фиксируются как hard rule:

- максимум 2 уточнения на одну сущность;
- максимум 2 уточнения суммарно на один пользовательский запрос.

Если лимит исчерпан:

- read-тул возвращает пользователю просьбу открыть форму/фильтр вручную или переформулировать;
- write-тул не создает commit-ready draft;
- audit пишет `ai_clarification_limit_reached`.

## 9. Что важно для будущей реализации

Нельзя делать:

- выбирать первое животное при дублирующейся бирке;
- считать дубли бирки ошибкой данных;
- использовать fuzzy DB lookup для write-команд;
- позволять LLM самому выбирать `animal_id` из списка без deterministic resolver;
- коммитить write draft с unresolved сущностями.

Нужно делать:

- точный поиск бирки внутри `OrganizationId`;
- при 2+ точных совпадениях возвращать `ambiguous`;
- показывать дату рождения и дополнительные идентификаторы;
- хранить выбранный пользователем candidate в draft;
- повторно проверять candidate перед commit.
