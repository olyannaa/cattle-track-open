using CAT.Controllers.DTO.AiAssistant;
using CAT.Services.Ai;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Xunit;

namespace CAT.Tests.Ai;

public sealed class AiAssistantServiceTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task CreateTextDraft_StoresUnsupportedDraftWithoutCommit()
    {
        var cache = new FakeDistributedCache();
        var audit = new RecordingAiAuditService();
        var service = CreateService(cache, auditService: audit);

        var response = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Покажи корову 523",
            ClientRequestId = "client-1"
        });

        Assert.NotEqual(Guid.Empty, response.DraftId);
        Assert.Equal(AiAssistantDraftStatus.FinalAnswer, response.Status);
        Assert.NotNull(response.Preview);
        Assert.Equal(AiAssistantToolNames.Unsupported, response.Preview.ToolName);
        Assert.True(cache.Contains($"ai-assistant:draft:{response.DraftId:N}"));
        Assert.Contains(audit.Events, e => e.ActionType == AiAuditActionTypes.DraftCreated);
    }

    [Fact]
    public async Task ConfirmDraft_WhenConfirmFalse_CancelsAndRemovesDraft()
    {
        var cache = new FakeDistributedCache();
        var audit = new RecordingAiAuditService();
        var service = CreateService(cache, auditService: audit);
        var draft = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO { Text = "Внеси вес 420 для 523" });

        var response = await service.ConfirmDraftAsync(OrgId, draft.DraftId, new AiAssistantConfirmRequestDTO
        {
            Confirm = false
        });

        Assert.Equal(AiAssistantDraftStatus.Canceled, response.Status);
        Assert.False(cache.Contains($"ai-assistant:draft:{draft.DraftId:N}"));
        Assert.Contains(audit.Events, e => e.ActionType == AiAuditActionTypes.Commit && e.Status == "canceled");
    }

    [Fact]
    public async Task ConfirmDraft_WhenDraftMissing_ReturnsConfirmExpired()
    {
        var audit = new RecordingAiAuditService();
        var service = CreateService(new FakeDistributedCache(), auditService: audit);

        var response = await service.ConfirmDraftAsync(OrgId, Guid.NewGuid(), new AiAssistantConfirmRequestDTO());

        Assert.Equal(AiAssistantDraftStatus.ConfirmExpired, response.Status);
        Assert.Contains(audit.Events, e => e.ActionType == AiAuditActionTypes.Commit && e.Status == "error");
    }

    [Fact]
    public async Task CreateTextDraft_WritePreviewWithoutReadyItems_IsPreviewButCannotCommit()
    {
        var payload = new AiWriteDraftPayload
        {
            ToolName = AiAssistantToolNames.CreateWeight,
            Items =
            {
                new AiWriteDraftItem
                {
                    Index = 0,
                    IdempotencyKey = "weight-1",
                    Tag = "523",
                    Status = AiWriteItemStatus.Ambiguous,
                    Message = "Нужно уточнение.",
                    CanCommit = false
                }
            }
        };
        var loopResult = new AiAgentLoopResult
        {
            Status = AiAgentLoopStatus.ToolResult,
            ToolCall = new AiAgentToolCall
            {
                Name = AiAssistantToolNames.CreateWeight,
                Arguments = JsonSerializer.SerializeToElement(new { tag = "523" }, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            },
            ToolResult = AiAgentToolResult.Ok(
                AiAssistantToolNames.CreateWeight,
                "Нужно уточнение.",
                JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                isTerminal: true,
                canCommit: false)
        };
        var cache = new FakeDistributedCache();
        var audit = new RecordingAiAuditService();
        var service = CreateService(cache, loopResult, audit);

        var draft = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO { Text = "Внеси вес 420 для 523" });
        var confirm = await service.ConfirmDraftAsync(OrgId, draft.DraftId, new AiAssistantConfirmRequestDTO());

        Assert.Equal(AiAssistantDraftStatus.Preview, draft.Status);
        Assert.NotNull(draft.Preview);
        Assert.Equal(0, draft.Preview.Arguments?.GetProperty("commitReady").GetInt32());
        Assert.Equal(AiAssistantDraftStatus.CannotCommit, confirm.Status);
        Assert.True(cache.Contains($"ai-assistant:draft:{draft.DraftId:N}"));
        Assert.Contains(audit.Events, e => e.ActionType == AiAuditActionTypes.Clarification);
        Assert.Contains(audit.Events, e => e.ActionType == AiAuditActionTypes.Commit && e.Status == "error");
    }

    [Fact]
    public async Task CreateTextDraft_ReusesPriorConversationMessages()
    {
        var cache = new FakeDistributedCache();
        var loop = new FakeAiAgentLoop();
        var service = CreateService(cache, agentLoop: loop);
        var conversationId = Guid.NewGuid().ToString();

        var first = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Покажи карточку коровы 25",
            ConversationId = conversationId
        });
        var second = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Сегодня было естественное осеменение",
            ConversationId = conversationId
        });

        Assert.Equal(first.ConversationId, second.ConversationId);
        Assert.Contains(loop.LastPriorHistory, message =>
            message.Role == AiAgentMessageRole.User && message.Content == "Покажи карточку коровы 25");
    }

    [Fact]
    public async Task CreateTextDraft_MergesBullClarificationIntoActiveInseminationDraft()
    {
        var sourceArguments = JsonSerializer.SerializeToElement(new
        {
            schema_version = "v1",
            batch_idempotency_key = "ins-1",
            items = new[]
            {
                new
                {
                    idempotency_key = "ins-1-0",
                    cow_tags = new[] { "25" },
                    date = "2026-07-14",
                    insemination_type = "Естественное",
                    bull_tags = Array.Empty<string>()
                }
            }
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var payload = new AiWriteDraftPayload
        {
            ToolName = AiAssistantToolNames.CreateInsemination,
            BatchIdempotencyKey = "ins-1",
            SourceArguments = sourceArguments,
            Items =
            {
                new AiWriteDraftItem
                {
                    Index = 0,
                    IdempotencyKey = "ins-1-0:25",
                    Tag = "25",
                    Status = AiWriteItemStatus.Invalid,
                    Message = "Для естественного осеменения нужен бык.",
                    CanCommit = false
                }
            }
        };
        var loopResult = new AiAgentLoopResult
        {
            Status = AiAgentLoopStatus.ToolResult,
            ToolCall = new AiAgentToolCall
            {
                Name = AiAssistantToolNames.CreateInsemination,
                Arguments = sourceArguments
            },
            ToolResult = AiAgentToolResult.Ok(
                AiAssistantToolNames.CreateInsemination,
                "Пока ничего не сохранено. Для естественного осеменения нужен бык.",
                JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                isTerminal: true,
                canCommit: false)
        };
        var cache = new FakeDistributedCache();
        var loop = new FakeAiAgentLoop(loopResult);
        var writeTool = new FakeAiWriteToolService();
        var service = CreateService(cache, agentLoop: loop, writeToolService: writeTool);
        var conversationId = Guid.NewGuid().ToString();

        var first = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Внеси осеменение корове 25",
            ConversationId = conversationId
        });
        var second = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Бык 10015",
            ConversationId = first.ConversationId
        });

        Assert.Equal(first.DraftId, second.DraftId);
        Assert.Equal(1, loop.CallCount);
        Assert.NotNull(writeTool.LastToolCall?.Arguments);
        var item = writeTool.LastToolCall!.Arguments!.Value.GetProperty("items")[0];
        Assert.Equal("25", item.GetProperty("cow_tags")[0].GetString());
        Assert.Equal("10015", item.GetProperty("bull_tags")[0].GetString());
        Assert.Equal(AiAssistantDraftStatus.Preview, second.Status);
    }

    [Fact]
    public async Task CreateTextDraft_NormalizesCommonAsrCowMistakeBeforeLlm()
    {
        var cache = new FakeDistributedCache();
        var loop = new FakeAiAgentLoop();
        var service = CreateService(cache, agentLoop: loop);

        await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Внеси осеменение крови 25."
        });

        Assert.Equal("Внеси осеменение корове 25.", loop.LastUserText);
    }

    [Fact]
    public async Task CreateTextDraft_NormalizesSpokenBullDigitsBeforeLlm()
    {
        var cache = new FakeDistributedCache();
        var loop = new FakeAiAgentLoop();
        var service = CreateService(cache, agentLoop: loop);

        await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Тип осеменения естественный. Бык. 1. 0. 0. 15."
        });

        Assert.Equal("Тип осеменения естественный. Бык 10015.", loop.LastUserText);
    }

    [Fact]
    public async Task CreateTextDraft_WhenNewWriteCommandArrives_DoesNotMergeIntoStaleInseminationDraft()
    {
        var sourceArguments = JsonSerializer.SerializeToElement(new
        {
            schema_version = "v1",
            batch_idempotency_key = "ins-1",
            items = new[]
            {
                new
                {
                    idempotency_key = "ins-1-0",
                    cow_tags = new[] { "T044F" },
                    date = (string?)null,
                    insemination_type = (string?)null,
                    bull_tags = Array.Empty<string>()
                }
            }
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var payload = new AiWriteDraftPayload
        {
            ToolName = AiAssistantToolNames.CreateInsemination,
            BatchIdempotencyKey = "ins-1",
            SourceArguments = sourceArguments,
            Items =
            {
                new AiWriteDraftItem
                {
                    Index = 0,
                    IdempotencyKey = "ins-1-0:T044F",
                    Tag = "T044F",
                    Status = AiWriteItemStatus.NotFound,
                    Message = "Животное с биркой T044F не найдено.",
                    CanCommit = false
                }
            }
        };
        var loopResult = new AiAgentLoopResult
        {
            Status = AiAgentLoopStatus.ToolResult,
            ToolCall = new AiAgentToolCall
            {
                Name = AiAssistantToolNames.CreateInsemination,
                Arguments = sourceArguments
            },
            ToolResult = AiAgentToolResult.Ok(
                AiAssistantToolNames.CreateInsemination,
                "Пока ничего не сохранено. Я не нашла животное с биркой T044F.",
                JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                isTerminal: true,
                canCommit: false)
        };
        var cache = new FakeDistributedCache();
        var loop = new FakeAiAgentLoop(loopResult);
        var writeTool = new FakeAiWriteToolService();
        var service = CreateService(cache, agentLoop: loop, writeToolService: writeTool);
        var conversationId = Guid.NewGuid().ToString();

        var first = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Внеси осеменение крови 25.",
            ConversationId = conversationId
        });
        await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Внеси осеменение корове 25.",
            ConversationId = first.ConversationId
        });

        Assert.Equal(2, loop.CallCount);
        Assert.Null(writeTool.LastToolCall);
    }

    [Fact]
    public async Task CreateTextDraft_WhenReadCommandArrives_DoesNotMergeIntoActiveWriteDraft()
    {
        var sourceArguments = JsonSerializer.SerializeToElement(new
        {
            schema_version = "v1",
            batch_idempotency_key = "ins-1",
            items = new[]
            {
                new
                {
                    idempotency_key = "ins-1-0",
                    cow_tags = new[] { "25" },
                    date = "2026-07-15",
                    insemination_type = "Естественное",
                    bull_tags = Array.Empty<string>()
                }
            }
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var payload = new AiWriteDraftPayload
        {
            ToolName = AiAssistantToolNames.CreateInsemination,
            BatchIdempotencyKey = "ins-1",
            SourceArguments = sourceArguments,
            Items =
            {
                new AiWriteDraftItem
                {
                    Index = 0,
                    IdempotencyKey = "ins-1-0:25",
                    Tag = "25",
                    Status = AiWriteItemStatus.Ambiguous,
                    Message = "Найдено несколько животных с биркой 25. Нужно уточнение.",
                    CanCommit = false
                }
            }
        };
        var firstLoopResult = new AiAgentLoopResult
        {
            Status = AiAgentLoopStatus.ToolResult,
            ToolCall = new AiAgentToolCall
            {
                Name = AiAssistantToolNames.CreateInsemination,
                Arguments = sourceArguments
            },
            ToolResult = AiAgentToolResult.Ok(
                AiAssistantToolNames.CreateInsemination,
                "Пока ничего не сохранено. Нашлось несколько животных с биркой 25.",
                JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                isTerminal: true,
                canCommit: false)
        };
        var secondLoopResult = new AiAgentLoopResult
        {
            Status = AiAgentLoopStatus.ToolResult,
            ToolCall = new AiAgentToolCall
            {
                Name = AiAssistantToolNames.GetAnimalCard,
                Arguments = JsonSerializer.SerializeToElement(new { schema_version = "v1", tag = "new tegt test" })
            },
            ToolResult = AiAgentToolResult.Ok(
                AiAssistantToolNames.GetAnimalCard,
                "По фразе «new tegt test» нашлось несколько похожих бирок.",
                JsonSerializer.SerializeToElement(new { state = "ambiguous" }),
                isTerminal: true,
                canCommit: false)
        };
        var cache = new FakeDistributedCache();
        var loop = new FakeAiAgentLoop(firstLoopResult, secondLoopResult);
        var writeTool = new FakeAiWriteToolService();
        var service = CreateService(cache, agentLoop: loop, writeToolService: writeTool);
        var conversationId = Guid.NewGuid().ToString();

        var first = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Внеси естественное осеменение корове 25.",
            ConversationId = conversationId
        });
        var second = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Найди карточку коровы new. tegt. test.",
            ConversationId = first.ConversationId
        });

        Assert.Equal(2, loop.CallCount);
        Assert.Null(writeTool.LastToolCall);
        Assert.NotEqual(first.DraftId, second.DraftId);
        Assert.Equal(AiAssistantToolNames.GetAnimalCard, second.Preview?.ToolName);
    }

    [Fact]
    public async Task CreateTextDraft_WhenWeightQuestionArrives_DoesNotMergeIntoActiveWriteDraft()
    {
        var sourceArguments = JsonSerializer.SerializeToElement(new
        {
            schema_version = "v1",
            idempotency_key = "weight-1",
            tag = "25",
            weight = 421,
            date = "2026-07-15",
            method = "Ручное взвешивание"
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var payload = new AiWriteDraftPayload
        {
            ToolName = AiAssistantToolNames.CreateWeight,
            SourceArguments = sourceArguments,
            Items =
            {
                new AiWriteDraftItem
                {
                    Index = 0,
                    IdempotencyKey = "weight-1",
                    Tag = "25",
                    Status = AiWriteItemStatus.Ambiguous,
                    Message = "Найдено несколько животных с биркой 25. Нужно уточнение.",
                    CanCommit = false
                }
            }
        };
        var firstLoopResult = new AiAgentLoopResult
        {
            Status = AiAgentLoopStatus.ToolResult,
            ToolCall = new AiAgentToolCall { Name = AiAssistantToolNames.CreateWeight, Arguments = sourceArguments },
            ToolResult = AiAgentToolResult.Ok(
                AiAssistantToolNames.CreateWeight,
                "Пока ничего не сохранено. Нашлось несколько животных с биркой 25.",
                JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                isTerminal: true,
                canCommit: false)
        };
        var secondLoopResult = new AiAgentLoopResult
        {
            Status = AiAgentLoopStatus.ToolResult,
            ToolCall = new AiAgentToolCall
            {
                Name = AiAssistantToolNames.GetWeightHistory,
                Arguments = JsonSerializer.SerializeToElement(new { schema_version = "v1", tag = "25" })
            },
            ToolResult = AiAgentToolResult.Ok(
                AiAssistantToolNames.GetWeightHistory,
                "История веса не найдена.",
                JsonSerializer.SerializeToElement(new { schemaVersion = "v1", items = Array.Empty<object>() }),
                isTerminal: true,
                canCommit: false)
        };
        var cache = new FakeDistributedCache();
        var loop = new FakeAiAgentLoop(firstLoopResult, secondLoopResult);
        var writeTool = new FakeAiWriteToolService();
        var service = CreateService(cache, agentLoop: loop, writeToolService: writeTool);

        var first = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Внеси вес 421 кг бирка 25 сегодня ручное взвешивание",
            ConversationId = Guid.NewGuid().ToString()
        });
        var second = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Какие веса были у коровы 25?",
            ConversationId = first.ConversationId
        });

        Assert.Equal(2, loop.CallCount);
        Assert.Null(writeTool.LastToolCall);
        Assert.Equal(AiAssistantToolNames.GetWeightHistory, second.Preview?.ToolName);
    }

    [Fact]
    public async Task CreateTextDraft_WhenConfirmCommandAndActiveDraft_CommitsWithoutLlm()
    {
        var payload = new AiWriteDraftPayload
        {
            ToolName = AiAssistantToolNames.CreateInsemination,
            Items =
            {
                new AiWriteDraftItem
                {
                    Index = 0,
                    IdempotencyKey = "ins-1-0:25",
                    Tag = "25",
                    Status = AiWriteItemStatus.Resolved,
                    Message = "Осеменение готово к проверке.",
                    CanCommit = true
                }
            }
        };
        var loopResult = new AiAgentLoopResult
        {
            Status = AiAgentLoopStatus.ToolResult,
            ToolCall = new AiAgentToolCall
            {
                Name = AiAssistantToolNames.CreateInsemination,
                Arguments = JsonSerializer.SerializeToElement(new { tag = "25" }, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            },
            ToolResult = AiAgentToolResult.Ok(
                AiAssistantToolNames.CreateInsemination,
                "Я подготовила запись об осеменении. Проверьте данные и подтвердите сохранение.",
                JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                isTerminal: true,
                canCommit: true)
        };
        var cache = new FakeDistributedCache();
        var loop = new FakeAiAgentLoop(loopResult);
        var service = CreateService(cache, agentLoop: loop);
        var first = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "Внеси осеменение корове 25 сегодня естественное",
            ConversationId = Guid.NewGuid().ToString()
        });

        var confirm = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO
        {
            Text = "подтверждаю",
            ConversationId = first.ConversationId
        });

        Assert.Equal(1, loop.CallCount);
        Assert.Equal(AiAssistantDraftStatus.Committed, confirm.Status);
    }

    [Fact]
    public async Task ConfirmDraft_WhenPartialPreviewRequiresExplicitPartialConfirm_DoesNotCommitOnNormalConfirm()
    {
        var payload = new AiWriteDraftPayload
        {
            ToolName = AiAssistantToolNames.CreateWeight,
            Items =
            {
                new AiWriteDraftItem
                {
                    Index = 0,
                    IdempotencyKey = "weight-1",
                    Tag = "523",
                    Status = AiWriteItemStatus.Resolved,
                    Message = "Готово.",
                    CanCommit = true
                },
                new AiWriteDraftItem
                {
                    Index = 1,
                    IdempotencyKey = "weight-2",
                    Tag = "0000",
                    Status = AiWriteItemStatus.NotFound,
                    Message = "Не найдено.",
                    CanCommit = false
                }
            }
        };
        var loopResult = new AiAgentLoopResult
        {
            Status = AiAgentLoopStatus.ToolResult,
            ToolCall = new AiAgentToolCall { Name = AiAssistantToolNames.CreateWeight },
            ToolResult = AiAgentToolResult.Ok(
                AiAssistantToolNames.CreateWeight,
                "Частичный черновик.",
                JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                isTerminal: true,
                canCommit: true)
        };
        var cache = new FakeDistributedCache();
        var writeTool = new FakeAiWriteToolService();
        var service = CreateService(cache, loopResult, writeToolService: writeTool);

        var draft = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO { Text = "Внеси вес" });
        var normalConfirm = await service.ConfirmDraftAsync(OrgId, draft.DraftId, new AiAssistantConfirmRequestDTO
        {
            IdempotencyKey = "confirm-key"
        });
        Assert.True(draft.Preview!.Arguments!.Value.GetProperty("requiresPartialConfirm").GetBoolean());
        Assert.Equal(AiAssistantDraftStatus.CannotCommit, normalConfirm.Status);
        Assert.Equal(0, writeTool.CommitCount);

        var partialConfirm = await service.ConfirmDraftAsync(OrgId, draft.DraftId, new AiAssistantConfirmRequestDTO
        {
            IdempotencyKey = "confirm-key",
            ConfirmPartial = true
        });

        Assert.Equal(AiAssistantDraftStatus.Committed, partialConfirm.Status);
        Assert.Equal(1, writeTool.CommitCount);
    }

    [Fact]
    public async Task ConfirmDraft_RepeatedConfirmWithSameIdempotencyKey_ReturnsCachedResultWithoutSecondCommit()
    {
        var payload = new AiWriteDraftPayload
        {
            ToolName = AiAssistantToolNames.CreateWeight,
            Items =
            {
                new AiWriteDraftItem
                {
                    Index = 0,
                    IdempotencyKey = "weight-1",
                    Tag = "523",
                    Status = AiWriteItemStatus.Resolved,
                    Message = "Готово.",
                    CanCommit = true
                }
            }
        };
        var loopResult = new AiAgentLoopResult
        {
            Status = AiAgentLoopStatus.ToolResult,
            ToolCall = new AiAgentToolCall { Name = AiAssistantToolNames.CreateWeight },
            ToolResult = AiAgentToolResult.Ok(
                AiAssistantToolNames.CreateWeight,
                "Готовый черновик.",
                JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                isTerminal: true,
                canCommit: true)
        };
        var cache = new FakeDistributedCache();
        var writeTool = new FakeAiWriteToolService();
        var service = CreateService(cache, loopResult, writeToolService: writeTool);
        var draft = await service.CreateTextDraftAsync(OrgId, new AiAssistantTextRequestDTO { Text = "Внеси вес" });
        var request = new AiAssistantConfirmRequestDTO { IdempotencyKey = "confirm-key" };

        var first = await service.ConfirmDraftAsync(OrgId, draft.DraftId, request);
        var second = await service.ConfirmDraftAsync(OrgId, draft.DraftId, request);

        Assert.Equal(AiAssistantDraftStatus.Committed, first.Status);
        Assert.Equal(AiAssistantDraftStatus.Committed, second.Status);
        Assert.Equal(first.Message, second.Message);
        Assert.Equal(1, writeTool.CommitCount);
    }

    [Fact]
    public async Task CreateVoiceDraft_UsesOnlyServerAsrTranscript()
    {
        var cache = new FakeDistributedCache();
        var loop = new FakeAiAgentLoop();
        var asr = new FakeAiAsrClient(new AiAsrTranscription(
            "Внеси осеменение T044F",
            "Внеси осеменение T044F",
            "test",
            0));
        var service = CreateService(cache, agentLoop: loop, asrClient: asr);

        var response = await service.CreateVoiceDraftAsync(
            OrgId,
            new MemoryStream(new byte[] { 1, 2, 3 }),
            "voice.webm",
            "audio/webm",
            "client-voice",
            Guid.NewGuid().ToString());

        Assert.Equal("Внеси осеменение T044F", response.Transcript);
        Assert.Equal("Внеси осеменение T044F", loop.LastUserText);
    }

    private static AiAssistantService CreateService(
        IDistributedCache cache,
        AiAgentLoopResult? loopResult = null,
        IAiAuditService? auditService = null,
        IAiAgentLoop? agentLoop = null,
        IAiWriteToolService? writeToolService = null,
        IAiAsrClient? asrClient = null)
        => new(
            cache,
            agentLoop ?? new FakeAiAgentLoop(loopResult),
            new FakeAiToolExecutor(),
            writeToolService ?? new FakeAiWriteToolService(),
            auditService ?? new RecordingAiAuditService(),
            asrClient ?? new FakeAiAsrClient(),
            Options.Create(new AiAssistantOptions { DraftTtlMinutes = 10 }));

    private sealed class FakeAiAgentLoop : IAiAgentLoop
    {
        private readonly Queue<AiAgentLoopResult> _results;

        public IReadOnlyList<AiAgentMessage> LastPriorHistory { get; private set; } = Array.Empty<AiAgentMessage>();
        public string LastUserText { get; private set; } = string.Empty;
        public int CallCount { get; private set; }

        public FakeAiAgentLoop(AiAgentLoopResult? result = null)
        {
            _results = result == null
                ? new Queue<AiAgentLoopResult>()
                : new Queue<AiAgentLoopResult>(new[] { result });
        }

        public FakeAiAgentLoop(params AiAgentLoopResult[] results)
        {
            _results = new Queue<AiAgentLoopResult>(results);
        }

        public Task<AiAgentLoopResult> RunAsync(
            Guid organizationId,
            string userText,
            IReadOnlyList<AiAgentMessage>? priorHistory = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastUserText = userText;
            LastPriorHistory = priorHistory?.ToArray() ?? Array.Empty<AiAgentMessage>();
            if (_results.Count > 0)
                return Task.FromResult(_results.Dequeue());

            return Task.FromResult(new AiAgentLoopResult
            {
                Status = AiAgentLoopStatus.FinalAnswer,
                FinalAnswer = "stub",
                History = new[]
                {
                    AiAgentMessage.User(userText),
                    AiAgentMessage.AssistantFinal("stub")
                }
            });
        }
    }

    private sealed class FakeAiToolExecutor : IAiToolExecutor
    {
        public Task<AiAgentToolResult> ExecuteAsync(
            Guid organizationId,
            AiAgentToolCall toolCall,
            CancellationToken cancellationToken = default)
            => Task.FromResult(AiAgentToolResult.Ok(toolCall.Name, "ok"));
    }

    private sealed class FakeAiWriteToolService : IAiWriteToolService
    {
        public AiAgentToolCall? LastToolCall { get; private set; }
        public int CommitCount { get; private set; }

        public AiAgentToolResult CreatePreview(Guid organizationId, AiAgentToolCall toolCall)
        {
            LastToolCall = toolCall;
            var item = toolCall.Arguments!.Value.GetProperty("items")[0];
            var tag = item.GetProperty("cow_tags")[0].GetString();
            var payload = new AiWriteDraftPayload
            {
                ToolName = toolCall.Name,
                BatchIdempotencyKey = "ins-1",
                SourceArguments = toolCall.Arguments,
                Items =
                {
                    new AiWriteDraftItem
                    {
                        Index = 0,
                        IdempotencyKey = $"ins-1-0:{tag}",
                        Tag = tag,
                        Status = AiWriteItemStatus.Resolved,
                        Message = "Осеменение готово к проверке.",
                        CanCommit = true
                    }
                }
            };

            return AiAgentToolResult.Ok(
                toolCall.Name,
                "Я подготовила запись об осеменении. Проверьте данные и подтвердите сохранение.",
                JsonSerializer.SerializeToElement(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
                isTerminal: true,
                canCommit: true);
        }

        public AiWriteDraftPayload SelectCandidate(Guid organizationId, AiWriteDraftPayload payload, int itemIndex, Guid candidateId)
            => throw new NotImplementedException();

        public AiWriteCommitReport Commit(Guid organizationId, Guid draftId, AiWriteDraftPayload payload)
        {
            CommitCount++;
            return new("v1", payload.ToolName, draftId, payload.Items.Count, payload.CommitReadyCount, 0, payload.Items.Count(i => !i.CanCommit), Array.Empty<AiWriteCommitItemReport>(), "ok", "ok");
        }
    }

    private sealed class FakeAiAsrClient : IAiAsrClient
    {
        private readonly AiAsrTranscription _transcription;

        public FakeAiAsrClient(AiAsrTranscription? transcription = null)
        {
            _transcription = transcription ?? new AiAsrTranscription("тест", "тест", "test", 0);
        }

        public string Provider => "test";

        public Task<AiAsrTranscription> TranscribeAsync(
            Stream audio,
            string fileName,
            string? contentType,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_transcription);
    }

    private sealed class FakeDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public byte[]? Get(string key)
            => _values.GetValueOrDefault(key);

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
            => Task.FromResult(Get(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            => _values[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
            => Task.CompletedTask;

        public void Remove(string key)
            => _values.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public bool Contains(string key)
            => _values.ContainsKey(key);
    }

    private sealed class RecordingAiAuditService : IAiAuditService
    {
        public List<(string ActionType, string Status, AiAgentError? Error)> Events { get; } = new();

        public void Log(Guid organizationId, string actionType, string status, object details, AiAgentError? error = null)
            => Events.Add((actionType, status, error));
    }
}
