# AI validation layer rules

Дата: 2026-07-09.

Область: `backend/CAT/Services/Ai/AiToolValidator.cs`.

Цель: вынести pre-commit бизнес-валидацию write-тулов в отдельный deterministic слой. Этот слой вызывается после JSON Schema validation/entity resolution и до создания draft preview/confirm.

## Retry policy

После ошибки AI validator разрешает максимум один retry к LLM.

Поведение:

1. `retryAttempt = 0`, есть retryable ошибки -> `RetryLlmOnce`.
2. `retryAttempt >= 1`, ошибки остались -> `ShowHumanError`.
3. Ошибок нет -> `ShowPreview`.

Non-retryable ошибки (`org-scope`, duplicate event) не должны автоматически исправляться LLM. Если одновременно есть retryable ошибка, один retry всё равно разрешен, но non-retryable ошибка останется и должна быть показана человеку, если не исчезла после повторной нормализации.

После исчерпания retry пользователь видит понятную ошибку или partial preview. Write commit не вызывается.

## Rule table

| Rule id | Source | What validates | Tests |
|---|---|---|---|
| `AI-VAL-ORG-ANIMAL` | `IOrganizationService.CheckAnimalById`, backend audit org-scope notes | animal id belongs to current `OrganizationId` | `ValidateCreateWeight_*`, `ValidateCreateInsemination_*` |
| `AI-VAL-ORG-GROUP` | `DailyActionsController` group org-check | `oldGroupId` / `newGroupId` belongs to current organization | `ValidateCreateDailyActions_MoveRequiresOrgScopedNewGroup` |
| `AI-VAL-REQUIRED` | backend DTO required-by-behavior fields | required batch items, dates, cow ids, feeding percentages | `ValidateCreateInsemination_*`, `ValidateFeedingPercentages_*` |
| `AI-VAL-DATE-FUTURE` | AI-only sanity check from architecture spec | write event date must not be in future | `ValidateCreateWeight_InvalidWeightFutureDateDuplicateAndUnknownMethod_ReturnsErrors`, `ValidateCreateDailyActions_DuplicateIsNonRetryableButOtherErrorsStillAllowOneLlmRetry` |
| `AI-VAL-DATE-BIRTH` | AI-only sanity check from architecture spec | write event date must not be before animal birth date | `ValidateCreateWeight_BeforeBirth_ReturnsDateBirthError` |
| `AI-VAL-DUP-WEIGHT` | AI-only duplicate event check | same animal + same date weight already exists | `ValidateCreateWeight_InvalidWeightFutureDateDuplicateAndUnknownMethod_ReturnsErrors` |
| `AI-VAL-WEIGHT-RANGE` | AI-only sanity check; backend currently lacks `Weight > 0` validation | weight must be 1-3000 kg | `ValidateCreateWeight_InvalidWeightFutureDateDuplicateAndUnknownMethod_ReturnsErrors` |
| `AI-VAL-ENUM-KNOWN` | tool schema enum fallback and DTO enum attributes | closed enums cannot be `__unknown` at commit time | `ValidateCreateWeight_InvalidWeightFutureDateDuplicateAndUnknownMethod_ReturnsErrors` |
| `AI-VAL-DAILY-CASCADE` | `DailyActionService.CreateDailyAction` hidden cascades | required fields for move/disposal/research/identification/type-change and insemination conditional fields | `ValidateCreateDailyActions_CascadeFieldsAreRequired`, `ValidateCreateInsemination_NaturalRequiresBullAndChecksDuplicate` |
| `AI-VAL-DUP-DAILY` | AI-only duplicate event check | same animal + type + date + subtype daily action already exists | `ValidateCreateDailyActions_DuplicateIsNonRetryableButOtherErrorsStillAllowOneLlmRetry` |
| `AI-VAL-DUP-INSEMINATION` | AI-only duplicate event check; `AnimalService.InsertInseminations` creates pregnancy cascade | same cow + date + insemination type already exists | `ValidateCreateInsemination_NaturalRequiresBullAndChecksDuplicate` |
| `AI-VAL-FEED-PERCENT-SUM` | tool catalog feeding warning; backend does not enforce sum = 100% | feeding percentages must be 0-100 and sum to 100 | `ValidateFeedingPercentages_RequiresHundredPercentTotal`, `ValidateFeedingPercentages_HundredPercentTotal_IsValid` |

## Hidden service rules duplicated in validator

`create_daily_action`:

- `"Перевод"` requires `NewGroupId` and changes animal group on commit.
- `"Выбытие"` requires `Subtype` as disposal reason and changes animal status on commit.
- `"Исследования"` requires `ResearchName` and writes to `research`, not `daily_actions`.
- `"Присвоение номеров"` requires `Subtype` as identification field name and `IdentificationValue`.
- `"Изменение половозрастной группы"` requires `OldType` and `NewType` and changes animal type on commit.

`create_insemination`:

- all cow ids must belong to organization;
- all bull ids must belong to organization;
- `"Естественное"` requires at least one bull;
- `"Эмбрион"` requires `EmbryoId`;
- commit path cascades into pregnancy creation, so draft must be valid before confirmation.

`create_weight`:

- animal must belong to organization;
- weight must be sane;
- date must be sane;
- duplicate weight for same animal/date is blocked before draft commit.

## Required fields from current forms

AI-layer не должен молча создавать write draft, если пользователь голосом не назвал обязательные поля. В таком случае нужно показать понятное уточнение, а не додумывать значение.

Фактические формы на 2026-07-09:

| Сценарий | Форма | Можно массово | Обязательные поля для AI |
|---|---|---:|---|
| Взвешивание | `FormAddWeight` | Нет | животное, дата взвешивания, вес, метод взвешивания |
| Перевод | `FormAddTransfer` | Да, выбранные животные | животные, дата перевода, новая группа |
| Выбытие | `FormAddDisposal` | Да, выбранные животные | животные, дата выбытия, причина выбытия |
| Присвоение номера | `FormAddAssigmentNumber` | Да, выбранные животные | животные, дата, тип идентификатора, значение идентификатора |
| Изменение половозрастной группы | `FormChangeAgeGenderGroup` | Да, выбранные животные | животные, дата, старая и новая половозрастная группа |
| Исследование | `FormAddResearch` | Да, выбранные животные | животные, название исследования, дата забора материала, вид материала |
| Обработка/лечение | `FormAddTreatment` | Да, выбранные животные | животные, дата обработки, тип обработки, препарат, доза, дата следующей обработки; для `Лечение` также диагноз |
| Осеменение | `InseminationForm` | Да, несколько коров в одном сохранении | корова/коровы, дата осеменения, тип осеменения; для `Естественное` нужен бык; для `Эмбрион` нужен номер эмбриона |
| Проверка стельности | `PregnancyRateForm` | Нет | корова, дата проверки, результат проверки |
| Отел | `CalvingForm` | Нет | корова, дата отела, тяжесть отела, тип отела; для живого/мертворожденного теленка обязательные поля регистрации теленка задаются отдельным блоком формы |

Для доступного tool-calling сейчас нет write-тулов для проверки стельности и отела, поэтому такие команды должны попадать в `no-tool` или будущий backlog, а не в `create_insemination`.

## Test command

Host with .NET SDK:

```bash
dotnet test backend/CAT.Tests/CAT.Tests.csproj
```

Docker SDK fallback:

```bash
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test backend/CAT.Tests/CAT.Tests.csproj
```
