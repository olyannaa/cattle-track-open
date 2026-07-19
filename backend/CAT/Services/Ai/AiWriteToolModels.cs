using System.Text.Json;
using CAT.Controllers.DTO;
using CAT.Controllers.DTO.AiAssistant;

namespace CAT.Services.Ai;

public static class AiWriteItemStatus
{
    public const string Resolved = "resolved";
    public const string Ambiguous = "ambiguous";
    public const string NotFound = "not_found";
    public const string Invalid = "invalid";
    public const string Committed = "committed";
    public const string Failed = "failed";
    public const string Skipped = "skipped";
}

public sealed class AiWriteDraftPayload
{
    public string SchemaVersion { get; set; } = "v1";

    public string ToolName { get; set; } = string.Empty;

    public string? BatchIdempotencyKey { get; set; }

    public JsonElement? SourceArguments { get; set; }

    public Dictionary<string, Guid> SelectedAnimalIds { get; set; } = new();

    public List<AiWriteDraftItem> Items { get; set; } = new();

    public int CommitReadyCount => Items.Count(i => i.CanCommit);

    public bool RequiresPartialConfirm => CommitReadyCount > 0 && Items.Any(i => !i.CanCommit);
}

public sealed class AiWriteDraftItem
{
    public int Index { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string? Tag { get; set; }

    public string Status { get; set; } = AiWriteItemStatus.Invalid;

    public bool CanCommit { get; set; }

    public string Message { get; set; } = string.Empty;

    public List<AiDisambiguationCandidate> Candidates { get; set; } = new();

    public WeightCreateDTO? Weight { get; set; }

    public CreateDailyActionDTO? DailyAction { get; set; }

    public InseminationItemDTO? Insemination { get; set; }

    public List<AiValidationError> ValidationErrors { get; set; } = new();
}

public sealed record AiWritePreviewResponse(
    string SchemaVersion,
    string ToolName,
    string? BatchIdempotencyKey,
    int Total,
    int CommitReady,
    int Ambiguous,
    int NotFound,
    int Invalid,
    bool RequiresPartialConfirm,
    IReadOnlyList<AiWriteItemPreview> Items,
    string Text,
    string Voice);

public sealed record AiWriteItemPreview(
    int Index,
    string IdempotencyKey,
    string? Tag,
    string Status,
    bool CanCommit,
    string Message,
    IReadOnlyList<AiDisambiguationCandidate> Candidates,
    object? Preview,
    IReadOnlyList<AiValidationError> ValidationErrors);

public sealed record AiWriteCommitReport(
    string SchemaVersion,
    string ToolName,
    Guid DraftId,
    int Total,
    int Committed,
    int Failed,
    int Skipped,
    IReadOnlyList<AiWriteCommitItemReport> Items,
    string Text,
    string Voice);

public sealed record AiWriteCommitItemReport(
    int Index,
    string IdempotencyKey,
    string? Tag,
    string Status,
    string Message);

public static class AiWriteAssistantMessages
{
    public static string ForPreview(AiWriteDraftPayload payload)
    {
        var ready = payload.Items.Where(item => item.CanCommit).ToList();
        var unresolved = payload.Items.Where(item => !item.CanCommit).ToList();

        if (ready.Count > 0 && unresolved.Count == 0)
        {
            return $"Я подготовила {DescribeOperation(payload.ToolName, ready.Count)}{DescribeTags(ready)}. " +
                   "Проверьте данные и подтвердите сохранение.";
        }

        var clarification = DescribeClarification(unresolved);
        if (ready.Count > 0)
        {
            return $"Я подготовила {DescribeOperation(payload.ToolName, ready.Count)}{DescribeTags(ready)}. " +
                   $"{clarification} Готовые записи можно подтвердить отдельно.";
        }

        return $"Пока ничего не сохранено. {clarification}";
    }

    public static string ForCommit(AiWriteCommitReport report)
    {
        var committed = report.Items.Where(item => item.Status == AiWriteItemStatus.Committed).ToList();
        var unsuccessful = report.Items.Where(item => item.Status is AiWriteItemStatus.Failed or AiWriteItemStatus.Skipped).ToList();

        if (committed.Count > 0 && unsuccessful.Count == 0)
        {
            return $"Информация внесена: {DescribeOperation(report.ToolName, committed.Count)}{DescribeTags(committed)}.";
        }

        if (committed.Count > 0)
        {
            return $"Информация внесена для {DescribeTags(committed, includePrefix: false)}. " +
                   $"Для остальных животных запись не выполнена: {DescribeCommitProblem(unsuccessful)}";
        }

        return $"Информация не внесена: {DescribeCommitProblem(unsuccessful)}";
    }

    private static string DescribeOperation(string toolName, int count)
    {
        var singular = toolName switch
        {
            AiAssistantToolNames.CreateWeight => "запись о весе",
            AiAssistantToolNames.CreateDailyAction => "ежедневное действие",
            AiAssistantToolNames.CreateInsemination => "запись об осеменении",
            _ => "запись"
        };

        if (count == 1)
            return singular;

        return toolName switch
        {
            AiAssistantToolNames.CreateWeight => "записи о весе",
            AiAssistantToolNames.CreateDailyAction => "ежедневные действия",
            AiAssistantToolNames.CreateInsemination => "записи об осеменении",
            _ => "записи"
        };
    }

    private static string DescribeTags(IEnumerable<AiWriteDraftItem> items, bool includePrefix = true)
        => DescribeTags(items.Select(item => item.Tag));

    private static string DescribeTags(IEnumerable<AiWriteCommitItemReport> items, bool includePrefix = true)
        => DescribeTags(items.Select(item => item.Tag), includePrefix);

    private static string DescribeTags(IEnumerable<string?> tags, bool includePrefix = true)
    {
        var values = GetTagValues(tags);

        if (values.Count == 0)
            return string.Empty;

        var label = values.Count == 1 ? "животного с биркой" : "животных с бирками";
        return $" {includePrefix switch { true => "для ", false => string.Empty }}{label} {string.Join(", ", values)}";
    }

    private static List<string> GetTagValues(IEnumerable<string?> tags)
        => tags.Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string DescribeClarification(IReadOnlyCollection<AiWriteDraftItem> items)
    {
        var ambiguous = items.Where(item => item.Status == AiWriteItemStatus.Ambiguous).ToList();
        if (ambiguous.Count > 0)
        {
            var tags = GetTagValues(ambiguous.Select(item => item.Tag));
            var tagLabel = tags.Count == 1 ? "биркой" : "бирками";
            return $"Нашлось несколько животных с {tagLabel} {string.Join(", ", tags)}. " +
                   "Выберите нужное животное по карточке или уточните дату рождения либо дополнительный идентификатор.";
        }

        var notFound = items.Where(item => item.Status == AiWriteItemStatus.NotFound).ToList();
        if (notFound.Count > 0)
        {
            var tags = GetTagValues(notFound.Select(item => item.Tag));
            var tagLabel = tags.Count == 1 ? "биркой" : "бирками";
            return $"Я не нашла животное с {tagLabel} {string.Join(", ", tags)}. Проверьте номер бирки.";
        }

        var error = items.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Message));
        return error?.Message ?? "Нужно уточнить данные для записи.";
    }

    private static string DescribeCommitProblem(IReadOnlyCollection<AiWriteCommitItemReport> items)
    {
        var first = items.FirstOrDefault();
        if (first == null)
            return "попробуйте ещё раз.";

        var tags = DescribeTags(items, includePrefix: false).Trim();
        return string.IsNullOrWhiteSpace(tags)
            ? first.Message
            : $"{tags}: {first.Message}";
    }
}
