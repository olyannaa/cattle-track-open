# AI Test Database Fixtures

Назначение: воспроизводимые seed-данные для интеграционных и state-based тестов AI-контура.

## Что создается

Фиксированная тестовая организация:

```text
OrganizationId: 90000000-0000-0000-0000-000000000001
Name: AI Test Organization
```

Данные:

- группы: `Основное стадо`, `Молодняк`, `Карантин`;
- животные для exact lookup и ambiguity;
- веса, включая duplicate fixture;
- daily actions, включая duplicate/cascade fixture;
- осеменения и pregnancy fixture;
- препараты для daily action / treatment сценариев;
- partial-invalid batch fixtures в `ai_fixtures.json`.

## Ambiguity Fixtures

| Сценарий | Бирка | Ожидаемый результат |
|---|---:|---:|
| 0 совпадений | `0000` | `not_found` |
| 1 совпадение | `1432` | resolved |
| 2-5 совпадений | `523` | 3 candidates, needs disambiguation |
| больше 5 | `777` | 6 candidates, truncated candidates + clarification |

Важно: одинаковые бирки внутри организации намеренно разрешены. Entity resolver должен уточнять животное по дате рождения, типу, группе или дополнительным идентификаторам.

## Запуск Seed

Полностью локальный demo-запуск:

```bash
docker compose -f docker-compose.yml -f docker-compose.demo.yml up --build
```

При первом старте PostgreSQL применяет `schema.sql`, затем этот seed. Для пересоздания базы с нуля:

```bash
docker compose -f docker-compose.yml -f docker-compose.demo.yml down -v
```

Проверка demo-БД:

```bash
./scripts/verify-demo-db.sh
```

Ручной запуск seed через локальный контейнер PostgreSQL:

```bash
docker compose exec -T postgres psql -U postgres -d postgres < backend/testdata/ai_seed.sql
```

Через внешний DSN:

```bash
psql "$POSTGRES_TEST_DSN" -v ON_ERROR_STOP=1 -f backend/testdata/ai_seed.sql
```

Скрипт можно запускать повторно. Он удаляет и пересоздает только данные организации `90000000-0000-0000-0000-000000000001`.

## Проверка Seed

```sql
SELECT tag_number, COUNT(*)
FROM animals
WHERE organization_id = '90000000-0000-0000-0000-000000000001'
GROUP BY tag_number
ORDER BY tag_number;
```

Ожидаемо:

```text
0000 -> 0 rows
1432 -> 1 row
523  -> 3 rows
777  -> 6 rows
```

## Интеграционные И State-Based Тесты

1. Поднять backend-зависимости.
2. Применить миграции/актуальную схему проекта.
3. Запустить `backend/testdata/ai_seed.sql`.
4. В тестах использовать `ai_fixtures.json` как manifest фиксированных id и expected outcomes.
5. Перед каждым state-based тестом повторять seed или оборачивать тест в rollback transaction.

Команда unit-тестов AI validator:

```bash
dotnet test backend/CAT.Tests/CAT.Tests.csproj
```

State-based тесты agent loop пока должны проверять не только HTTP status, но и состояние БД после confirm:

- weight inserted only for valid resolved animal;
- ambiguous/not_found batch items are not committed;
- duplicate weight/daily action/insemination rejected;
- move action updates expected group only after confirm;
- partial batch returns per-item report.
