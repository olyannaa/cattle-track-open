using System.Text.Json;

namespace CAT.Services.Ai;

public static class AiAgentLoopDefaults
{
    public const int MaxIterations = 8;
}

public static class AiAgentOutputType
{
    public const string FinalAnswer = "final_answer";
    public const string ToolCall = "tool_call";
}

public static class AiAgentLoopStatus
{
    public const string FinalAnswer = "final_answer";
    public const string ToolResult = "tool_result";
    public const string FailedTool = "failed_tool";
    public const string IterationLimit = "iteration_limit";
    public const string DuplicateToolCall = "duplicate_tool_call";
    public const string InvalidOutput = "invalid_output";
}

public static class AiAgentMessageRole
{
    public const string User = "user";
    public const string Assistant = "assistant";
    public const string Tool = "tool";
}

public sealed class AiAgentSession
{
    private readonly List<AiAgentMessage> _history = new();

    public AiAgentSession(
        Guid organizationId,
        string userText,
        IReadOnlyList<AiAgentMessage>? priorHistory = null)
    {
        SessionId = Guid.NewGuid();
        OrganizationId = organizationId;
        UserText = userText;
        foreach (var message in priorHistory ?? Array.Empty<AiAgentMessage>())
        {
            if (message.Role is not (AiAgentMessageRole.User or AiAgentMessageRole.Assistant) ||
                string.IsNullOrWhiteSpace(message.Content))
                continue;

            Add(new AiAgentMessage { Role = message.Role, Content = message.Content });
        }

        Add(AiAgentMessage.User(userText));
    }

    public Guid SessionId { get; }

    public Guid OrganizationId { get; }

    public string UserText { get; }

    public IReadOnlyList<AiAgentMessage> History => _history;

    public void Add(AiAgentMessage message)
        => _history.Add(message);
}

public sealed class AiAgentMessage
{
    public string Role { get; set; } = string.Empty;

    public string? Content { get; set; }

    public AiAgentToolCall? ToolCall { get; set; }

    public AiAgentToolResult? ToolResult { get; set; }

    public static AiAgentMessage User(string content)
        => new() { Role = AiAgentMessageRole.User, Content = content };

    public static AiAgentMessage AssistantFinal(string content)
        => new() { Role = AiAgentMessageRole.Assistant, Content = content };

    public static AiAgentMessage AssistantToolCall(AiAgentToolCall toolCall)
        => new() { Role = AiAgentMessageRole.Assistant, ToolCall = toolCall };

    public static AiAgentMessage Tool(AiAgentToolResult toolResult)
        => new() { Role = AiAgentMessageRole.Tool, ToolResult = toolResult };
}

public sealed class AiAgentLlmOutput
{
    public string Type { get; set; } = string.Empty;

    public string? FinalAnswer { get; set; }

    public AiAgentToolCall? ToolCall { get; set; }

    public static AiAgentLlmOutput Final(string finalAnswer)
        => new() { Type = AiAgentOutputType.FinalAnswer, FinalAnswer = finalAnswer };

    public static AiAgentLlmOutput Call(string toolName, JsonElement? arguments = null)
        => new()
        {
            Type = AiAgentOutputType.ToolCall,
            ToolCall = new AiAgentToolCall { Name = toolName, Arguments = arguments }
        };
}

public sealed class AiAgentToolCall
{
    public string Name { get; set; } = string.Empty;

    public JsonElement? Arguments { get; set; }
}

public sealed class AiAgentToolResult
{
    public string ToolName { get; set; } = string.Empty;

    public bool Success { get; set; }

    public bool IsTerminal { get; set; }

    public bool CanCommit { get; set; }

    public string Summary { get; set; } = string.Empty;

    public JsonElement? Data { get; set; }

    public AiAgentError? Error { get; set; }

    public static AiAgentToolResult Ok(
        string toolName,
        string summary,
        JsonElement? data = null,
        bool isTerminal = false,
        bool canCommit = false)
        => new()
        {
            ToolName = toolName,
            Success = true,
            Summary = summary,
            Data = data,
            IsTerminal = isTerminal,
            CanCommit = canCommit
        };

    public static AiAgentToolResult Fail(string toolName, AiAgentError error)
        => new() { ToolName = toolName, Success = false, Summary = error.Message, Error = error };
}

public sealed class AiAgentError
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool Retryable { get; set; }

    public string? Path { get; set; }

    public static AiAgentError Create(string code, string message, bool retryable = false, string? path = null)
        => new() { Code = code, Message = message, Retryable = retryable, Path = path };
}

public sealed class AiAgentLoopResult
{
    public string Status { get; set; } = string.Empty;

    public string? FinalAnswer { get; set; }

    public AiAgentToolCall? ToolCall { get; set; }

    public AiAgentToolResult? ToolResult { get; set; }

    public AiAgentError? Error { get; set; }

    public IReadOnlyList<AiAgentMessage> History { get; set; } = Array.Empty<AiAgentMessage>();
}
