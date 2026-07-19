using System.Text.Json;
using CAT.Controllers.DTO.AiAssistant;
using CAT.Services.Ai;
using Xunit;

namespace CAT.Tests.Ai;

public sealed class AiAgentLoopTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task RunAsync_FinalAnswer_ReturnsFinalAnswerAndHistory()
    {
        var loop = CreateLoop(new QueueLlmClient(AiAgentLlmOutput.Final("Готово.")));

        var result = await loop.RunAsync(OrgId, "Покажи корову 523");

        Assert.Equal(AiAgentLoopStatus.FinalAnswer, result.Status);
        Assert.Equal("Готово.", result.FinalAnswer);
        Assert.Equal(2, result.History.Count);
        Assert.Equal(AiAgentMessageRole.User, result.History[0].Role);
        Assert.Equal(AiAgentMessageRole.Assistant, result.History[1].Role);
    }

    [Fact]
    public async Task RunAsync_ReachesMaxIterations_ReturnsIterationLimit()
    {
        var loop = CreateLoop(
            new RepeatToolCallLlmClient("find_animal"),
            new SequenceToolExecutor(_ => AiAgentToolResult.Ok("find_animal", "ok")));

        var result = await loop.RunAsync(OrgId, "Найди животное");

        Assert.Equal(AiAgentLoopStatus.IterationLimit, result.Status);
        Assert.Equal("AI_AGENT_ITERATION_LIMIT", result.Error?.Code);
    }

    [Fact]
    public async Task RunAsync_DuplicateToolCall_ReturnsDuplicateCall()
    {
        var args = JsonDocument.Parse("""{"schema_version":"v1","tag":"523"}""").RootElement.Clone();
        var loop = CreateLoop(
            new QueueLlmClient(
                AiAgentLlmOutput.Call("find_animal", args),
                AiAgentLlmOutput.Call("find_animal", args)),
            new SequenceToolExecutor(_ => AiAgentToolResult.Ok("find_animal", "ok")));

        var result = await loop.RunAsync(OrgId, "Найди 523");

        Assert.Equal(AiAgentLoopStatus.DuplicateToolCall, result.Status);
        Assert.Equal("AI_AGENT_DUPLICATE_TOOL_CALL", result.Error?.Code);
    }

    [Fact]
    public async Task RunAsync_FailedTool_ReturnsUnifiedToolError()
    {
        var loop = CreateLoop(
            new QueueLlmClient(AiAgentLlmOutput.Call("find_animal", JsonSerializer.SerializeToElement(new { schema_version = "v1", tag = "523" }))),
            new SequenceToolExecutor(call => AiAgentToolResult.Fail(
                call.Name,
                AiAgentError.Create("AI_TOOL_NOT_FOUND", "Инструмент не найден."))));

        var result = await loop.RunAsync(OrgId, "Найди 523");

        Assert.Equal(AiAgentLoopStatus.FailedTool, result.Status);
        Assert.NotNull(result.ToolResult);
        Assert.False(result.ToolResult.Success);
        Assert.Equal("AI_TOOL_NOT_FOUND", result.Error?.Code);
    }

    [Fact]
    public async Task RunAsync_TerminalReadTool_ReturnsToolResultWithoutExtraLlmTurn()
    {
        var llm = new QueueLlmClient(AiAgentLlmOutput.Call(
            "find_animal",
            JsonSerializer.SerializeToElement(new { schema_version = "v1", tag = "523" })));
        var audit = new RecordingAiAuditService();
        var loop = CreateLoop(
            llm,
            new SequenceToolExecutor(call => AiAgentToolResult.Ok(call.Name, "Нашла животное.", isTerminal: true)),
            audit);

        var result = await loop.RunAsync(OrgId, "Найди 523");

        Assert.Equal(AiAgentLoopStatus.ToolResult, result.Status);
        Assert.Equal("Нашла животное.", result.ToolResult?.Summary);
        Assert.Equal(3, result.History.Count);
        Assert.Contains(audit.Events, e => e.ActionType == AiAuditActionTypes.LlmTurn);
        Assert.Contains(audit.Events, e => e.ActionType == AiAuditActionTypes.ToolCall && e.Status == "success");
    }

    [Fact]
    public async Task RunAsync_ExplicitWeightHistoryRead_UsesDeterministicReadGuard()
    {
        var audit = new RecordingAiAuditService();
        var executed = new List<AiAgentToolCall>();
        var loop = CreateLoop(
            new ThrowingLlmClient(),
            new SequenceToolExecutor(call =>
            {
                executed.Add(call);
                return AiAgentToolResult.Ok(call.Name, "Найдена одна запись веса.", isTerminal: true);
            }),
            audit);

        var result = await loop.RunAsync(OrgId, "Покажи историю взвешивания животного 10007");

        Assert.Equal(AiAgentLoopStatus.ToolResult, result.Status);
        var call = Assert.Single(executed);
        Assert.Equal("get_weight_history", call.Name);
        Assert.Equal("10007", call.Arguments!.Value.GetProperty("tag").GetString());
        Assert.DoesNotContain(audit.Events, e => e.ActionType == AiAuditActionTypes.LlmTurn);
        Assert.Contains(audit.Events, e => e.ActionType == AiAuditActionTypes.ToolCall && e.Status == "success");
    }

    [Fact]
    public async Task RunAsync_WeightQuestion_UsesWeightHistoryReadGuard()
    {
        var executed = new List<AiAgentToolCall>();
        var loop = CreateLoop(
            new ThrowingLlmClient(),
            new SequenceToolExecutor(call =>
            {
                executed.Add(call);
                return AiAgentToolResult.Ok(call.Name, "История веса.", isTerminal: true);
            }));

        var result = await loop.RunAsync(OrgId, "Какие веса были у коровы 25?");

        Assert.Equal(AiAgentLoopStatus.ToolResult, result.Status);
        var call = Assert.Single(executed);
        Assert.Equal(AiAssistantToolNames.GetWeightHistory, call.Name);
        Assert.Equal("25", call.Arguments!.Value.GetProperty("tag").GetString());
    }

    [Fact]
    public async Task RunAsync_PregnancyCheckQuestion_UsesPregnancyReadGuard()
    {
        var executed = new List<AiAgentToolCall>();
        var loop = CreateLoop(
            new ThrowingLlmClient(),
            new SequenceToolExecutor(call =>
            {
                executed.Add(call);
                return AiAgentToolResult.Ok(call.Name, "Для диагностики стельности найдено: 0.", isTerminal: true);
            }));

        var result = await loop.RunAsync(OrgId, "Кого нужно проверить настельность?");

        Assert.Equal(AiAgentLoopStatus.ToolResult, result.Status);
        var call = Assert.Single(executed);
        Assert.Equal(AiAssistantToolNames.GetPregnanciesToCheck, call.Name);
    }

    [Fact]
    public async Task RunAsync_WeightWriteWithAsrTypo_UsesCreateWeightGuard()
    {
        var executed = new List<AiAgentToolCall>();
        var loop = CreateLoop(
            new ThrowingLlmClient(),
            new SequenceToolExecutor(call =>
            {
                executed.Add(call);
                return AiAgentToolResult.Ok(call.Name, "Пока ничего не сохранено.", isTerminal: true);
            }));

        var result = await loop.RunAsync(OrgId, "внеси вес 421 килограмм пирка 25 сегодня ручное взвешивание");

        Assert.Equal(AiAgentLoopStatus.ToolResult, result.Status);
        var call = Assert.Single(executed);
        Assert.Equal(AiAssistantToolNames.CreateWeight, call.Name);
        var args = call.Arguments!.Value;
        Assert.Equal("25", args.GetProperty("tag").GetString());
        Assert.Equal(421, args.GetProperty("weight").GetDouble());
        Assert.Equal("Ручное взвешивание", args.GetProperty("method").GetString());
    }

    [Fact]
    public async Task RunAsync_TransferWrite_ExtractsOnlyAnimalTagBeforeGroupPhrase()
    {
        var executed = new List<AiAgentToolCall>();
        var loop = CreateLoop(
            new ThrowingLlmClient(),
            new SequenceToolExecutor(call =>
            {
                executed.Add(call);
                return AiAgentToolResult.Ok(call.Name, "Пока ничего не сохранено.", isTerminal: true);
            }));

        var result = await loop.RunAsync(OrgId, "Переводи корову 25 сегодня в новую группу, группа 345.");

        Assert.Equal(AiAgentLoopStatus.ToolResult, result.Status);
        var call = Assert.Single(executed);
        Assert.Equal(AiAssistantToolNames.CreateDailyAction, call.Name);
        var item = call.Arguments!.Value.GetProperty("items")[0];
        Assert.Equal("25", item.GetProperty("tag").GetString());
        Assert.Equal("Перевод", item.GetProperty("type").GetString());
        Assert.Equal("345", item.GetProperty("new_group_name").GetString());
    }

    [Fact]
    public async Task RunAsync_InvalidConstrainedOutput_ReturnsInvalidOutput()
    {
        var audit = new RecordingAiAuditService();
        var loop = CreateLoop(new QueueLlmClient(new AiAgentLlmOutput { Type = AiAgentOutputType.ToolCall }), auditService: audit);

        var result = await loop.RunAsync(OrgId, "Найди 523");

        Assert.Equal(AiAgentLoopStatus.InvalidOutput, result.Status);
        Assert.Equal("AI_AGENT_CONSTRAINED_OUTPUT_INVALID", result.Error?.Code);
        Assert.Contains(audit.Events, e => e.ActionType == AiAuditActionTypes.LoopGuard && e.Status == "error");
    }

    [Fact]
    public async Task RunAsync_InvalidToolSchema_RetriesOnceAndExecutesCorrectedToolCall()
    {
        var llm = new QueueLlmClient(
            AiAgentLlmOutput.Call("find_animal", JsonSerializer.SerializeToElement(new { schema_version = "v1" })),
            AiAgentLlmOutput.Call("find_animal", JsonSerializer.SerializeToElement(new { schema_version = "v1", tag = "523" })));
        var executed = new List<AiAgentToolCall>();
        var loop = CreateLoop(
            llm,
            new SequenceToolExecutor(call =>
            {
                executed.Add(call);
                return AiAgentToolResult.Ok(call.Name, "Нашла животное.", isTerminal: true);
            }));

        var result = await loop.RunAsync(OrgId, "Найди 523");

        Assert.Equal(AiAgentLoopStatus.ToolResult, result.Status);
        Assert.Single(executed);
        Assert.Equal("523", executed[0].Arguments!.Value.GetProperty("tag").GetString());
    }

    [Fact]
    public async Task RunAsync_InvalidToolSchemaAfterRetry_ReturnsInvalidOutputWithoutExecutingTool()
    {
        var loop = CreateLoop(new QueueLlmClient(
            AiAgentLlmOutput.Call("create_weight", JsonSerializer.SerializeToElement(new { schema_version = "v1" })),
            AiAgentLlmOutput.Call("create_weight", JsonSerializer.SerializeToElement(new { schema_version = "v1" }))));

        var result = await loop.RunAsync(OrgId, "Запиши вес");

        Assert.Equal(AiAgentLoopStatus.InvalidOutput, result.Status);
        Assert.Equal("AI_TOOL_SCHEMA_INVALID", result.Error?.Code);
        Assert.Equal("$.tag", result.Error?.Path);
    }

    [Fact]
    public async Task RunAsync_NaturalInseminationWithCowTag_UsesDeterministicWriteGuard()
    {
        var audit = new RecordingAiAuditService();
        var executed = new List<AiAgentToolCall>();
        var loop = CreateLoop(
            new ThrowingLlmClient(),
            new SequenceToolExecutor(call =>
            {
                executed.Add(call);
                return AiAgentToolResult.Ok(call.Name, "Пока ничего не сохранено.", isTerminal: true);
            }),
            audit);

        var result = await loop.RunAsync(OrgId, "Внеси естественное осеменение корове 25.");

        Assert.Equal(AiAgentLoopStatus.ToolResult, result.Status);
        var call = Assert.Single(executed);
        Assert.Equal(AiAssistantToolNames.CreateInsemination, call.Name);
        var root = call.Arguments!.Value;
        Assert.Equal("v1", root.GetProperty("schema_version").GetString());
        var item = root.GetProperty("items")[0];
        Assert.Equal("25", item.GetProperty("cow_tags")[0].GetString());
        Assert.Equal("Естественное", item.GetProperty("insemination_type").GetString());
        Assert.DoesNotContain(audit.Events, e => e.ActionType == AiAuditActionTypes.LlmTurn);
    }

    [Fact]
    public async Task RunAsync_LlmWrongWriteTag_ReplacesItWithSourceTextTagAndAddsSchemaVersion()
    {
        var executed = new List<AiAgentToolCall>();
        var llmArguments = JsonSerializer.SerializeToElement(new
        {
            batch_idempotency_key = "daily:bad:tag",
            items = new[]
            {
                new
                {
                    idempotency_key = "daily:bad:tag:1",
                    tag = "КРС 1",
                    type = "Лечение"
                }
            }
        });
        var loop = CreateLoop(
            new QueueLlmClient(AiAgentLlmOutput.Call(AiAssistantToolNames.CreateDailyAction, llmArguments)),
            new SequenceToolExecutor(call =>
            {
                executed.Add(call);
                return AiAgentToolResult.Ok(call.Name, "Пока ничего не сохранено.", isTerminal: true);
            }));

        var result = await loop.RunAsync(OrgId, "Запиши лечение корове 25");

        Assert.Equal(AiAgentLoopStatus.ToolResult, result.Status);
        var call = Assert.Single(executed);
        var root = call.Arguments!.Value;
        Assert.Equal("v1", root.GetProperty("schema_version").GetString());
        Assert.Equal("25", root.GetProperty("items")[0].GetProperty("tag").GetString());
    }

    private static AiAgentLoop CreateLoop(
        IAiAgentLlmClient llmClient,
        IAiToolExecutor? toolExecutor = null,
        IAiAuditService? auditService = null)
        => new(
            llmClient,
            toolExecutor ?? new SequenceToolExecutor(call => AiAgentToolResult.Ok(call.Name, "ok")),
            new DefaultAiConstrainedOutputValidator(),
            new AiToolSchemaValidator(),
            auditService ?? new DisabledAiAuditService());

    private sealed class QueueLlmClient : IAiAgentLlmClient
    {
        private readonly Queue<AiAgentLlmOutput> _outputs;

        public QueueLlmClient(params AiAgentLlmOutput[] outputs)
        {
            _outputs = new Queue<AiAgentLlmOutput>(outputs);
        }

        public Task<AiAgentLlmOutput> GetNextAsync(
            AiAgentSession session,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_outputs.Count > 0
                ? _outputs.Dequeue()
                : AiAgentLlmOutput.Final("Готово."));
        }
    }

    private sealed class RepeatToolCallLlmClient : IAiAgentLlmClient
    {
        private int _index;
        private readonly string _toolName;

        public RepeatToolCallLlmClient(string toolName)
        {
            _toolName = toolName;
        }

        public Task<AiAgentLlmOutput> GetNextAsync(
            AiAgentSession session,
            CancellationToken cancellationToken = default)
        {
            _index++;
            var args = JsonSerializer.SerializeToElement(new { schema_version = "v1", tag = _index.ToString() });
            return Task.FromResult(AiAgentLlmOutput.Call(_toolName, args));
        }
    }

    private sealed class ThrowingLlmClient : IAiAgentLlmClient
    {
        public Task<AiAgentLlmOutput> GetNextAsync(
            AiAgentSession session,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("LLM should not be called for deterministic read guard.");
        }
    }

    private sealed class SequenceToolExecutor : IAiToolExecutor
    {
        private readonly Func<AiAgentToolCall, AiAgentToolResult> _handler;

        public SequenceToolExecutor(Func<AiAgentToolCall, AiAgentToolResult> handler)
        {
            _handler = handler;
        }

        public Task<AiAgentToolResult> ExecuteAsync(
            Guid organizationId,
            AiAgentToolCall toolCall,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_handler(toolCall));
        }
    }

    private sealed class RecordingAiAuditService : IAiAuditService
    {
        public List<(string ActionType, string Status, AiAgentError? Error)> Events { get; } = new();

        public void Log(Guid organizationId, string actionType, string status, object details, AiAgentError? error = null)
            => Events.Add((actionType, status, error));
    }
}
