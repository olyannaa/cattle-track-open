using System.Text.Json;

namespace CAT.Controllers.DTO.AiAssistant;

public sealed class AiAssistantTextRequestDTO
{
    public string Text { get; set; } = string.Empty;

    public string? ClientRequestId { get; set; }

    public string? ConversationId { get; set; }
}

public sealed class AiAssistantConfirmRequestDTO
{
    public bool Confirm { get; set; } = true;

    public string? IdempotencyKey { get; set; }

    public bool ConfirmPartial { get; set; }
}

public class AiAssistantResponseDTO
{
    public Guid DraftId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public AiAssistantPreviewDTO? Preview { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public string ConversationId { get; set; } = string.Empty;
}

public sealed class AiAssistantSelectCandidateRequestDTO
{
    public string CandidateId { get; set; } = string.Empty;
}

public sealed class AiAssistantSelectReadCandidateRequestDTO
{
    public string ToolName { get; set; } = string.Empty;

    public string CandidateId { get; set; } = string.Empty;

    public string? ConversationId { get; set; }
}

public sealed class AiAssistantVoiceResponseDTO : AiAssistantResponseDTO
{
    public string Transcript { get; set; } = string.Empty;

    public string RawTranscript { get; set; } = string.Empty;

    public string AsrModel { get; set; } = string.Empty;

    public double? AsrLatencySeconds { get; set; }
}

public sealed class AiAssistantConfirmResponseDTO
{
    public Guid DraftId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public JsonElement? CommitResult { get; set; }
}

public sealed class AiAssistantPreviewDTO
{
    public string ToolName { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public JsonElement? Arguments { get; set; }

    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}

public sealed class AiAssistantDraftDTO
{
    public Guid DraftId { get; set; }

    public Guid OrganizationId { get; set; }

    public string UserText { get; set; } = string.Empty;

    public string? ClientRequestId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public string ToolName { get; set; } = string.Empty;

    public JsonElement? Arguments { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool CanCommit { get; set; }

    public bool RequiresPartialConfirm { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }
}

public static class AiAssistantDraftStatus
{
    public const string Preview = "preview";
    public const string FinalAnswer = "final_answer";
    public const string Unsupported = "unsupported";
    public const string Canceled = "canceled";
    public const string Committed = "committed";
    public const string ConfirmExpired = "confirm_expired";
    public const string CannotCommit = "cannot_commit";
}

public static class AiAssistantToolNames
{
    public const string Unsupported = "unsupported";
    public const string FindAnimal = "find_animal";
    public const string GetAnimalCard = "get_animal_card";
    public const string GetAnimalParents = "get_animal_parents";
    public const string GetWeightHistory = "get_weight_history";
    public const string GetPregnanciesToCheck = "get_pregnancies_to_check";
    public const string ListGroups = "list_groups";
    public const string CreateWeight = "create_weight";
    public const string CreateDailyAction = "create_daily_action";
    public const string CreateInsemination = "create_insemination";
}
