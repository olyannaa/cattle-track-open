using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CAT.Controllers.DTO.AiAssistant;

namespace CAT.Services.Ai;

public interface IAiAgentLoop
{
    Task<AiAgentLoopResult> RunAsync(
        Guid organizationId,
        string userText,
        IReadOnlyList<AiAgentMessage>? priorHistory = null,
        CancellationToken cancellationToken = default);
}

public interface IAiAgentLlmClient
{
    Task<AiAgentLlmOutput> GetNextAsync(
        AiAgentSession session,
        CancellationToken cancellationToken = default);
}

public interface IAiToolExecutor
{
    Task<AiAgentToolResult> ExecuteAsync(
        Guid organizationId,
        AiAgentToolCall toolCall,
        CancellationToken cancellationToken = default);
}

public interface IAiConstrainedOutputValidator
{
    AiConstrainedOutputValidationResult Validate(AiAgentLlmOutput output);
}

public sealed class AiConstrainedOutputValidationResult
{
    public bool IsValid => Error == null;

    public AiAgentError? Error { get; set; }

    public static AiConstrainedOutputValidationResult Valid()
        => new();

    public static AiConstrainedOutputValidationResult Invalid(string message, string? path = null)
        => new()
        {
            Error = AiAgentError.Create(
                "AI_AGENT_CONSTRAINED_OUTPUT_INVALID",
                message,
                retryable: false,
                path)
        };
}

public sealed class AiAgentLoop : IAiAgentLoop
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiAgentLlmClient _llmClient;
    private readonly IAiToolExecutor _toolExecutor;
    private readonly IAiConstrainedOutputValidator _constrainedOutputValidator;
    private readonly IAiToolSchemaValidator _toolSchemaValidator;
    private readonly IAiAuditService _auditService;

    public AiAgentLoop(
        IAiAgentLlmClient llmClient,
        IAiToolExecutor toolExecutor,
        IAiConstrainedOutputValidator constrainedOutputValidator,
        IAiToolSchemaValidator toolSchemaValidator,
        IAiAuditService auditService)
    {
        _llmClient = llmClient;
        _toolExecutor = toolExecutor;
        _constrainedOutputValidator = constrainedOutputValidator;
        _toolSchemaValidator = toolSchemaValidator;
        _auditService = auditService;
    }

    public async Task<AiAgentLoopResult> RunAsync(
        Guid organizationId,
        string userText,
        IReadOnlyList<AiAgentMessage>? priorHistory = null,
        CancellationToken cancellationToken = default)
    {
        var session = new AiAgentSession(organizationId, userText, priorHistory);
        var seenToolCalls = new HashSet<string>(StringComparer.Ordinal);
        var schemaRetryUsed = false;

        if (TryBuildDeterministicWriteToolCall(userText, out var deterministicWriteToolCall))
        {
            return await ExecuteDeterministicToolCallAsync(
                organizationId,
                session,
                deterministicWriteToolCall,
                "deterministic_write_guard",
                cancellationToken);
        }

        if (TryBuildDeterministicReadToolCall(userText, out var deterministicToolCall))
        {
            return await ExecuteDeterministicToolCallAsync(
                organizationId,
                session,
                deterministicToolCall,
                "deterministic_read_guard",
                cancellationToken);
        }

        for (var iteration = 0; iteration < AiAgentLoopDefaults.MaxIterations; iteration++)
        {
            var output = await _llmClient.GetNextAsync(session, cancellationToken);
            _auditService.Log(
                organizationId,
                AiAuditActionTypes.LlmTurn,
                "success",
                new
                {
                    session.SessionId,
                    Iteration = iteration + 1,
                    OutputType = output?.Type,
                    FinalAnswerPreview = TrimForAudit(output?.FinalAnswer),
                    ToolName = output?.ToolCall?.Name,
                    Arguments = output?.ToolCall?.Arguments
                });

            var constrained = _constrainedOutputValidator.Validate(output);
            if (!constrained.IsValid)
            {
                _auditService.Log(
                    organizationId,
                    AiAuditActionTypes.LoopGuard,
                    "error",
                    new
                    {
                        session.SessionId,
                        Iteration = iteration + 1,
                        Guard = AiAgentLoopStatus.InvalidOutput,
                        OutputType = output?.Type
                    },
                    constrained.Error);

                return BuildResult(
                    AiAgentLoopStatus.InvalidOutput,
                    session,
                    error: constrained.Error);
            }

            if (output.Type == AiAgentOutputType.FinalAnswer &&
                TryRecoverToolCallFromFinalAnswer(output.FinalAnswer, out var recoveredToolCall))
            {
                output = AiAgentLlmOutput.Call(recoveredToolCall.Name, recoveredToolCall.Arguments);
            }

            if (output.Type == AiAgentOutputType.FinalAnswer)
            {
                var answer = output.FinalAnswer?.Trim() ?? string.Empty;
                session.Add(AiAgentMessage.AssistantFinal(answer));
                return BuildResult(
                    AiAgentLoopStatus.FinalAnswer,
                    session,
                    finalAnswer: answer);
            }

            var toolCall = NormalizeToolCallFromUserText(output.ToolCall!, userText);
            var schema = _toolSchemaValidator.Validate(toolCall);
            if (!schema.IsValid)
            {
                _auditService.Log(
                    organizationId,
                    AiAuditActionTypes.LoopGuard,
                    "error",
                    new
                    {
                        session.SessionId,
                        Iteration = iteration + 1,
                        Guard = "schema_validation",
                        ToolName = toolCall.Name,
                        Arguments = toolCall.Arguments
                    },
                    schema.Error);

                if (!schemaRetryUsed && schema.Error?.Retryable == true)
                {
                    schemaRetryUsed = true;
                    session.Add(AiAgentMessage.AssistantFinal(
                        $"Предыдущий tool call отклонён валидатором схемы: {schema.Error.Message} " +
                        "Верни исправленный tool_call по той же пользовательской команде. Не задавай вопрос, если можно исправить структуру."));
                    continue;
                }

                return BuildResult(
                    AiAgentLoopStatus.InvalidOutput,
                    session,
                    toolCall: toolCall,
                    error: schema.Error);
            }

            var callKey = BuildToolCallKey(toolCall);
            if (!seenToolCalls.Add(callKey))
            {
                var error = AiAgentError.Create(
                    "AI_AGENT_DUPLICATE_TOOL_CALL",
                    "LLM повторил тот же tool call без изменения аргументов.",
                    retryable: false);

                _auditService.Log(
                    organizationId,
                    AiAuditActionTypes.LoopGuard,
                    "error",
                    new
                    {
                        session.SessionId,
                        Iteration = iteration + 1,
                        Guard = AiAgentLoopStatus.DuplicateToolCall,
                        ToolName = toolCall.Name,
                        Arguments = toolCall.Arguments
                    },
                    error);

                return BuildResult(
                    AiAgentLoopStatus.DuplicateToolCall,
                    session,
                    toolCall: toolCall,
                    error: error);
            }

            session.Add(AiAgentMessage.AssistantToolCall(toolCall));

            var toolResult = await _toolExecutor.ExecuteAsync(organizationId, toolCall, cancellationToken);
            session.Add(AiAgentMessage.Tool(toolResult));
            _auditService.Log(
                organizationId,
                AiAuditActionTypes.ToolCall,
                toolResult.Success ? "success" : "error",
                new
                {
                    session.SessionId,
                    Iteration = iteration + 1,
                    ToolName = toolCall.Name,
                    Arguments = toolCall.Arguments,
                    toolResult.Success,
                    toolResult.IsTerminal,
                    toolResult.CanCommit,
                    Summary = TrimForAudit(toolResult.Summary),
                    Result = toolResult.Data
                },
                toolResult.Error);

            if (!toolResult.Success)
            {
                return BuildResult(
                    AiAgentLoopStatus.FailedTool,
                    session,
                    toolCall: toolCall,
                    toolResult: toolResult,
                    error: toolResult.Error);
            }

            if (toolResult.IsTerminal)
            {
                return BuildResult(
                    AiAgentLoopStatus.ToolResult,
                    session,
                    toolCall: toolCall,
                    toolResult: toolResult);
            }
        }

        var limitError = AiAgentError.Create(
            "AI_AGENT_ITERATION_LIMIT",
            $"Agent loop остановлен по лимиту {AiAgentLoopDefaults.MaxIterations} итераций.",
            retryable: false);

        _auditService.Log(
            organizationId,
            AiAuditActionTypes.LoopGuard,
            "error",
            new
            {
                session.SessionId,
                Guard = AiAgentLoopStatus.IterationLimit,
                MaxIterations = AiAgentLoopDefaults.MaxIterations
            },
            limitError);

        return BuildResult(
            AiAgentLoopStatus.IterationLimit,
            session,
            error: limitError);
    }

    private static AiAgentLoopResult BuildResult(
        string status,
        AiAgentSession session,
        string? finalAnswer = null,
        AiAgentToolCall? toolCall = null,
        AiAgentToolResult? toolResult = null,
        AiAgentError? error = null)
        => new()
        {
            Status = status,
            FinalAnswer = finalAnswer,
            ToolCall = toolCall,
            ToolResult = toolResult,
            Error = error,
            History = session.History.ToArray()
        };

    private static string BuildToolCallKey(AiAgentToolCall toolCall)
    {
        var argumentsJson = toolCall.Arguments.HasValue
            ? JsonSerializer.Serialize(toolCall.Arguments.Value, JsonOptions)
            : "{}";

        return $"{toolCall.Name.Trim()}:{argumentsJson}";
    }

    private async Task<AiAgentLoopResult> ExecuteDeterministicToolCallAsync(
        Guid organizationId,
        AiAgentSession session,
        AiAgentToolCall toolCall,
        string guard,
        CancellationToken cancellationToken)
    {
        session.Add(AiAgentMessage.AssistantToolCall(toolCall));
        var toolResult = await _toolExecutor.ExecuteAsync(
            organizationId,
            toolCall,
            cancellationToken);
        session.Add(AiAgentMessage.Tool(toolResult));

        _auditService.Log(
            organizationId,
            AiAuditActionTypes.ToolCall,
            toolResult.Success ? "success" : "error",
            new
            {
                session.SessionId,
                Iteration = 0,
                Guard = guard,
                ToolName = toolCall.Name,
                Arguments = toolCall.Arguments,
                toolResult.Success,
                toolResult.IsTerminal,
                toolResult.CanCommit,
                Summary = TrimForAudit(toolResult.Summary),
                Result = toolResult.Data
            },
            toolResult.Error);

        return BuildResult(
            toolResult.Success ? AiAgentLoopStatus.ToolResult : AiAgentLoopStatus.FailedTool,
            session,
            toolCall: toolCall,
            toolResult: toolResult,
            error: toolResult.Error);
    }

    private static bool TryBuildDeterministicWriteToolCall(string userText, out AiAgentToolCall toolCall)
    {
        toolCall = new AiAgentToolCall();
        if (string.IsNullOrWhiteSpace(userText))
            return false;

        var normalizedText = userText.Trim().ToLowerInvariant();
        if (TryBuildDeterministicWeightToolCall(userText, normalizedText, out toolCall))
            return true;

        if (TryBuildDeterministicDailyActionToolCall(userText, normalizedText, out toolCall))
            return true;

        if (!normalizedText.Contains("осемен", StringComparison.Ordinal) ||
            !TryExtractEntityTags(userText, out var refs) ||
            refs.CowTags.Count == 0)
        {
            return false;
        }

        var firstCow = refs.CowTags[0];
        var date = normalizedText.Contains("сегодня", StringComparison.Ordinal)
            ? DateOnly.FromDateTime(DateTime.Today)
            : DateOnly.FromDateTime(DateTime.Today);
        var batchKey = BuildIdempotencyKey("insemination", date, firstCow, normalizedText);
        var item = new JsonObject
        {
            ["idempotency_key"] = $"{batchKey}:{NormalizeKeyPart(firstCow)}",
            ["cow_tags"] = ToJsonArray(refs.CowTags),
            ["date"] = date.ToString("yyyy-MM-dd")
        };

        if (normalizedText.Contains("естествен", StringComparison.Ordinal))
            item["insemination_type"] = "Естественное";
        else if (normalizedText.Contains("искусствен", StringComparison.Ordinal))
            item["insemination_type"] = "Искусственное";
        else if (normalizedText.Contains("эмбри", StringComparison.Ordinal))
            item["insemination_type"] = "Эмбрион";
        else
        {
            item["insemination_type"] = "__unknown";
            item["insemination_type_raw"] = "не указано";
        }

        if (refs.BullTags.Count > 0)
            item["bull_tags"] = ToJsonArray(refs.BullTags);

        var root = new JsonObject
        {
            ["schema_version"] = "v1",
            ["batch_idempotency_key"] = batchKey,
            ["items"] = new JsonArray(item)
        };

        toolCall = new AiAgentToolCall
        {
            Name = AiAssistantToolNames.CreateInsemination,
            Arguments = JsonSerializer.SerializeToElement(root, JsonOptions)
        };
        return true;
    }

    private static bool TryBuildDeterministicWeightToolCall(string userText, string normalizedText, out AiAgentToolCall toolCall)
    {
        toolCall = new AiAgentToolCall();
        if (!HasWriteIntent(userText) || !IsWeightWriteIntent(normalizedText))
            return false;

        if (!TryExtractExplicitTag(userText, out var tag))
            return false;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var root = new JsonObject
        {
            ["schema_version"] = "v1",
            ["idempotency_key"] = BuildIdempotencyKey("weight", today, tag, normalizedText),
            ["tag"] = tag
        };

        if (TryExtractWeightValue(userText, out var weight))
            root["weight"] = weight;
        if (normalizedText.Contains("сегодня", StringComparison.Ordinal))
            root["date"] = today.ToString("yyyy-MM-dd");
        if (normalizedText.Contains("ручн", StringComparison.Ordinal))
            root["method"] = "Ручное взвешивание";
        else if (normalizedText.Contains("автомат", StringComparison.Ordinal) || normalizedText.Contains("станц", StringComparison.Ordinal))
            root["method"] = "Автоматическая весовая станция";

        toolCall = new AiAgentToolCall
        {
            Name = AiAssistantToolNames.CreateWeight,
            Arguments = JsonSerializer.SerializeToElement(root, JsonOptions)
        };
        return true;
    }

    private static bool TryBuildDeterministicDailyActionToolCall(string userText, string normalizedText, out AiAgentToolCall toolCall)
    {
        toolCall = new AiAgentToolCall();
        if (!HasWriteIntent(userText) || !TryExtractDailyActionType(normalizedText, out var actionType, out var subtype))
            return false;

        if (!TryExtractExplicitTag(userText, out var tag))
            return false;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var date = normalizedText.Contains("сегодня", StringComparison.Ordinal)
            ? today
            : today;
        var batchKey = BuildIdempotencyKey("daily-action", date, tag, normalizedText);
        var item = new JsonObject
        {
            ["idempotency_key"] = $"{batchKey}:{NormalizeKeyPart(tag)}",
            ["tag"] = tag,
            ["type"] = actionType,
            ["date"] = date.ToString("yyyy-MM-dd")
        };

        if (!string.IsNullOrWhiteSpace(subtype))
            item["subtype"] = subtype;

        if (actionType == "Перевод" && TryExtractTransferGroupName(userText, out var groupName))
            item["new_group_name"] = groupName;

        var root = new JsonObject
        {
            ["schema_version"] = "v1",
            ["batch_idempotency_key"] = batchKey,
            ["items"] = new JsonArray(item)
        };

        toolCall = new AiAgentToolCall
        {
            Name = AiAssistantToolNames.CreateDailyAction,
            Arguments = JsonSerializer.SerializeToElement(root, JsonOptions)
        };
        return true;
    }

    private static bool TryExtractTransferGroupName(string text, out string groupName)
    {
        groupName = string.Empty;
        var match = Regex.Match(
            text,
            @"\bгрупп\w*\s*[-:–—]?\s+(?<group>[A-Za-zА-Яа-я0-9][A-Za-zА-Яа-я0-9\s._-]{0,80})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
            return false;

        var tokens = Regex.Split(match.Groups["group"].Value, @"\s+", RegexOptions.CultureInvariant)
            .Select(token => token.Trim().Trim('.', ',', ';', ':', '!', '?', '(', ')', '[', ']'))
            .Where(token => token.Length > 0)
            .TakeWhile(token => !IsGroupPhraseStopWord(token))
            .ToList();

        groupName = string.Join(" ", tokens);
        return groupName.Any(char.IsLetterOrDigit);
    }

    private static bool IsGroupPhraseStopWord(string token)
    {
        var normalized = token.ToLowerInvariant();
        return normalized is "сегодня" or "вчера" or "завтра";
    }

    private static bool IsWeightWriteIntent(string normalizedText)
        => normalizedText.Contains("вес", StringComparison.Ordinal) ||
           normalizedText.Contains("килограмм", StringComparison.Ordinal) ||
           Regex.IsMatch(normalizedText, @"\b\d+(?:[,.]\d+)?\s*кг\b", RegexOptions.CultureInvariant);

    private static bool TryExtractWeightValue(string text, out double weight)
    {
        weight = 0;
        var match = Regex.Match(
            text,
            @"(?:вес\s*)?(?<weight>\d+(?:[,.]\d+)?)\s*(?:кг|килограмм\w*)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success &&
               double.TryParse(
                   match.Groups["weight"].Value.Replace(',', '.'),
                   System.Globalization.NumberStyles.Float,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out weight);
    }

    private static bool TryExtractDailyActionType(string normalizedText, out string actionType, out string? subtype)
    {
        actionType = string.Empty;
        subtype = null;

        if (normalizedText.Contains("лечение", StringComparison.Ordinal) ||
            normalizedText.Contains("лечен", StringComparison.Ordinal))
        {
            actionType = "Лечение";
            subtype = "Лечение";
            return true;
        }

        if (normalizedText.Contains("вакцин", StringComparison.Ordinal))
        {
            actionType = "Вакцинации и обработки";
            subtype = "Вакцинация";
            return true;
        }

        if (normalizedText.Contains("дегельмин", StringComparison.Ordinal) ||
            normalizedText.Contains("дегильмин", StringComparison.Ordinal))
        {
            actionType = "Вакцинации и обработки";
            subtype = "Дегельминтизация";
            return true;
        }

        if (normalizedText.Contains("переведи", StringComparison.Ordinal) ||
            normalizedText.Contains("переводи", StringComparison.Ordinal) ||
            normalizedText.Contains("перевод", StringComparison.Ordinal))
        {
            actionType = "Перевод";
            subtype = "Перевод";
            return true;
        }

        if (normalizedText.Contains("другое", StringComparison.Ordinal))
        {
            actionType = "Обработка";
            subtype = "Другое";
            return true;
        }

        return false;
    }

    private static AiAgentToolCall NormalizeToolCallFromUserText(AiAgentToolCall toolCall, string userText)
    {
        var root = toolCall.Arguments.HasValue && toolCall.Arguments.Value.ValueKind == JsonValueKind.Object
            ? JsonNode.Parse(toolCall.Arguments.Value.GetRawText()) as JsonObject ?? new JsonObject()
            : new JsonObject();

        root["schema_version"] = "v1";

        if (TryExtractEntityTags(userText, out var refs))
        {
            ApplySourceTags(toolCall.Name, root, refs);
        }

        ApplyTechnicalDefaults(toolCall.Name, root, userText);

        return new AiAgentToolCall
        {
            Name = toolCall.Name,
            Arguments = JsonSerializer.SerializeToElement(root, JsonOptions)
        };
    }

    private static void ApplySourceTags(string toolName, JsonObject root, ExtractedEntityTags refs)
    {
        var primaryTags = refs.CowTags.Count > 0
            ? refs.CowTags
            : refs.GenericTags.Count > 0
                ? refs.GenericTags
                : refs.AllTags;

        switch (toolName)
        {
            case AiAssistantToolNames.FindAnimal:
            case AiAssistantToolNames.GetAnimalParents:
            case AiAssistantToolNames.GetAnimalCard:
            case AiAssistantToolNames.GetWeightHistory:
                if (primaryTags.Count > 0)
                    root["tag"] = primaryTags[0];
                break;

            case AiAssistantToolNames.CreateWeight:
                if (primaryTags.Count > 0)
                    root["tag"] = primaryTags[0];
                break;

            case AiAssistantToolNames.CreateDailyAction:
                ApplyDailyActionSourceTags(root, primaryTags);
                break;

            case AiAssistantToolNames.CreateInsemination:
                ApplyInseminationSourceTags(root, refs);
                break;
        }
    }

    private static void ApplyDailyActionSourceTags(JsonObject root, IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
            return;

        var items = EnsureItems(root);
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is not JsonObject item)
                continue;

            item["tag"] = tags[Math.Min(i, tags.Count - 1)];
        }
    }

    private static void ApplyInseminationSourceTags(JsonObject root, ExtractedEntityTags refs)
    {
        var cowTags = refs.CowTags.Count > 0 ? refs.CowTags : refs.GenericTags;
        var items = EnsureItems(root);
        foreach (var node in items)
        {
            if (node is not JsonObject item)
                continue;

            if (cowTags.Count > 0)
                item["cow_tags"] = ToJsonArray(cowTags);
            if (refs.BullTags.Count > 0)
                item["bull_tags"] = ToJsonArray(refs.BullTags);
        }
    }

    private static void ApplyTechnicalDefaults(string toolName, JsonObject root, string userText)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var hashSeed = userText.Trim().ToLowerInvariant();

        switch (toolName)
        {
            case AiAssistantToolNames.CreateWeight:
                root["idempotency_key"] ??= BuildIdempotencyKey("weight", today, root["tag"]?.GetValue<string>() ?? "tag", hashSeed);
                break;

            case AiAssistantToolNames.CreateDailyAction:
            {
                var batchKey = root["batch_idempotency_key"]?.GetValue<string>() ??
                               BuildIdempotencyKey("daily-action", today, "batch", hashSeed);
                root["batch_idempotency_key"] = batchKey;
                var items = EnsureItems(root);
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i] is JsonObject item)
                        item["idempotency_key"] ??= $"{batchKey}:{i}";
                }
                break;
            }

            case AiAssistantToolNames.CreateInsemination:
            {
                var batchKey = root["batch_idempotency_key"]?.GetValue<string>() ??
                               BuildIdempotencyKey("insemination", today, "batch", hashSeed);
                root["batch_idempotency_key"] = batchKey;
                var items = EnsureItems(root);
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i] is not JsonObject item)
                        continue;

                    item["idempotency_key"] ??= $"{batchKey}:{i}";
                    if (item["date"] == null && userText.Contains("сегодня", StringComparison.OrdinalIgnoreCase))
                        item["date"] = today.ToString("yyyy-MM-dd");
                    if (item["insemination_type"] == null)
                    {
                        var normalized = userText.ToLowerInvariant();
                        if (normalized.Contains("естествен", StringComparison.Ordinal))
                            item["insemination_type"] = "Естественное";
                        else if (normalized.Contains("искусствен", StringComparison.Ordinal))
                            item["insemination_type"] = "Искусственное";
                        else if (normalized.Contains("эмбри", StringComparison.Ordinal))
                            item["insemination_type"] = "Эмбрион";
                    }
                }
                break;
            }
        }
    }

    private static JsonArray EnsureItems(JsonObject root)
    {
        if (root["items"] is JsonArray existing && existing.Count > 0)
            return existing;

        var created = new JsonArray(new JsonObject());
        root["items"] = created;
        return created;
    }

    private static bool TryBuildDeterministicReadToolCall(string userText, out AiAgentToolCall toolCall)
    {
        toolCall = new AiAgentToolCall();
        if (string.IsNullOrWhiteSpace(userText) || HasWriteIntent(userText))
            return false;

        var normalizedText = userText.Trim().ToLowerInvariant();
        var toolName = normalizedText switch
        {
            var text when IsPregnancyCheckIntent(text) => "get_pregnancies_to_check",
            var text when text.Contains("родител", StringComparison.Ordinal) => "get_animal_parents",
            var text when IsWeightHistoryIntent(text) => "get_weight_history",
            var text when IsAnimalCardIntent(text) => "get_animal_card",
            _ => null
        };

        if (toolName is null)
            return false;

        if (toolName == "get_pregnancies_to_check")
        {
            toolCall = new AiAgentToolCall
            {
                Name = toolName,
                Arguments = JsonSerializer.SerializeToElement(new Dictionary<string, object?>
                {
                    ["schema_version"] = "v1"
                }, JsonOptions)
            };
            return true;
        }

        if (!TryExtractExplicitTag(userText, out var tag))
            return false;

        var arguments = toolName == "get_weight_history"
            ? JsonSerializer.SerializeToElement(new Dictionary<string, object?>
            {
                ["schema_version"] = "v1",
                ["tag"] = tag,
                ["limit"] = 20
            }, JsonOptions)
            : JsonSerializer.SerializeToElement(new Dictionary<string, object?>
            {
                ["schema_version"] = "v1",
                ["tag"] = tag
            }, JsonOptions);

        toolCall = new AiAgentToolCall
        {
            Name = toolName,
            Arguments = arguments
        };
        return true;
    }

    private static bool HasWriteIntent(string text)
        => Regex.IsMatch(
            text,
            @"\b(внеси|внести|добавь|добавить|запиши|записать|сохрани|сохранить|осемени|осеменение|взвесь|взвесить|лечение|переведи|переводи|перевод)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static bool IsWeightHistoryIntent(string normalizedText)
        => (normalizedText.Contains("истори", StringComparison.Ordinal) ||
            normalizedText.Contains("какие", StringComparison.Ordinal) ||
            normalizedText.Contains("покажи", StringComparison.Ordinal)) &&
           (normalizedText.Contains("вес", StringComparison.Ordinal) ||
            normalizedText.Contains("взвеш", StringComparison.Ordinal));

    private static bool IsPregnancyCheckIntent(string normalizedText)
        => (normalizedText.Contains("стельност", StringComparison.Ordinal) ||
            normalizedText.Contains("стильност", StringComparison.Ordinal) ||
            normalizedText.Contains("настельност", StringComparison.Ordinal)) &&
           (normalizedText.Contains("проверк", StringComparison.Ordinal) ||
            normalizedText.Contains("проверить", StringComparison.Ordinal) ||
            normalizedText.Contains("кого", StringComparison.Ordinal) ||
            normalizedText.Contains("коров", StringComparison.Ordinal));

    private static bool IsAnimalCardIntent(string normalizedText)
        => normalizedText.Contains("карточ", StringComparison.Ordinal) ||
           normalizedText.Contains("открой", StringComparison.Ordinal);

    private static bool TryExtractExplicitTag(string text, out string tag)
    {
        tag = string.Empty;

        if (TryExtractTagPhrase(text, out var phraseTag) &&
            TryNormalizeTag(phraseTag, out tag))
        {
            return true;
        }

        if (TryExtractEntityTags(text, out var refs) &&
            refs.AllTags.Count > 0 &&
            TryNormalizeTag(refs.AllTags[0], out tag))
        {
            return true;
        }

        var numericCandidates = Regex.Matches(
                text,
                @"(?<!\d)(?:\d[\s.\-]*){1,16}(?!\d)",
                RegexOptions.CultureInvariant)
            .Select(match => Regex.Replace(match.Value, @"[\s.\-]+", string.Empty))
            .Where(value => value.Length > 0)
            .OrderByDescending(value => value.Length)
            .ToArray();

        foreach (var candidate in numericCandidates)
        {
            if (TryNormalizeTag(candidate, out tag))
                return true;
        }

        return false;
    }

    private static bool TryExtractTagPhrase(string text, out string tag)
    {
        tag = string.Empty;
        var match = Regex.Match(
            text,
            @"(?:коров\w*|телк\w*|т[её]лк\w*|нетел\w*|животн\w*|бирк\w*|пирк\w*|номер\w*)\s*[-:–—]?\s+(?<tag>[A-Za-zА-Яа-я0-9][A-Za-zА-Яа-я0-9\s._-]{0,60})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
            return false;

        var raw = match.Groups["tag"].Value;
        var parenthesisIndex = raw.IndexOf('(');
        if (parenthesisIndex >= 0)
            raw = raw[..parenthesisIndex];

        var tokens = Regex.Split(raw, @"\s+", RegexOptions.CultureInvariant)
            .Select(token => token.Trim().Trim('.', ',', ';', ':', '!', '?', '(', ')', '[', ']'))
            .Where(token => token.Length > 0)
            .TakeWhile(token => !IsTagPhraseStopWord(token))
            .ToList();

        if (tokens.Count == 0)
            return false;

        tag = string.Join(" ", tokens);
        return tag.Any(char.IsLetterOrDigit);
    }

    private static bool IsTagPhraseStopWord(string token)
    {
        var normalized = token.ToLowerInvariant();
        return normalized is "за" or "с" or "со" or "по" or "от" or "до" or "на" or "в" or
            "группа" or "группу" or "группе" or "группы" or
            "сегодня" or "вчера" or "завтра" or "история" or "историю" or
            "вес" or "веса" or "взвешивания";
    }

    private static bool TryNormalizeTag(string raw, out string tag)
    {
        tag = raw.Trim().Trim('.', ',', ';', ':', '!', '?');
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        if (Regex.IsMatch(tag, @"^[\d\s.\-]+$", RegexOptions.CultureInvariant))
            tag = Regex.Replace(tag, @"[\s.\-]+", string.Empty);

        return tag.Length > 0;
    }

    private static bool TryExtractEntityTags(string text, out ExtractedEntityTags tags)
    {
        var cowTags = ExtractTags(
            @"(?:коров\w*|телк\w*|т[её]лк\w*|нетел\w*|животн\w*)\s*[-:–—]?\s+(?<tag>[A-Za-zА-Яа-я0-9][A-Za-zА-Яа-я0-9._-]{0,24})",
            text);
        var bullTags = ExtractTags(
            @"(?:бык\w*|бычк\w*)\s*[-:–—]?\s+(?<tag>[A-Za-zА-Яа-я0-9][A-Za-zА-Яа-я0-9._-]{0,24})",
            text);
        var genericTags = ExtractTags(
            @"(?:бирк\w*|пирк\w*|номер\w*)\s*[-:–—]?\s+(?<tag>[A-Za-zА-Яа-я0-9][A-Za-zА-Яа-я0-9._-]{0,24})",
            text);

        var all = cowTags
            .Concat(bullTags)
            .Concat(genericTags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (all.Count == 0)
        {
            all = Regex.Matches(text, @"(?<!\d)(?:\d[\s.\-]*){1,16}(?!\d)", RegexOptions.CultureInvariant)
                .Select(match => Regex.Replace(match.Value, @"[\s.\-]+", string.Empty))
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        tags = new ExtractedEntityTags(cowTags, bullTags, genericTags, all);
        return tags.AllTags.Count > 0;
    }

    private static List<string> ExtractTags(string pattern, string text)
        => Regex.Matches(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            .Select(match => match.Groups["tag"].Value)
            .Where(raw => TryNormalizeTag(raw, out _))
            .Select(raw =>
            {
                TryNormalizeTag(raw, out var tag);
                return tag;
            })
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
            array.Add(value);
        return array;
    }

    private static string BuildIdempotencyKey(string prefix, DateOnly date, string tag, string seed)
        => $"{prefix}:{date:yyyy-MM-dd}:{NormalizeKeyPart(tag)}:{ComputeShortHash(seed)}";

    private static string NormalizeKeyPart(string value)
        => Regex.Replace(value, @"[^A-Za-z0-9._:-]+", "_", RegexOptions.CultureInvariant).Trim('_') switch
        {
            { Length: > 0 } normalized => normalized,
            _ => "tag"
        };

    private static string ComputeShortHash(string value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash)[..12].ToLowerInvariant();
    }

    private sealed record ExtractedEntityTags(
        IReadOnlyList<string> CowTags,
        IReadOnlyList<string> BullTags,
        IReadOnlyList<string> GenericTags,
        IReadOnlyList<string> AllTags);

    private static bool TryRecoverToolCallFromFinalAnswer(string? text, out AiAgentToolCall toolCall)
    {
        toolCall = new AiAgentToolCall();
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var trimmed = text.Trim();
        var jsonStart = trimmed.IndexOf('{');
        var jsonEnd = trimmed.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd <= jsonStart)
            return false;

        var json = trimmed[jsonStart..(jsonEnd + 1)];
        try
        {
            using var document = JsonDocument.Parse(json);
            return TryRecoverToolCall(document.RootElement, out toolCall);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryRecoverToolCall(JsonElement element, out AiAgentToolCall toolCall)
    {
        toolCall = new AiAgentToolCall();
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        if (element.TryGetProperty("tool_call", out var toolCallElement) &&
            TryBuildRecoveredToolCall(toolCallElement, out toolCall))
        {
            return true;
        }

        if (element.TryGetProperty("function", out var functionElement) &&
            TryBuildRecoveredToolCall(functionElement, out toolCall))
        {
            return true;
        }

        if (element.TryGetProperty("final_answer", out var finalAnswerElement) &&
            finalAnswerElement.ValueKind == JsonValueKind.String &&
            TryRecoverToolCallFromFinalAnswer(finalAnswerElement.GetString(), out toolCall))
        {
            return true;
        }

        return TryBuildRecoveredToolCall(element, out toolCall);
    }

    private static bool TryBuildRecoveredToolCall(JsonElement element, out AiAgentToolCall toolCall)
    {
        toolCall = new AiAgentToolCall();
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty("name", out var nameElement) ||
            nameElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var name = nameElement.GetString();
        if (string.IsNullOrWhiteSpace(name))
            return false;

        JsonElement? arguments = null;
        if (TryGetRecoveredArguments(element, out var argumentElement))
            arguments = argumentElement;

        toolCall = new AiAgentToolCall
        {
            Name = name.Trim(),
            Arguments = arguments
        };
        return true;
    }

    private static bool TryGetRecoveredArguments(JsonElement element, out JsonElement arguments)
    {
        arguments = default;
        if (!element.TryGetProperty("arguments", out var candidate) &&
            !element.TryGetProperty("parameters", out candidate))
        {
            return false;
        }

        if (candidate.ValueKind == JsonValueKind.String)
        {
            var raw = candidate.GetString();
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            try
            {
                using var document = JsonDocument.Parse(raw);
                arguments = JsonSerializer.SerializeToElement(document.RootElement, JsonOptions);
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        arguments = JsonSerializer.SerializeToElement(candidate, JsonOptions);
        return true;
    }


    private static string? TrimForAudit(string? value)
        => string.IsNullOrWhiteSpace(value) || value.Length <= 500 ? value : value[..500];
}

public sealed class DefaultAiConstrainedOutputValidator : IAiConstrainedOutputValidator
{
    public AiConstrainedOutputValidationResult Validate(AiAgentLlmOutput output)
    {
        if (output == null)
            return AiConstrainedOutputValidationResult.Invalid("LLM вернул пустой ответ.", "$");

        return output.Type switch
        {
            AiAgentOutputType.FinalAnswer when string.IsNullOrWhiteSpace(output.FinalAnswer)
                => AiConstrainedOutputValidationResult.Invalid("final_answer должен содержать текст.", "$.finalAnswer"),

            AiAgentOutputType.ToolCall when output.ToolCall == null
                => AiConstrainedOutputValidationResult.Invalid("tool_call должен содержать объект toolCall.", "$.toolCall"),

            AiAgentOutputType.ToolCall when string.IsNullOrWhiteSpace(output.ToolCall.Name)
                => AiConstrainedOutputValidationResult.Invalid("tool_call должен содержать имя инструмента.", "$.toolCall.name"),

            AiAgentOutputType.FinalAnswer or AiAgentOutputType.ToolCall
                => AiConstrainedOutputValidationResult.Valid(),

            _
                => AiConstrainedOutputValidationResult.Invalid("LLM вернул неизвестный тип ответа.", "$.type")
        };
    }
}

public sealed class DisabledAiAgentLlmClient : IAiAgentLlmClient
{
    public Task<AiAgentLlmOutput> GetNextAsync(
        AiAgentSession session,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AiAgentLlmOutput.Final("AI помощник временно недоступен: не настроено подключение к LLM сервису."));
    }
}

public sealed class DisabledAiToolExecutor : IAiToolExecutor
{
    public Task<AiAgentToolResult> ExecuteAsync(
        Guid organizationId,
        AiAgentToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AiAgentToolResult.Fail(
            toolCall.Name,
            AiAgentError.Create(
                "AI_TOOL_EXECUTOR_DISABLED",
                "AI tool executor ещё не подключён.",
                retryable: false)));
    }
}
