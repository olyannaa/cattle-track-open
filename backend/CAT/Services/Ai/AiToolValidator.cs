using CAT.Controllers.DTO;
using System.Text.Json;

namespace CAT.Services.Ai;

public sealed class AiToolValidator : IAiToolValidator
{
    public const double MinWeightKg = 1;
    public const double MaxWeightKg = 3000;

    private static readonly HashSet<string> WeightMethods = new(StringComparer.Ordinal)
    {
        "Автоматическая весовая станция",
        "Ручное взвешивание",
        "Расчетный метод"
    };

    private static readonly HashSet<string> DailyActionTypes = new(StringComparer.Ordinal)
    {
        "Осмотры",
        "Обработка",
        "Вакцинации и обработки",
        "Лечение",
        "Перевод",
        "Выбытие",
        "Исследования",
        "Присвоение номеров",
        "Изменение половозрастной группы"
    };

    private static readonly HashSet<string> InseminationTypes = new(StringComparer.Ordinal)
    {
        "Искусственное",
        "Естественное",
        "Эмбрион"
    };

    private readonly IAiToolValidationDataSource _dataSource;
    private readonly Func<DateOnly> _todayProvider;

    public AiToolValidator(IAiToolValidationDataSource dataSource)
        : this(dataSource, () => DateOnly.FromDateTime(DateTime.UtcNow))
    {
    }

    public AiToolValidator(IAiToolValidationDataSource dataSource, Func<DateOnly> todayProvider)
    {
        _dataSource = dataSource;
        _todayProvider = todayProvider;
    }

    public AiToolValidationResult ValidateCreateWeight(Guid organizationId, WeightCreateDTO dto, int retryAttempt = 0)
    {
        var errors = new List<AiValidationError>();
        var animal = ValidateAnimal(organizationId, dto.AnimalId, "$.animalId", errors);

        if (dto.Weight is null or < MinWeightKg or > MaxWeightKg)
        {
            errors.Add(Error(
                AiValidationRules.WeightRange,
                "AI-only sanity check",
                $"Вес должен быть в диапазоне {MinWeightKg}-{MaxWeightKg} кг.",
                "$.weight"));
        }

        ValidateDate(dto.Date, animal, "$.date", errors);

        if (string.IsNullOrWhiteSpace(dto.Method) || !WeightMethods.Contains(dto.Method))
        {
            errors.Add(Error(
                AiValidationRules.EnumKnown,
                "AI tool schema enum fallback",
                "Метод взвешивания не распознан. Нужно выбрать известный метод или уточнить у пользователя.",
                "$.method"));
        }

        if (_dataSource.WeightExists(organizationId, dto.AnimalId, dto.Date))
        {
            errors.Add(Error(
                AiValidationRules.DuplicateWeight,
                "AI-only duplicate event check",
                "Взвешивание этого животного на указанную дату уже существует.",
                "$.date",
                retryableByLlm: false));
        }

        return BuildResult(errors, retryAttempt);
    }

    public AiToolValidationResult ValidateCreateDailyActions(
        Guid organizationId,
        IEnumerable<CreateDailyActionDTO> items,
        int retryAttempt = 0)
    {
        var errors = new List<AiValidationError>();
        var list = items?.ToList() ?? new List<CreateDailyActionDTO>();

        if (list.Count == 0)
        {
            errors.Add(Error(AiValidationRules.RequiredField, "POST /api/DailyActions", "Нужно передать хотя бы одно действие.", "$"));
            return BuildResult(errors, retryAttempt);
        }

        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var path = $"$.items[{i}]";
            var animal = ValidateAnimal(organizationId, item.AnimalId, $"{path}.animalId", errors);

            if (string.IsNullOrWhiteSpace(item.Type) || !DailyActionTypes.Contains(item.Type))
            {
                errors.Add(Error(
                    AiValidationRules.EnumKnown,
                    "CreateDailyActionDTO.Type IsIn attribute",
                    "Тип ежедневного действия не распознан.",
                    $"{path}.type"));
            }

            ValidateDate(item.Date, animal, $"{path}.date", errors);

            if (item.OldGroupId.HasValue && !_dataSource.IsGroupInOrganization(organizationId, item.OldGroupId))
            {
                errors.Add(Error(AiValidationRules.OrgGroupScope, "DailyActionsController org-check", "Старая группа не принадлежит организации.", $"{path}.oldGroupId", retryableByLlm: false));
            }

            if (item.NewGroupId.HasValue && !_dataSource.IsGroupInOrganization(organizationId, item.NewGroupId))
            {
                errors.Add(Error(AiValidationRules.OrgGroupScope, "DailyActionsController org-check", "Новая группа не принадлежит организации.", $"{path}.newGroupId", retryableByLlm: false));
            }

            ValidateDailyActionCascade(item, path, errors);

            if (_dataSource.DailyActionExists(organizationId, item.AnimalId, item.Type, item.Date, item.Subtype))
            {
                errors.Add(Error(
                    AiValidationRules.DuplicateDailyAction,
                    "AI-only duplicate event check",
                    "Такое ежедневное действие для животного на указанную дату уже существует.",
                    path,
                    retryableByLlm: false));
            }
        }

        return BuildResult(errors, retryAttempt);
    }

    public AiToolValidationResult ValidateCreateInsemination(
        Guid organizationId,
        InseminationBatchDTO dto,
        int retryAttempt = 0)
    {
        var errors = new List<AiValidationError>();
        var items = dto?.Items?.ToList() ?? new List<InseminationItemDTO>();

        if (items.Count == 0)
        {
            errors.Add(Error(AiValidationRules.RequiredField, "POST /api/Reproductive/inseminations/batch", "Нужно передать хотя бы одно осеменение.", "$.items"));
            return BuildResult(errors, retryAttempt);
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var path = $"$.items[{i}]";

            if (string.IsNullOrWhiteSpace(item.InseminationType) || !InseminationTypes.Contains(item.InseminationType))
            {
                errors.Add(Error(AiValidationRules.EnumKnown, "frontend InseminationForm radio values", "Тип осеменения не распознан.", $"{path}.inseminationType"));
            }

            ValidateDate(item.Date, null, $"{path}.date", errors);

            var cowIds = GetCowIds(item).Distinct().ToList();
            if (cowIds.Count == 0)
            {
                errors.Add(Error(AiValidationRules.RequiredField, "ReproductiveController ValidateInseminationItemsOrganization", "Нужно указать хотя бы одну корову.", $"{path}.cowIds"));
            }

            foreach (var cowId in cowIds)
            {
                var cow = ValidateAnimal(organizationId, cowId, $"{path}.cowIds", errors);
                ValidateDate(item.Date, cow, $"{path}.date", errors);

                if (!string.IsNullOrWhiteSpace(item.InseminationType) &&
                    _dataSource.InseminationExists(organizationId, cowId, item.Date, item.InseminationType))
                {
                    errors.Add(Error(
                        AiValidationRules.DuplicateInsemination,
                        "AI-only duplicate event check",
                        "Осеменение этой коровы с таким типом на указанную дату уже существует.",
                        path,
                        retryableByLlm: false));
                }
            }

            foreach (var bullId in GetBullIds(item).Distinct())
            {
                ValidateAnimal(organizationId, bullId, $"{path}.bullIds", errors);
            }

            ValidateInseminationConditionalFields(item, path, errors);
        }

        return BuildResult(errors, retryAttempt);
    }

    public AiToolValidationResult ValidateFeedingPercentages(
        IEnumerable<AiFeedingPercentage> percentages,
        int retryAttempt = 0)
    {
        var list = percentages?.ToList() ?? new List<AiFeedingPercentage>();
        var errors = new List<AiValidationError>();

        if (list.Count == 0)
        {
            errors.Add(Error(AiValidationRules.RequiredField, "feeding tools roadmap", "Нужно передать проценты кормления.", "$"));
            return BuildResult(errors, retryAttempt);
        }

        foreach (var item in list)
        {
            if (item.Percentage < 0 || item.Percentage > 100)
            {
                errors.Add(Error(AiValidationRules.FeedingPercentSum, "AI-only feeding sanity check", "Процент кормления должен быть от 0 до 100.", $"$.{item.Name}"));
            }
        }

        var sum = list.Sum(x => x.Percentage);
        if (Math.Abs(sum - 100m) > 0.01m)
        {
            errors.Add(Error(AiValidationRules.FeedingPercentSum, "AI-only feeding sanity check", "Сумма процентов кормления должна быть 100%.", "$"));
        }

        return BuildResult(errors, retryAttempt);
    }

    private AiAnimalFacts? ValidateAnimal(Guid organizationId, Guid animalId, string path, List<AiValidationError> errors)
    {
        var animal = _dataSource.GetAnimalFacts(organizationId, animalId);
        if (animal == null)
        {
            errors.Add(Error(AiValidationRules.OrgAnimalScope, "IOrganizationService.CheckAnimalById", "Животное не найдено в организации пользователя.", path, retryableByLlm: false));
        }

        return animal;
    }

    private void ValidateDate(DateOnly? date, AiAnimalFacts? animal, string path, List<AiValidationError> errors)
    {
        if (!date.HasValue || date.Value == default)
        {
            errors.Add(Error(AiValidationRules.RequiredField, "backend DTO date field", "Дата обязательна.", path));
            return;
        }

        if (date.Value > _todayProvider())
        {
            errors.Add(Error(AiValidationRules.DateNotFuture, "AI-only sanity check", "Дата события не может быть в будущем.", path));
        }

        if (animal?.BirthDate != null && date.Value < animal.BirthDate.Value)
        {
            errors.Add(Error(AiValidationRules.DateAfterBirth, "AI-only sanity check", "Дата события не может быть раньше даты рождения животного.", path));
        }
    }

    private static void ValidateDailyActionCascade(CreateDailyActionDTO item, string path, List<AiValidationError> errors)
    {
        switch (item.Type)
        {
            case "Перевод":
                Require(item.NewGroupId.HasValue, "Для перевода нужна новая группа.", $"{path}.newGroupId", errors);
                break;
            case "Выбытие":
                Require(!string.IsNullOrWhiteSpace(item.Subtype), "Для выбытия нужна причина выбытия в subtype.", $"{path}.subtype", errors);
                break;
            case "Исследования":
                Require(!string.IsNullOrWhiteSpace(item.ResearchName), "Для исследования нужно название исследования.", $"{path}.researchName", errors);
                break;
            case "Присвоение номеров":
                Require(!string.IsNullOrWhiteSpace(item.Subtype), "Для присвоения номера нужно имя поля идентификации в subtype.", $"{path}.subtype", errors);
                Require(!string.IsNullOrWhiteSpace(item.IdentificationValue), "Для присвоения номера нужно значение идентификатора.", $"{path}.identificationValue", errors);
                break;
            case "Изменение половозрастной группы":
                Require(!string.IsNullOrWhiteSpace(item.OldType), "Для изменения половозрастной группы нужен старый тип.", $"{path}.oldType", errors);
                Require(!string.IsNullOrWhiteSpace(item.NewType), "Для изменения половозрастной группы нужен новый тип.", $"{path}.newType", errors);
                break;
        }
    }

    private static void ValidateInseminationConditionalFields(InseminationItemDTO item, string path, List<AiValidationError> errors)
    {
        switch (item.InseminationType)
        {
            case "Естественное":
                Require(GetBullIds(item).Any(), "Для естественного осеменения нужен бык.", $"{path}.bullIds", errors);
                break;
            case "Эмбрион":
                Require(!string.IsNullOrWhiteSpace(item.EmbryoId), "Для переноса эмбриона нужен embryoId.", $"{path}.embryoId", errors);
                break;
        }
    }

    private static void Require(bool condition, string message, string path, List<AiValidationError> errors)
    {
        if (condition) return;
        errors.Add(Error(AiValidationRules.DailyActionCascade, "hidden backend service cascade", message, path));
    }

    private static IEnumerable<Guid> GetCowIds(InseminationItemDTO item)
    {
        if (item.CowIds is { Count: > 0 }) return item.CowIds;
        return item.CowId.HasValue ? new[] { item.CowId.Value } : Array.Empty<Guid>();
    }

    private static IEnumerable<Guid> GetBullIds(InseminationItemDTO item)
    {
        if (item.BullIds is { Count: > 0 }) return item.BullIds;

        if (!item.BullJson.HasValue ||
            item.BullJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return Array.Empty<Guid>();
        }

        var ids = new List<Guid>();
        CollectBullIds(item.BullJson.Value, ids);
        return ids;
    }

    private static void CollectBullIds(JsonElement element, List<Guid> ids)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                if (Guid.TryParse(element.GetString(), out var stringId))
                    ids.Add(stringId);
                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    CollectBullIds(property.Value, ids);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectBullIds(item, ids);
                break;
        }
    }

    private static AiToolValidationResult BuildResult(List<AiValidationError> errors, int retryAttempt)
        => errors.Count == 0 ? AiToolValidationResult.Valid() : AiToolValidationResult.FromErrors(errors, retryAttempt);

    private static AiValidationError Error(
        string ruleId,
        string source,
        string message,
        string path,
        bool retryableByLlm = true)
        => new(ruleId, source, message, path, RetryableByLlm: retryableByLlm);
}
