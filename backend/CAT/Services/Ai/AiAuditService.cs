using System.Security.Claims;
using System.Text.Json;
using CAT.Controllers.DTO;

namespace CAT.Services.Ai;

public static class AiAuditActionTypes
{
    public const string LlmTurn = "ai_llm_turn";
    public const string ToolCall = "ai_tool_call";
    public const string DraftCreated = "ai_draft_created";
    public const string Clarification = "ai_clarification";
    public const string Commit = "ai_commit";
    public const string LoopGuard = "ai_loop_guard";
    public const string AsrTranscription = "ai_asr_transcription";
}

public interface IAiAuditService
{
    void Log(Guid organizationId, string actionType, string status, object details, AiAgentError? error = null);
}

public sealed class AiAuditService : IAiAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] SensitiveNameFragments =
    {
        "password",
        "token",
        "secret",
        "apikey",
        "api_key",
        "authorization",
        "bearer",
        "connectionstring",
        "connection_string"
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserActionQueue _actionQueue;

    public AiAuditService(IHttpContextAccessor httpContextAccessor, UserActionQueue actionQueue)
    {
        _httpContextAccessor = httpContextAccessor;
        _actionQueue = actionQueue;
    }

    public void Log(Guid organizationId, string actionType, string status, object details, AiAgentError? error = null)
    {
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return;

        var additionalInfo = new
        {
            SchemaVersion = "v1",
            OrganizationId = organizationId,
            Event = new
            {
                ActionType = actionType,
                Status = status,
                CreatedAtUtc = DateTimeOffset.UtcNow
            },
            Details = SanitizeToElement(details),
            Error = error == null
                ? null
                : new
                {
                    error.Code,
                    Message = Truncate(error.Message, 500),
                    error.Retryable,
                    error.Path
                }
        };

        _actionQueue.Enqueue(UserActionDtoFactory.Create(
            userId,
            actionType,
            status: status,
            errorMessage: error?.Message,
            additionalInfo: JsonSerializer.Serialize(additionalInfo, JsonOptions),
            table: "ai_assistant"));
    }

    private static JsonElement SanitizeToElement(object details)
    {
        var serialized = JsonSerializer.SerializeToElement(details, JsonOptions);
        var sanitized = SanitizeElement(serialized, propertyName: null);
        return JsonSerializer.SerializeToElement(sanitized, JsonOptions);
    }

    private static object? SanitizeElement(JsonElement element, string? propertyName)
    {
        if (IsSensitiveName(propertyName))
            return "[REDACTED]";

        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => SanitizeElement(p.Value, p.Name)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(item => SanitizeElement(item, propertyName))
                .ToList(),
            JsonValueKind.String => Truncate(element.GetString(), 500),
            JsonValueKind.Number when element.TryGetInt64(out var value) => value,
            JsonValueKind.Number when element.TryGetDouble(out var value) => value,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static bool IsSensitiveName(string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return false;

        var normalized = propertyName.Replace("-", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        return SensitiveNameFragments.Any(fragment => normalized.Contains(fragment.Replace("_", string.Empty)));
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }
}

public sealed class DisabledAiAuditService : IAiAuditService
{
    public void Log(Guid organizationId, string actionType, string status, object details, AiAgentError? error = null)
    {
    }
}
