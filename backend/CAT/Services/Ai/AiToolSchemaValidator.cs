using System.Text.Json;
using System.Text.RegularExpressions;
using CAT.Controllers.DTO.AiAssistant;

namespace CAT.Services.Ai;

public interface IAiToolSchemaValidator
{
    AiToolSchemaValidationResult Validate(AiAgentToolCall toolCall);
}

public sealed class AiToolSchemaValidationResult
{
    public bool IsValid => Error == null;

    public AiAgentError? Error { get; set; }

    public static AiToolSchemaValidationResult Valid()
        => new();

    public static AiToolSchemaValidationResult Invalid(string message, string path)
        => new()
        {
            Error = AiAgentError.Create(
                "AI_TOOL_SCHEMA_INVALID",
                message,
                retryable: true,
                path)
        };
}

public sealed class AiToolSchemaValidator : IAiToolSchemaValidator
{
    private static readonly Regex IdempotencyKeyRegex = new("^[A-Za-z0-9._:-]{8,120}$", RegexOptions.Compiled);
    private static readonly Regex DateRegex = new("^[0-9]{4}-[0-9]{2}-[0-9]{2}$", RegexOptions.Compiled);

    private static readonly HashSet<string> WeightMethods = new(StringComparer.Ordinal)
    {
        "Автоматическая весовая станция",
        "Ручное взвешивание",
        "Расчетный метод",
        "__unknown"
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
        "Изменение половозрастной группы",
        "__unknown"
    };

    private static readonly HashSet<string> InseminationTypes = new(StringComparer.Ordinal)
    {
        "Искусственное",
        "Естественное",
        "Эмбрион",
        "__unknown"
    };

    public AiToolSchemaValidationResult Validate(AiAgentToolCall toolCall)
    {
        if (string.IsNullOrWhiteSpace(toolCall.Name))
            return Invalid("Не удалось выбрать действие. Сформулируйте запрос подробнее.", "$.name");

        if (!toolCall.Arguments.HasValue || toolCall.Arguments.Value.ValueKind != JsonValueKind.Object)
            return Invalid("Не удалось разобрать параметры команды. Повторите запрос чуть подробнее.", "$.arguments");

        var root = toolCall.Arguments.Value;
        var version = GetString(root, "schema_version");
        if (!string.Equals(version, "v1", StringComparison.Ordinal))
            return Invalid("Не удалось разобрать версию команды. Повторите запрос.", "$.schema_version");

        return toolCall.Name switch
        {
            AiAssistantToolNames.FindAnimal => RequireTag(root, "$.tag"),
            AiAssistantToolNames.GetAnimalParents => RequireTag(root, "$.tag"),
            AiAssistantToolNames.GetAnimalCard => ValidateAnimalLookup(root),
            AiAssistantToolNames.GetWeightHistory => ValidateWeightHistory(root),
            AiAssistantToolNames.GetPregnanciesToCheck => ValidateOptionalDate(root, "due_before", "$.due_before"),
            AiAssistantToolNames.ListGroups => ValidateOptionalBoolean(root, "include_empty", "$.include_empty"),
            AiAssistantToolNames.CreateWeight => ValidateCreateWeight(root),
            AiAssistantToolNames.CreateDailyAction => ValidateCreateDailyAction(root),
            AiAssistantToolNames.CreateInsemination => ValidateCreateInsemination(root),
            _ => Invalid("Такое действие пока не поддержано.", "$.name")
        };
    }

    private static AiToolSchemaValidationResult ValidateAnimalLookup(JsonElement root)
    {
        if (HasNonEmptyString(root, "tag"))
            return AiToolSchemaValidationResult.Valid();

        if (HasValidUuid(root, "animal_id"))
            return AiToolSchemaValidationResult.Valid();

        return Invalid("Нужно указать бирку животного.", "$.tag");
    }

    private static AiToolSchemaValidationResult ValidateWeightHistory(JsonElement root)
    {
        var lookup = ValidateAnimalLookup(root);
        if (!lookup.IsValid) return lookup;

        var dateFrom = ValidateOptionalDate(root, "date_from", "$.date_from");
        if (!dateFrom.IsValid) return dateFrom;

        var dateTo = ValidateOptionalDate(root, "date_to", "$.date_to");
        if (!dateTo.IsValid) return dateTo;

        if (root.TryGetProperty("limit", out var limit) &&
            (limit.ValueKind != JsonValueKind.Number ||
             !limit.TryGetInt32(out var value) ||
             value is < 1 or > 100))
        {
            return Invalid("Лимит истории веса должен быть от 1 до 100.", "$.limit");
        }

        return AiToolSchemaValidationResult.Valid();
    }

    private static AiToolSchemaValidationResult ValidateCreateWeight(JsonElement root)
    {
        var key = RequireIdempotencyKey(root, "idempotency_key", "$.idempotency_key");
        if (!key.IsValid) return key;

        var tag = RequireTag(root, "$.tag");
        if (!tag.IsValid) return tag;

        if (root.TryGetProperty("weight", out var weight) &&
            (weight.ValueKind != JsonValueKind.Number ||
             !weight.TryGetDouble(out var value) ||
             value <= 0 ||
             value > 3000))
        {
            return Invalid("Вес должен быть числом от 1 до 3000 кг.", "$.weight");
        }

        var date = ValidateOptionalDate(root, "date", "$.date");
        if (!date.IsValid) return date;

        var method = ValidateOptionalEnum(root, "method", WeightMethods, "$.method", "Метод взвешивания не распознан.");
        if (!method.IsValid) return method;

        if (string.Equals(GetString(root, "method"), "__unknown", StringComparison.Ordinal) &&
            !HasNonEmptyString(root, "method_raw"))
        {
            return Invalid("Нужно уточнить метод взвешивания.", "$.method_raw");
        }

        return AiToolSchemaValidationResult.Valid();
    }

    private static AiToolSchemaValidationResult ValidateCreateDailyAction(JsonElement root)
    {
        var key = RequireIdempotencyKey(root, "batch_idempotency_key", "$.batch_idempotency_key");
        if (!key.IsValid) return key;

        if (!root.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array ||
            items.GetArrayLength() == 0)
        {
            return Invalid("Нужно указать хотя бы одно животное и действие.", "$.items");
        }

        var index = 0;
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                return Invalid("Каждая строка действия должна быть объектом.", $"$.items[{index}]");

            var itemKey = RequireIdempotencyKey(item, "idempotency_key", $"$.items[{index}].idempotency_key");
            if (!itemKey.IsValid) return itemKey;

            var tag = RequireTag(item, $"$.items[{index}].tag");
            if (!tag.IsValid) return tag;

            var type = ValidateOptionalEnum(item, "type", DailyActionTypes, $"$.items[{index}].type", "Тип ежедневного действия не распознан.");
            if (!type.IsValid) return type;

            var date = ValidateOptionalDate(item, "date", $"$.items[{index}].date");
            if (!date.IsValid) return date;

            index++;
        }

        return AiToolSchemaValidationResult.Valid();
    }

    private static AiToolSchemaValidationResult ValidateCreateInsemination(JsonElement root)
    {
        var key = RequireIdempotencyKey(root, "batch_idempotency_key", "$.batch_idempotency_key");
        if (!key.IsValid) return key;

        if (!root.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array ||
            items.GetArrayLength() == 0)
        {
            return Invalid("Нужно указать хотя бы одну корову для осеменения.", "$.items");
        }

        var index = 0;
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                return Invalid("Каждая строка осеменения должна быть объектом.", $"$.items[{index}]");

            var itemKey = RequireIdempotencyKey(item, "idempotency_key", $"$.items[{index}].idempotency_key");
            if (!itemKey.IsValid) return itemKey;

            if (!item.TryGetProperty("cow_tags", out var cowTags) ||
                cowTags.ValueKind != JsonValueKind.Array ||
                cowTags.GetArrayLength() == 0)
            {
                return Invalid("Нужно указать бирку коровы.", $"$.items[{index}].cow_tags");
            }

            for (var cowIndex = 0; cowIndex < cowTags.GetArrayLength(); cowIndex++)
            {
                if (!IsNonEmptyString(cowTags[cowIndex]))
                    return Invalid("Бирка коровы должна быть строкой.", $"$.items[{index}].cow_tags[{cowIndex}]");
            }

            var date = ValidateOptionalDate(item, "date", $"$.items[{index}].date");
            if (!date.IsValid) return date;

            var type = ValidateOptionalEnum(item, "insemination_type", InseminationTypes, $"$.items[{index}].insemination_type", "Тип осеменения не распознан.");
            if (!type.IsValid) return type;

            if (item.TryGetProperty("bull_tags", out var bullTags) &&
                (bullTags.ValueKind != JsonValueKind.Array || bullTags.GetArrayLength() == 0))
            {
                return Invalid("Бирки быков должны быть непустым списком.", $"$.items[{index}].bull_tags");
            }

            index++;
        }

        return AiToolSchemaValidationResult.Valid();
    }

    private static AiToolSchemaValidationResult RequireTag(JsonElement root, string path)
        => HasNonEmptyString(root, "tag")
            ? AiToolSchemaValidationResult.Valid()
            : Invalid("Нужно указать бирку животного.", path);

    private static AiToolSchemaValidationResult RequireIdempotencyKey(JsonElement root, string property, string path)
    {
        var value = GetString(root, property);
        return !string.IsNullOrWhiteSpace(value) && IdempotencyKeyRegex.IsMatch(value)
            ? AiToolSchemaValidationResult.Valid()
            : Invalid("Не удалось подготовить безопасный ключ повтора команды. Повторите запрос.", path);
    }

    private static AiToolSchemaValidationResult ValidateOptionalBoolean(JsonElement root, string property, string path)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return AiToolSchemaValidationResult.Valid();

        return value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False
            ? AiToolSchemaValidationResult.Valid()
            : Invalid("Параметр должен быть да/нет.", path);
    }

    private static AiToolSchemaValidationResult ValidateOptionalDate(JsonElement root, string property, string path)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return AiToolSchemaValidationResult.Valid();

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        return !string.IsNullOrWhiteSpace(text) &&
               DateRegex.IsMatch(text) &&
               DateOnly.TryParseExact(text, "yyyy-MM-dd", out _)
            ? AiToolSchemaValidationResult.Valid()
            : Invalid("Дата должна быть в формате год-месяц-день.", path);
    }

    private static AiToolSchemaValidationResult ValidateOptionalEnum(
        JsonElement root,
        string property,
        HashSet<string> allowed,
        string path,
        string message)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return AiToolSchemaValidationResult.Valid();

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        return !string.IsNullOrWhiteSpace(text) && allowed.Contains(text)
            ? AiToolSchemaValidationResult.Valid()
            : Invalid(message, path);
    }

    private static bool HasNonEmptyString(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) && IsNonEmptyString(value);

    private static bool IsNonEmptyString(JsonElement value)
        => value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString());

    private static bool HasValidUuid(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) &&
           value.ValueKind == JsonValueKind.String &&
           Guid.TryParse(value.GetString(), out _);

    private static string? GetString(JsonElement root, string property)
        => root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static AiToolSchemaValidationResult Invalid(string message, string path)
        => AiToolSchemaValidationResult.Invalid(message, path);
}
