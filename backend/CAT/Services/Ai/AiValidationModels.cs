using CAT.Controllers.DTO;

namespace CAT.Services.Ai;

public enum AiValidationSeverity
{
    Error,
    Warning
}

public enum AiValidationRetryAction
{
    None,
    RetryLlmOnce,
    ShowHumanError,
    ShowPreview
}

public sealed record AiValidationError(
    string RuleId,
    string Source,
    string Message,
    string Path,
    AiValidationSeverity Severity = AiValidationSeverity.Error,
    bool RetryableByLlm = true);

public sealed class AiToolValidationResult
{
    private readonly List<AiValidationError> _errors = new();

    public IReadOnlyList<AiValidationError> Errors => _errors;

    public bool IsValid => _errors.All(e => e.Severity != AiValidationSeverity.Error);

    public AiValidationRetryAction RetryAction { get; private set; } = AiValidationRetryAction.ShowPreview;

    public static AiToolValidationResult Valid()
        => new() { RetryAction = AiValidationRetryAction.ShowPreview };

    public static AiToolValidationResult FromErrors(IEnumerable<AiValidationError> errors, int retryAttempt)
    {
        var result = new AiToolValidationResult();
        result._errors.AddRange(errors);
        result.RetryAction = result.ComputeRetryAction(retryAttempt);
        return result;
    }

    private AiValidationRetryAction ComputeRetryAction(int retryAttempt)
    {
        if (IsValid) return AiValidationRetryAction.ShowPreview;
        if (retryAttempt <= 0 && _errors.Any(e => e.RetryableByLlm))
            return AiValidationRetryAction.RetryLlmOnce;
        return AiValidationRetryAction.ShowHumanError;
    }
}

public sealed record AiAnimalFacts(
    Guid Id,
    DateOnly? BirthDate,
    string? Type,
    string? Status);

public sealed record AiFeedingPercentage(
    string Name,
    decimal Percentage);

public static class AiValidationRules
{
    public const string OrgAnimalScope = "AI-VAL-ORG-ANIMAL";
    public const string OrgGroupScope = "AI-VAL-ORG-GROUP";
    public const string RequiredField = "AI-VAL-REQUIRED";
    public const string DateNotFuture = "AI-VAL-DATE-FUTURE";
    public const string DateAfterBirth = "AI-VAL-DATE-BIRTH";
    public const string DuplicateWeight = "AI-VAL-DUP-WEIGHT";
    public const string WeightRange = "AI-VAL-WEIGHT-RANGE";
    public const string EnumKnown = "AI-VAL-ENUM-KNOWN";
    public const string DailyActionCascade = "AI-VAL-DAILY-CASCADE";
    public const string DuplicateDailyAction = "AI-VAL-DUP-DAILY";
    public const string DuplicateInsemination = "AI-VAL-DUP-INSEMINATION";
    public const string FeedingPercentSum = "AI-VAL-FEED-PERCENT-SUM";
}

public interface IAiToolValidator
{
    AiToolValidationResult ValidateCreateWeight(Guid organizationId, WeightCreateDTO dto, int retryAttempt = 0);

    AiToolValidationResult ValidateCreateDailyActions(
        Guid organizationId,
        IEnumerable<CreateDailyActionDTO> items,
        int retryAttempt = 0);

    AiToolValidationResult ValidateCreateInsemination(
        Guid organizationId,
        InseminationBatchDTO dto,
        int retryAttempt = 0);

    AiToolValidationResult ValidateFeedingPercentages(
        IEnumerable<AiFeedingPercentage> percentages,
        int retryAttempt = 0);
}

public interface IAiToolValidationDataSource
{
    AiAnimalFacts? GetAnimalFacts(Guid organizationId, Guid? animalId);

    bool IsGroupInOrganization(Guid organizationId, Guid? groupId);

    bool WeightExists(Guid organizationId, Guid animalId, DateOnly date);

    bool DailyActionExists(Guid organizationId, Guid animalId, string? type, DateOnly? date, string? subtype);

    bool InseminationExists(Guid organizationId, Guid cowId, DateOnly date, string inseminationType);
}
