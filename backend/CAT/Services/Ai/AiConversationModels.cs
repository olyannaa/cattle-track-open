namespace CAT.Services.Ai;

public sealed class AiConversationState
{
    public string ConversationId { get; set; } = string.Empty;

    public Guid OrganizationId { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public Guid? ActiveDraftId { get; set; }

    public List<AiConversationMessage> Messages { get; set; } = new();
}

public sealed class AiConversationMessage
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}
