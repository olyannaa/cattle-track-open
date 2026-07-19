using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CAT.Controllers.DTO.AiAssistant;
using CAT.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace CAT.Services.Ai;

public sealed class AiAssistantService : IAiAssistantService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan ConversationTtl = TimeSpan.FromDays(7);
    private const int ConversationMessageLimit = 16;
    private static readonly Regex TagAfterBullRegex = new(
        @"\b(?:бык|быка|быком|быку)\s+(?<tag>[A-Za-zА-Яа-я0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TagAfterCowRegex = new(
        @"\b(?:корова|корове|корову|коровы|нетель|нетели|бирка|бирку|биркой)\s*[-:–—]?\s+(?:номер\s+)?(?<tag>[A-Za-zА-Яа-я0-9_-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex StandaloneTagRegex = new(
        @"^\s*(?:№\s*)?(?<tag>[A-Za-zА-Яа-я0-9_-]+)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex InseminationIntentRegex = new(
        @"\b(?:осемен|осеменени|осемени|осеменить|случк|покрыл)\w*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex NewWriteCommandRegex = new(
        @"\b(?:внеси|добавь|добавить|запиши|записать|создай|создать|осемени|сделай|переведи|переводи|перевод)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ReadCommandRegex = new(
        @"\b(?:найди|найти|открой|открыть|покажи|показать|список|какие|кого)\b.*\b(?:карточ\w*|животн\w*|коров\w*|бирк\w*|родител\w*|истори\w*|вес\w*|взвеш\w*|групп\w*|стельност\w*|стильност\w*|настельност\w*)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ConfirmCommandRegex = new(
        @"^\s*(?:да|подтверждаю|подтверди|сохрани|сохранить|записывай|да[, ]+записывай|верно|ок(?:ей)?)\.?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CancelCommandRegex = new(
        @"^\s*(?:нет|отмени|отмена|не\s+сохраняй|не\s+надо|стоп|удали\s+черновик)\.?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ContextualSpokenTagRegex = new(
        @"\b(?<prefix>бык|быка|быком|быку|корова|корове|корову|коровы|животное|животного|бирка|бирку|номер)\.?\s+(?<tag>\d+(?:[\s.,-]+\d+){1,7})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WeightValueRegex = new(
        @"\b(?:вес\s*)?(?<weight>\d+(?:[,.]\d+)?)\s*(?:кг|килограмм\w*)?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly IDistributedCache _cache;
    private readonly IAiAgentLoop _agentLoop;
    private readonly IAiToolExecutor _toolExecutor;
    private readonly IAiWriteToolService _writeToolService;
    private readonly IAiAuditService _auditService;
    private readonly IAiAsrClient _asrClient;
    private readonly AiAssistantOptions _options;

    public AiAssistantService(
        IDistributedCache cache,
        IAiAgentLoop agentLoop,
        IAiToolExecutor toolExecutor,
        IAiWriteToolService writeToolService,
        IAiAuditService auditService,
        IAiAsrClient asrClient,
        IOptions<AiAssistantOptions> options)
    {
        _cache = cache;
        _agentLoop = agentLoop;
        _toolExecutor = toolExecutor;
        _writeToolService = writeToolService;
        _auditService = auditService;
        _asrClient = asrClient;
        _options = options.Value;
    }

    public async Task<AiAssistantResponseDTO> CreateTextDraftAsync(
        Guid organizationId,
        AiAssistantTextRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Text))
            throw new ArgumentException("Текст запроса обязателен.", nameof(request));

        var userText = NormalizeUserText(request.Text);
        var now = DateTimeOffset.UtcNow;
        var conversation = await GetConversationAsync(organizationId, request.ConversationId, cancellationToken);
        var controlResponse = await TryApplyDraftControlCommandAsync(
            organizationId,
            conversation,
            userText,
            now,
            cancellationToken);
        if (controlResponse != null)
            return controlResponse;

        var clarificationResponse = await TryApplyClarificationToActiveDraftAsync(
            organizationId,
            conversation,
            userText,
            now,
            cancellationToken);
        if (clarificationResponse != null)
            return clarificationResponse;

        var ttl = TimeSpan.FromMinutes(Math.Max(1, _options.DraftTtlMinutes));
        var loopResult = await _agentLoop.RunAsync(
            organizationId,
            userText,
            conversation.Messages
                .Select(message => new AiAgentMessage { Role = message.Role, Content = message.Content })
                .ToList(),
            cancellationToken);
        var toolName = loopResult.ToolCall?.Name ?? AiAssistantToolNames.Unsupported;
        var message = BuildDraftMessage(loopResult);
        var warnings = BuildDraftWarnings(loopResult);
        var canCommit = loopResult.ToolResult?.CanCommit == true;
        var isWritePreview = IsWriteDraftPayload(loopResult.ToolResult?.Data);
        var writePayload = TryGetWritePayload(loopResult.ToolResult?.Data);
        var responseStatus = isWritePreview
            ? AiAssistantDraftStatus.Preview
            : AiAssistantDraftStatus.FinalAnswer;

        var draft = new AiAssistantDraftDTO
        {
            DraftId = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserText = userText,
            ClientRequestId = request.ClientRequestId,
            ConversationId = conversation.ConversationId,
            ToolName = toolName,
            Arguments = loopResult.ToolResult?.Data ?? loopResult.ToolCall?.Arguments,
            Status = responseStatus,
            Message = message,
            CanCommit = canCommit,
            RequiresPartialConfirm = writePayload?.RequiresPartialConfirm == true,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(ttl)
        };

        await _cache.SetStringAsync(
            GetDraftCacheKey(draft.DraftId),
            JsonSerializer.Serialize(draft, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);

        _auditService.Log(
            organizationId,
            AiAuditActionTypes.DraftCreated,
            loopResult.Error == null ? "success" : "error",
            new
            {
                draft.DraftId,
                draft.ToolName,
                draft.Status,
                draft.CanCommit,
                draft.ClientRequestId,
                draft.ExpiresAtUtc,
                AgentStatus = loopResult.Status,
                CommitReady = TryGetWritePayload(draft.Arguments)?.CommitReadyCount,
                PreviewSummary = TrimForAudit(draft.Message)
            },
            loopResult.Error);

        conversation.ActiveDraftId = writePayload != null ? draft.DraftId : conversation.ActiveDraftId;
        if (writePayload != null && !draft.CanCommit)
        {
            _auditService.Log(
                organizationId,
                AiAuditActionTypes.Clarification,
                "required",
                new
                {
                    draft.DraftId,
                    draft.ToolName,
                    draft.Status,
                    Reason = "no_commit_ready_items",
                    CommitReady = writePayload.CommitReadyCount,
                    Ambiguous = writePayload.Items.Count(i => i.Status == AiWriteItemStatus.Ambiguous),
                    NotFound = writePayload.Items.Count(i => i.Status == AiWriteItemStatus.NotFound),
                    Invalid = writePayload.Items.Count(i => i.Status == AiWriteItemStatus.Invalid)
                });
        }

        AddConversationMessage(conversation, AiAgentMessageRole.User, draft.UserText);
        AddConversationMessage(conversation, AiAgentMessageRole.Assistant, draft.Message);
        conversation.UpdatedAtUtc = now;
        await SaveConversationAsync(conversation, cancellationToken);

        return new AiAssistantResponseDTO
        {
            DraftId = draft.DraftId,
            Status = draft.Status,
            Message = draft.Message,
            ExpiresAtUtc = draft.ExpiresAtUtc,
            ConversationId = conversation.ConversationId,
            Preview = new AiAssistantPreviewDTO
            {
                ToolName = draft.ToolName,
                Summary = draft.Message,
                Arguments = BuildPreviewArguments(loopResult),
                Warnings = warnings
            }
        };
    }

    public async Task<AiAssistantVoiceResponseDTO> CreateVoiceDraftAsync(
        Guid organizationId,
        Stream audio,
        string fileName,
        string? contentType,
        string? clientRequestId,
        string? conversationId,
        CancellationToken cancellationToken = default)
    {
        var transcription = await _asrClient.TranscribeAsync(
            audio,
            fileName,
            contentType,
            cancellationToken);
        var effectiveText = NormalizeUserText(transcription.NormalizedText);

        _auditService.Log(
            organizationId,
            AiAuditActionTypes.AsrTranscription,
            "success",
            new
            {
                Provider = _asrClient.Provider,
                transcription.Model,
                transcription.LatencySeconds,
                TranscriptPreview = TrimForAudit(effectiveText),
                RawTranscriptPreview = TrimForAudit(transcription.NormalizedText)
            });

        var textResponse = await CreateTextDraftAsync(
            organizationId,
            new AiAssistantTextRequestDTO
            {
                Text = effectiveText,
                ClientRequestId = clientRequestId,
                ConversationId = conversationId
            },
            cancellationToken);

        return new AiAssistantVoiceResponseDTO
        {
            DraftId = textResponse.DraftId,
            Status = textResponse.Status,
            Message = textResponse.Message,
            Preview = textResponse.Preview,
            ExpiresAtUtc = textResponse.ExpiresAtUtc,
            ConversationId = textResponse.ConversationId,
            Transcript = effectiveText,
            RawTranscript = transcription.Text,
            AsrModel = transcription.Model,
            AsrLatencySeconds = transcription.LatencySeconds
        };
    }

    public async Task<AiAssistantResponseDTO> SelectDraftCandidateAsync(
        Guid organizationId,
        Guid draftId,
        int itemIndex,
        AiAssistantSelectCandidateRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(request?.CandidateId, out var candidateId))
            throw new ArgumentException("Выберите животное из предложенного списка.", nameof(request));

        var cacheKey = GetDraftCacheKey(draftId);
        var draftJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(draftJson))
            throw new ArgumentException("Черновик не найден или срок подтверждения истёк.");

        var draft = JsonSerializer.Deserialize<AiAssistantDraftDTO>(draftJson, JsonOptions)
            ?? throw new InvalidOperationException("Черновик AI повреждён.");
        if (draft.OrganizationId != organizationId)
            throw new UnauthorizedAccessException("Черновик не принадлежит организации пользователя.");
        if (draft.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            throw new ArgumentException("Черновик не найден или срок подтверждения истёк.");
        }

        var payload = TryGetWritePayload(draft.Arguments)
            ?? throw new ArgumentException("В этом сообщении нет черновика для выбора животного.");
        var updatedPayload = _writeToolService.SelectCandidate(organizationId, payload, itemIndex, candidateId);
        var preview = BuildWritePreview(updatedPayload);
        draft.Arguments = JsonSerializer.SerializeToElement(updatedPayload, JsonOptions);
        draft.Message = preview.Voice;
        draft.CanCommit = updatedPayload.CommitReadyCount > 0;
        draft.RequiresPartialConfirm = updatedPayload.RequiresPartialConfirm;

        var remaining = draft.ExpiresAtUtc - DateTimeOffset.UtcNow;
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(draft, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = remaining },
            cancellationToken);

        var conversation = await GetConversationAsync(organizationId, draft.ConversationId, cancellationToken);
        conversation.ActiveDraftId = draft.DraftId;
        AddConversationMessage(conversation, AiAgentMessageRole.User, "Выбрано животное из списка.");
        AddConversationMessage(conversation, AiAgentMessageRole.Assistant, draft.Message);
        conversation.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await SaveConversationAsync(conversation, cancellationToken);

        _auditService.Log(organizationId, AiAuditActionTypes.Clarification, "resolved", new
        {
            draft.DraftId,
            draft.ToolName,
            ItemIndex = itemIndex,
            CandidateId = candidateId,
            draft.CanCommit
        });

        return new AiAssistantResponseDTO
        {
            DraftId = draft.DraftId,
            Status = AiAssistantDraftStatus.Preview,
            Message = draft.Message,
            ExpiresAtUtc = draft.ExpiresAtUtc,
            ConversationId = draft.ConversationId,
            Preview = new AiAssistantPreviewDTO
            {
                ToolName = draft.ToolName,
                Summary = draft.Message,
                Arguments = JsonSerializer.SerializeToElement(preview, JsonOptions)
            }
        };
    }

    public async Task<AiAssistantResponseDTO> SelectReadCandidateAsync(
        Guid organizationId,
        AiAssistantSelectReadCandidateRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(request?.CandidateId, out var candidateId))
            throw new ArgumentException("Выберите животное из предложенного списка.", nameof(request));

        var toolName = NormalizeSelectedReadToolName(request.ToolName);
        if (toolName == null)
            throw new ArgumentException("Выбор животного доступен только для read-сценариев.");

        var now = DateTimeOffset.UtcNow;
        var conversation = await GetConversationAsync(organizationId, request.ConversationId, cancellationToken);
        var toolCall = new AiAgentToolCall
        {
            Name = toolName,
            Arguments = JsonSerializer.SerializeToElement(new
            {
                schema_version = "v1",
                animal_id = candidateId.ToString()
            }, JsonOptions)
        };

        var result = await _toolExecutor.ExecuteAsync(organizationId, toolCall, cancellationToken);
        _auditService.Log(
            organizationId,
            AiAuditActionTypes.ToolCall,
            result.Success ? "success" : "error",
            new
            {
                ToolName = toolName,
                SelectedAnimalId = candidateId,
                request.ConversationId
            },
            result.Error);

        if (!result.Success)
            throw new ArgumentException(result.Error?.Message ?? "Не удалось открыть выбранное животное.");

        AddConversationMessage(conversation, AiAgentMessageRole.User, $"Выбрано животное {candidateId:N}.");
        AddConversationMessage(conversation, AiAgentMessageRole.Assistant, result.Summary);
        conversation.UpdatedAtUtc = now;
        await SaveConversationAsync(conversation, cancellationToken);

        return new AiAssistantResponseDTO
        {
            DraftId = Guid.NewGuid(),
            Status = AiAssistantDraftStatus.FinalAnswer,
            Message = result.Summary,
            ExpiresAtUtc = now,
            ConversationId = conversation.ConversationId,
            Preview = new AiAssistantPreviewDTO
            {
                ToolName = toolName,
                Summary = result.Summary,
                Arguments = result.Data,
                Warnings = Array.Empty<string>()
            }
        };
    }

    public async Task<AiAssistantConfirmResponseDTO> ConfirmDraftAsync(
        Guid organizationId,
        Guid draftId,
        AiAssistantConfirmRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var confirmCacheKey = GetConfirmCacheKey(organizationId, draftId, request?.IdempotencyKey);
        if (confirmCacheKey != null)
        {
            var cachedConfirm = await _cache.GetStringAsync(confirmCacheKey, cancellationToken);
            if (!string.IsNullOrWhiteSpace(cachedConfirm))
            {
                return JsonSerializer.Deserialize<AiAssistantConfirmResponseDTO>(cachedConfirm, JsonOptions)
                    ?? throw new InvalidOperationException("Кэшированный результат подтверждения AI повреждён.");
            }
        }

        var cacheKey = GetDraftCacheKey(draftId);
        var draftJson = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (string.IsNullOrWhiteSpace(draftJson))
        {
            _auditService.Log(
                organizationId,
                AiAuditActionTypes.Commit,
                "error",
                new
                {
                    DraftId = draftId,
                    Reason = AiAssistantDraftStatus.ConfirmExpired
                },
                AiAgentError.Create("AI_DRAFT_CONFIRM_EXPIRED", "Черновик не найден или срок подтверждения истёк."));

            return new AiAssistantConfirmResponseDTO
            {
                DraftId = draftId,
                Status = AiAssistantDraftStatus.ConfirmExpired,
                Message = "Черновик не найден или срок подтверждения истёк."
            };
        }

        var draft = JsonSerializer.Deserialize<AiAssistantDraftDTO>(draftJson, JsonOptions)
            ?? throw new InvalidOperationException("Черновик AI повреждён.");

        if (draft.OrganizationId != organizationId)
            throw new UnauthorizedAccessException("Черновик не принадлежит организации пользователя.");

        if (request?.Confirm == false)
        {
            await _cache.RemoveAsync(cacheKey, cancellationToken);
            await ClearActiveDraftAsync(organizationId, draft.ConversationId, draft.DraftId, cancellationToken);
            _auditService.Log(
                organizationId,
                AiAuditActionTypes.Commit,
                "canceled",
                new
                {
                    draft.DraftId,
                    draft.ToolName,
                    draft.Status,
                    request?.IdempotencyKey
                });

            return new AiAssistantConfirmResponseDTO
            {
                DraftId = draft.DraftId,
                Status = AiAssistantDraftStatus.Canceled,
                Message = "Черновик отменён."
            };
        }

        if (!draft.CanCommit)
        {
            _auditService.Log(
                organizationId,
                AiAuditActionTypes.Clarification,
                "required",
                new
                {
                    draft.DraftId,
                    draft.ToolName,
                    draft.Status,
                    Reason = "confirm_without_commit_ready_items"
                });

            _auditService.Log(
                organizationId,
                AiAuditActionTypes.Commit,
                "error",
                new
                {
                    draft.DraftId,
                    draft.ToolName,
                    draft.Status,
                    request?.IdempotencyKey
                },
                AiAgentError.Create("AI_DRAFT_CANNOT_COMMIT", "Нет валидных строк для сохранения."));

            return new AiAssistantConfirmResponseDTO
            {
                DraftId = draft.DraftId,
                Status = AiAssistantDraftStatus.CannotCommit,
                Message = "Этот черновик нельзя подтвердить: нет валидных строк для сохранения."
            };
        }

        var payload = draft.Arguments.HasValue
            ? draft.Arguments.Value.Deserialize<AiWriteDraftPayload>(JsonOptions)
            : null;

        if (payload == null)
        {
            _auditService.Log(
                organizationId,
                AiAuditActionTypes.Commit,
                "error",
                new
                {
                    draft.DraftId,
                    draft.ToolName,
                    draft.Status,
                    Reason = "missing_payload",
                    request?.IdempotencyKey
                },
                AiAgentError.Create("AI_DRAFT_PAYLOAD_MISSING", "Черновик повреждён: нет данных для commit."));

            return new AiAssistantConfirmResponseDTO
            {
                DraftId = draft.DraftId,
                Status = AiAssistantDraftStatus.CannotCommit,
                Message = "Черновик повреждён: нет данных для commit."
            };
        }

        if (payload.RequiresPartialConfirm && request?.ConfirmPartial != true)
        {
            _auditService.Log(
                organizationId,
                AiAuditActionTypes.Clarification,
                "required",
                new
                {
                    draft.DraftId,
                    draft.ToolName,
                    draft.Status,
                    Reason = "partial_commit_requires_explicit_confirmation",
                    payload.CommitReadyCount,
                    Unresolved = payload.Items.Count(i => !i.CanCommit)
                });

            return new AiAssistantConfirmResponseDTO
            {
                DraftId = draft.DraftId,
                Status = AiAssistantDraftStatus.CannotCommit,
                Message = "В черновике есть строки, которые нельзя сохранить. Подтвердите сохранение только готовых записей отдельной кнопкой."
            };
        }

        var report = _writeToolService.Commit(organizationId, draft.DraftId, payload);
        await _cache.RemoveAsync(cacheKey, cancellationToken);
        await ClearActiveDraftAsync(organizationId, draft.ConversationId, draft.DraftId, cancellationToken);

        _auditService.Log(
            organizationId,
            AiAuditActionTypes.Commit,
            report.Failed == 0 ? "success" : report.Committed > 0 ? "partial" : "error",
            new
            {
                draft.DraftId,
                draft.ToolName,
                request?.IdempotencyKey,
                report.Total,
                report.Committed,
                report.Failed,
                report.Skipped,
                Items = report.Items
            });

        var response = new AiAssistantConfirmResponseDTO
        {
            DraftId = draft.DraftId,
            Status = AiAssistantDraftStatus.Committed,
            Message = report.Voice,
            CommitResult = JsonSerializer.SerializeToElement(report, JsonOptions)
        };

        if (confirmCacheKey != null)
        {
            await _cache.SetStringAsync(
                confirmCacheKey,
                JsonSerializer.Serialize(response, JsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(1, _options.DraftTtlMinutes))
                },
                cancellationToken);
        }

        return response;
    }

    private static string GetDraftCacheKey(Guid draftId)
        => $"ai-assistant:draft:{draftId:N}";

    private static string? GetConfirmCacheKey(Guid organizationId, Guid draftId, string? idempotencyKey)
        => string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : $"ai-assistant:confirm:{organizationId:N}:{draftId:N}:{idempotencyKey.Trim()}";

    private static string? NormalizeSelectedReadToolName(string? toolName)
        => toolName switch
        {
            AiAssistantToolNames.FindAnimal => AiAssistantToolNames.GetAnimalCard,
            AiAssistantToolNames.GetAnimalCard => AiAssistantToolNames.GetAnimalCard,
            AiAssistantToolNames.GetAnimalParents => AiAssistantToolNames.GetAnimalParents,
            AiAssistantToolNames.GetWeightHistory => AiAssistantToolNames.GetWeightHistory,
            _ => null
        };

    private static string GetConversationCacheKey(Guid organizationId, string conversationId)
        => $"ai-assistant:conversation:{organizationId:N}:{conversationId}";

    private async Task<AiConversationState> GetConversationAsync(
        Guid organizationId,
        string? requestedConversationId,
        CancellationToken cancellationToken)
    {
        var conversationId = Guid.TryParse(requestedConversationId, out var parsed)
            ? parsed.ToString("N")
            : Guid.NewGuid().ToString("N");
        var json = await _cache.GetStringAsync(GetConversationCacheKey(organizationId, conversationId), cancellationToken);
        if (!string.IsNullOrWhiteSpace(json))
        {
            var stored = JsonSerializer.Deserialize<AiConversationState>(json, JsonOptions);
            if (stored?.OrganizationId == organizationId)
                return stored;
        }

        return new AiConversationState
        {
            ConversationId = conversationId,
            OrganizationId = organizationId,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    private async Task SaveConversationAsync(AiConversationState conversation, CancellationToken cancellationToken)
        => await _cache.SetStringAsync(
            GetConversationCacheKey(conversation.OrganizationId, conversation.ConversationId),
            JsonSerializer.Serialize(conversation, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ConversationTtl },
            cancellationToken);

    private async Task ClearActiveDraftAsync(
        Guid organizationId,
        string? conversationId,
        Guid draftId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
            return;

        var conversation = await GetConversationAsync(organizationId, conversationId, cancellationToken);
        if (conversation.ActiveDraftId != draftId)
            return;

        conversation.ActiveDraftId = null;
        conversation.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await SaveConversationAsync(conversation, cancellationToken);
    }

    private async Task<AiAssistantResponseDTO?> TryApplyClarificationToActiveDraftAsync(
        Guid organizationId,
        AiConversationState conversation,
        string userText,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (conversation.ActiveDraftId is not { } draftId)
            return null;

        if (LooksLikeNewWriteCommand(userText))
            return null;

        if (LooksLikeReadCommand(userText))
            return null;

        var cacheKey = GetDraftCacheKey(draftId);
        var draftJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(draftJson))
        {
            conversation.ActiveDraftId = null;
            await SaveConversationAsync(conversation, cancellationToken);
            return null;
        }

        var draft = JsonSerializer.Deserialize<AiAssistantDraftDTO>(draftJson, JsonOptions);
        if (draft == null ||
            draft.OrganizationId != organizationId ||
            draft.ExpiresAtUtc <= now)
            return null;

        var payload = TryGetWritePayload(draft.Arguments);
        if (payload?.SourceArguments == null)
            return null;

        if (!TryMergeDraftClarification(draft.ToolName, payload, userText, out var mergedArguments))
            return null;

        var result = _writeToolService.CreatePreview(organizationId, new AiAgentToolCall
        {
            Name = draft.ToolName,
            Arguments = mergedArguments
        });
        if (!result.Success)
            return null;

        var updatedPayload = TryGetWritePayload(result.Data);
        if (updatedPayload == null)
            return null;

        draft.UserText = $"{draft.UserText}\n{userText.Trim()}";
        draft.Arguments = result.Data;
        draft.Message = result.Summary;
        draft.CanCommit = updatedPayload.CommitReadyCount > 0;
        draft.RequiresPartialConfirm = updatedPayload.RequiresPartialConfirm;

        var remaining = draft.ExpiresAtUtc - now;
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(draft, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = remaining },
            cancellationToken);

        AddConversationMessage(conversation, AiAgentMessageRole.User, userText.Trim());
        AddConversationMessage(conversation, AiAgentMessageRole.Assistant, draft.Message);
        conversation.ActiveDraftId = draft.DraftId;
        conversation.UpdatedAtUtc = now;
        await SaveConversationAsync(conversation, cancellationToken);

        _auditService.Log(
            organizationId,
            AiAuditActionTypes.Clarification,
            "merged",
            new
            {
                draft.DraftId,
                draft.ToolName,
                draft.CanCommit,
                PreviewSummary = TrimForAudit(draft.Message)
            });

        var preview = BuildWritePreview(updatedPayload);
        return new AiAssistantResponseDTO
        {
            DraftId = draft.DraftId,
            Status = AiAssistantDraftStatus.Preview,
            Message = draft.Message,
            ExpiresAtUtc = draft.ExpiresAtUtc,
            ConversationId = conversation.ConversationId,
            Preview = new AiAssistantPreviewDTO
            {
                ToolName = draft.ToolName,
                Summary = draft.Message,
                Arguments = JsonSerializer.SerializeToElement(preview, JsonOptions),
                Warnings = new[] { "Уточнение применено к текущему черновику, запись в БД ещё не выполнялась." }
            }
        };
    }

    private static bool TryMergeDraftClarification(
        string toolName,
        AiWriteDraftPayload payload,
        string userText,
        out JsonElement mergedArguments)
        => toolName switch
        {
            AiAssistantToolNames.CreateWeight => TryMergeWeightClarification(payload, userText, out mergedArguments),
            AiAssistantToolNames.CreateInsemination => TryMergeInseminationClarification(payload, userText, out mergedArguments),
            _ => TryMergeDailyActionClarification(payload, userText, out mergedArguments)
        };

    private static bool TryMergeWeightClarification(
        AiWriteDraftPayload payload,
        string userText,
        out JsonElement mergedArguments)
    {
        mergedArguments = default;

        var source = payload.SourceArguments!.Value;
        var root = JsonNode.Parse(source.GetRawText()) as JsonObject;
        if (root == null)
            return false;

        var changed = false;
        var needsTag = !root.TryGetPropertyValue("tag", out var tagNode) ||
                       string.IsNullOrWhiteSpace(tagNode?.ToString()) ||
                       payload.Items.Any(i => i.Status is AiWriteItemStatus.NotFound or AiWriteItemStatus.Ambiguous);

        foreach (var tag in ExtractTags(TagAfterCowRegex, userText))
        {
            root["tag"] = AiEntityNormalizer.NormalizeAnimalTag(tag);
            changed = true;
        }

        if (needsTag && StandaloneTagRegex.Match(userText) is { Success: true } standalone)
        {
            root["tag"] = AiEntityNormalizer.NormalizeAnimalTag(standalone.Groups["tag"].Value);
            changed = true;
        }

        var weightMatch = WeightValueRegex.Matches(userText)
            .Cast<Match>()
            .LastOrDefault(match => userText.Contains("вес", StringComparison.OrdinalIgnoreCase) ||
                                    userText.Contains("кг", StringComparison.OrdinalIgnoreCase) ||
                                    userText.Contains("килограмм", StringComparison.OrdinalIgnoreCase));
        if (weightMatch != null &&
            double.TryParse(
                weightMatch.Groups["weight"].Value.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var weight))
        {
            root["weight"] = weight;
            changed = true;
        }

        if (TryExtractRelativeDate(userText) is { } date)
        {
            root["date"] = date;
            changed = true;
        }

        if (TryExtractWeightMethod(userText) is { } method)
        {
            root["method"] = method;
            changed = true;
        }

        if (!changed)
            return false;

        mergedArguments = JsonSerializer.SerializeToElement(root, JsonOptions);
        return true;
    }

    private static bool TryMergeDailyActionClarification(
        AiWriteDraftPayload payload,
        string userText,
        out JsonElement mergedArguments)
    {
        mergedArguments = default;

        var source = payload.SourceArguments!.Value;
        var root = JsonNode.Parse(source.GetRawText()) as JsonObject;
        var items = root?["items"] as JsonArray;
        if (root == null || items == null)
            return false;

        var changed = false;
        foreach (var item in items.OfType<JsonObject>())
        {
            if (TryExtractRelativeDate(userText) is { } date)
            {
                item["date"] = date;
                changed = true;
            }
        }

        if (!changed)
            return false;

        mergedArguments = JsonSerializer.SerializeToElement(root, JsonOptions);
        return true;
    }

    private static bool TryMergeInseminationClarification(
        AiWriteDraftPayload payload,
        string userText,
        out JsonElement mergedArguments)
    {
        mergedArguments = default;

        var source = payload.SourceArguments!.Value;
        var root = JsonNode.Parse(source.GetRawText()) as JsonObject;
        var items = root?["items"] as JsonArray;
        var item = items?.OfType<JsonObject>().FirstOrDefault();
        if (root == null || item == null)
            return false;

        var changed = false;
        var cowTags = EnsureStringArray(item, "cow_tags");
        var bullTags = EnsureStringArray(item, "bull_tags");
        var needsCow = cowTags.Count == 0 ||
                       payload.Items.Any(i => i.Message.Contains("коров", StringComparison.OrdinalIgnoreCase) ||
                                              i.Message.Contains("бирк", StringComparison.OrdinalIgnoreCase));
        var needsBull = payload.Items.Any(i => i.Message.Contains("бык", StringComparison.OrdinalIgnoreCase)) ||
                        (StringEquals(item, "insemination_type", "Естественное") && bullTags.Count == 0);

        var cowClarificationTags = ExtractTags(TagAfterCowRegex, userText).ToList();
        if (cowClarificationTags.Count > 0 && ExistingCowResolutionFailed(payload))
        {
            cowTags.Clear();
            changed = true;
        }

        foreach (var tag in cowClarificationTags)
            changed |= AddString(cowTags, tag);

        foreach (var tag in ExtractTags(TagAfterBullRegex, userText))
            changed |= AddString(bullTags, tag);

        if (StandaloneTagRegex.Match(userText) is { Success: true } standalone)
        {
            var tag = standalone.Groups["tag"].Value;
            if (needsCow)
                changed |= AddString(cowTags, tag);
            else if (needsBull)
                changed |= AddString(bullTags, tag);
        }

        if (TryExtractInseminationType(userText) is { } type &&
            !StringEquals(item, "insemination_type", type))
        {
            item["insemination_type"] = type;
            changed = true;
        }

        if (TryExtractRelativeDate(userText) is { } date &&
            (!item.TryGetPropertyValue("date", out var dateNode) || dateNode?.ToString() != date))
        {
            item["date"] = date;
            changed = true;
        }

        if (!changed)
            return false;

        mergedArguments = JsonSerializer.SerializeToElement(root, JsonOptions);
        return true;
    }

    private async Task<AiAssistantResponseDTO?> TryApplyDraftControlCommandAsync(
        Guid organizationId,
        AiConversationState conversation,
        string userText,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (conversation.ActiveDraftId is not { } draftId)
            return null;

        var confirm = IsConfirmCommand(userText);
        var cancel = IsCancelCommand(userText);
        if (!confirm && !cancel)
            return null;

        var result = await ConfirmDraftAsync(
            organizationId,
            draftId,
                new AiAssistantConfirmRequestDTO
                {
                    Confirm = confirm,
                    IdempotencyKey = $"conversation:{conversation.ConversationId}:draft:{draftId:N}:confirm"
                },
            cancellationToken);

        AddConversationMessage(conversation, AiAgentMessageRole.User, userText);
        AddConversationMessage(conversation, AiAgentMessageRole.Assistant, result.Message);
        if (result.Status is AiAssistantDraftStatus.Committed or AiAssistantDraftStatus.Canceled)
            conversation.ActiveDraftId = null;
        conversation.UpdatedAtUtc = now;
        await SaveConversationAsync(conversation, cancellationToken);

        return new AiAssistantResponseDTO
        {
            DraftId = result.DraftId,
            Status = result.Status,
            Message = result.Message,
            ExpiresAtUtc = now,
            ConversationId = conversation.ConversationId,
            Preview = result.CommitResult.HasValue
                ? new AiAssistantPreviewDTO
                {
                    ToolName = TryGetCommitToolName(result.CommitResult.Value) ?? string.Empty,
                    Summary = result.Message,
                    Arguments = result.CommitResult,
                    Warnings = Array.Empty<string>()
                }
                : null
        };
    }

    private static string NormalizeUserText(string text)
    {
        var normalized = Regex.Replace(text.Trim(), @"\s+", " ");
        normalized = Regex.Replace(normalized, @"\bпирк\w*\b", "бирка", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bнастельност", "на стельност", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bстильност", "стельност", RegexOptions.IgnoreCase);
        normalized = ContextualSpokenTagRegex.Replace(normalized, match =>
        {
            var tag = AiEntityNormalizer.NormalizeAnimalTag(match.Groups["tag"].Value);
            return string.IsNullOrWhiteSpace(tag)
                ? match.Value
                : $"{match.Groups["prefix"].Value} {tag}";
        });

        if (InseminationIntentRegex.IsMatch(normalized))
        {
            normalized = Regex.Replace(
                normalized,
                @"\bкрови\s+(?=\d+\b)",
                "корове ",
                RegexOptions.IgnoreCase);
            normalized = Regex.Replace(
                normalized,
                @"\bкровь\s+(?=\d+\b)",
                "корове ",
                RegexOptions.IgnoreCase);
        }

        return normalized.Trim();
    }

    private static bool LooksLikeNewWriteCommand(string text)
        => NewWriteCommandRegex.IsMatch(text) &&
           (InseminationIntentRegex.IsMatch(text) ||
            text.Contains("вес", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("кг", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("лечение", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("вакцина", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("переведи", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("переводи", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("перевод", StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeReadCommand(string text)
        => ReadCommandRegex.IsMatch(text);

    private static bool IsConfirmCommand(string text)
        => ConfirmCommandRegex.IsMatch(text);

    private static bool IsCancelCommand(string text)
        => CancelCommandRegex.IsMatch(text);

    private static bool ExistingCowResolutionFailed(AiWriteDraftPayload payload)
        => payload.Items.Any(item => item.Status == AiWriteItemStatus.NotFound);

    private static string? TryGetCommitToolName(JsonElement commitResult)
        => commitResult.ValueKind == JsonValueKind.Object &&
           commitResult.TryGetProperty("toolName", out var toolName) &&
           toolName.ValueKind == JsonValueKind.String
            ? toolName.GetString()
            : null;

    private static JsonArray EnsureStringArray(JsonObject item, string propertyName)
    {
        if (item[propertyName] is JsonArray existing)
            return existing;

        var created = new JsonArray();
        item[propertyName] = created;
        return created;
    }

    private static bool AddString(JsonArray array, string? value)
    {
        var normalized = AiEntityNormalizer.NormalizeAnimalTag(value);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        if (array.Any(node => string.Equals(node?.ToString(), normalized, StringComparison.OrdinalIgnoreCase)))
            return false;

        array.Add(normalized);
        return true;
    }

    private static IEnumerable<string> ExtractTags(Regex regex, string text)
        => regex.Matches(text)
            .Select(match => match.Groups["tag"].Value)
            .Where(value => !string.IsNullOrWhiteSpace(value));

    private static bool StringEquals(JsonObject item, string propertyName, string expected)
        => item.TryGetPropertyValue(propertyName, out var value) &&
           string.Equals(value?.ToString(), expected, StringComparison.OrdinalIgnoreCase);

    private static string? TryExtractInseminationType(string text)
    {
        if (text.Contains("естествен", StringComparison.OrdinalIgnoreCase))
            return "Естественное";
        if (text.Contains("искусствен", StringComparison.OrdinalIgnoreCase))
            return "Искусственное";
        if (text.Contains("эмбрион", StringComparison.OrdinalIgnoreCase))
            return "Эмбрион";

        return null;
    }

    private static string? TryExtractWeightMethod(string text)
    {
        if (text.Contains("ручн", StringComparison.OrdinalIgnoreCase))
            return "Ручное взвешивание";
        if (text.Contains("автомат", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("станц", StringComparison.OrdinalIgnoreCase))
            return "Автоматическая весовая станция";
        if (text.Contains("расчет", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("расчёт", StringComparison.OrdinalIgnoreCase))
            return "Расчетный метод";

        return null;
    }

    private static string? TryExtractRelativeDate(string text)
    {
        if (text.Contains("сегодня", StringComparison.OrdinalIgnoreCase))
            return DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
        if (text.Contains("вчера", StringComparison.OrdinalIgnoreCase))
            return DateOnly.FromDateTime(DateTime.Now.AddDays(-1)).ToString("yyyy-MM-dd");

        return null;
    }

    private static void AddConversationMessage(AiConversationState conversation, string role, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return;

        conversation.Messages.Add(new AiConversationMessage
        {
            Role = role,
            Content = content.Length <= 1_500 ? content : content[..1_500]
        });

        if (conversation.Messages.Count > ConversationMessageLimit)
            conversation.Messages.RemoveRange(0, conversation.Messages.Count - ConversationMessageLimit);
    }

    private static JsonElement? BuildPreviewArguments(AiAgentLoopResult loopResult)
    {
        if (loopResult.ToolResult?.Data is not { } data)
            return loopResult.ToolCall?.Arguments;

        if (!loopResult.ToolResult.CanCommit)
            return IsWriteDraftPayload(data)
                ? JsonSerializer.SerializeToElement(BuildWritePreview(data.Deserialize<AiWriteDraftPayload>(JsonOptions)!), JsonOptions)
                : data;

        var payload = data.Deserialize<AiWriteDraftPayload>(JsonOptions);
        return payload == null
            ? data
            : JsonSerializer.SerializeToElement(BuildWritePreview(payload), JsonOptions);
    }

    private static bool IsWriteDraftPayload(JsonElement? data)
        => TryGetWritePayload(data) != null;

    private static AiWriteDraftPayload? TryGetWritePayload(JsonElement? data)
    {
        if (!data.HasValue)
            return null;

        try
        {
            var payload = data.Value.Deserialize<AiWriteDraftPayload>(JsonOptions);
            return string.IsNullOrWhiteSpace(payload?.ToolName) ? null : payload;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AiWritePreviewResponse BuildWritePreview(AiWriteDraftPayload payload)
    {
        var items = payload.Items
            .Select(i => new AiWriteItemPreview(
                i.Index,
                i.IdempotencyKey,
                i.Tag,
                i.Status,
                i.CanCommit,
                i.Message,
                i.Candidates,
                i.Weight ?? (object?)i.DailyAction ?? i.Insemination,
                i.ValidationErrors))
            .ToList();

        var ambiguous = items.Count(i => i.Status == AiWriteItemStatus.Ambiguous);
        var notFound = items.Count(i => i.Status == AiWriteItemStatus.NotFound);
        var invalid = items.Count(i => i.Status == AiWriteItemStatus.Invalid);
        var message = AiWriteAssistantMessages.ForPreview(payload);

        return new AiWritePreviewResponse(
            payload.SchemaVersion,
            payload.ToolName,
            payload.BatchIdempotencyKey,
            items.Count,
            payload.CommitReadyCount,
            ambiguous,
            notFound,
            invalid,
            payload.RequiresPartialConfirm,
            items,
            message,
            message);
    }

    private static string BuildDraftMessage(AiAgentLoopResult loopResult)
    {
        if (loopResult.Status == AiAgentLoopStatus.FinalAnswer && !string.IsNullOrWhiteSpace(loopResult.FinalAnswer))
            return loopResult.FinalAnswer;

        if (!string.IsNullOrWhiteSpace(loopResult.ToolResult?.Summary))
            return loopResult.ToolResult.Summary;

        if (!string.IsNullOrWhiteSpace(loopResult.Error?.Message))
            return loopResult.Error.Message;

        return "AI agent loop завершился без commit-ready черновика.";
    }

    private static IReadOnlyList<string> BuildDraftWarnings(AiAgentLoopResult loopResult)
    {
        var warnings = new List<string>();

        if (IsWriteDraftPayload(loopResult.ToolResult?.Data))
            warnings.Add(loopResult.ToolResult?.CanCommit == true
                ? "Черновик ожидает подтверждения, запись в БД ещё не выполнялась."
                : "Нужны уточнения: запись в БД ещё не выполнялась.");

        if (loopResult.Error != null)
            warnings.Add($"{loopResult.Error.Code}: {loopResult.Error.Message}");

        return warnings;
    }

    private static string? TrimForAudit(string? value)
        => string.IsNullOrWhiteSpace(value) || value.Length <= 500 ? value : value[..500];
}
