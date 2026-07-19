using System.Text.Json;
using CAT.Controllers.DTO;
using CAT.Controllers.DTO.AiAssistant;
using CAT.Services.Interfaces;

namespace CAT.Services.Ai;

public interface IAiWriteToolService
{
    AiAgentToolResult CreatePreview(Guid organizationId, AiAgentToolCall toolCall);

    AiWriteDraftPayload SelectCandidate(Guid organizationId, AiWriteDraftPayload payload, int itemIndex, Guid candidateId);

    AiWriteCommitReport Commit(Guid organizationId, Guid draftId, AiWriteDraftPayload payload);
}

public sealed class AiWriteToolService : IAiWriteToolService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiReadToolDataSource _readDataSource;
    private readonly IAiToolValidator _validator;
    private readonly IWeightsService _weightsService;
    private readonly IDailyActionService _dailyActionService;
    private readonly IAnimalService _animalService;

    public AiWriteToolService(
        IAiReadToolDataSource readDataSource,
        IAiToolValidator validator,
        IWeightsService weightsService,
        IDailyActionService dailyActionService,
        IAnimalService animalService)
    {
        _readDataSource = readDataSource;
        _validator = validator;
        _weightsService = weightsService;
        _dailyActionService = dailyActionService;
        _animalService = animalService;
    }

    public AiAgentToolResult CreatePreview(Guid organizationId, AiAgentToolCall toolCall)
    {
        var payload = BuildPayload(organizationId, toolCall, null);

        if (payload == null)
        {
            return AiAgentToolResult.Fail(
                toolCall.Name,
                AiAgentError.Create("AI_WRITE_TOOL_UNKNOWN", $"Write-инструмент {toolCall.Name} не поддержан."));
        }

        var preview = ToPreview(payload);
        return AiAgentToolResult.Ok(
            toolCall.Name,
            preview.Voice,
            JsonSerializer.SerializeToElement(payload, JsonOptions),
            isTerminal: true,
            canCommit: payload.CommitReadyCount > 0);
    }

    public AiWriteDraftPayload SelectCandidate(Guid organizationId, AiWriteDraftPayload payload, int itemIndex, Guid candidateId)
    {
        var item = payload.Items.SingleOrDefault(candidate => candidate.Index == itemIndex);
        if (item == null || item.Status != AiWriteItemStatus.Ambiguous ||
            !item.Candidates.Any(candidate => Guid.TryParse(candidate.Id, out var id) && id == candidateId))
            throw new ArgumentException("Выбранное животное недоступно для этого черновика.");

        if (!payload.SourceArguments.HasValue)
            throw new InvalidOperationException("Черновик не содержит исходных данных для выбора животного.");

        payload.SelectedAnimalIds[item.IdempotencyKey] = candidateId;
        return BuildPayload(organizationId, new AiAgentToolCall
        {
            Name = payload.ToolName,
            Arguments = payload.SourceArguments.Value
        }, payload.SelectedAnimalIds)
            ?? throw new InvalidOperationException("Не удалось пересобрать черновик.");
    }

    public AiWriteCommitReport Commit(Guid organizationId, Guid draftId, AiWriteDraftPayload payload)
    {
        var reports = new List<AiWriteCommitItemReport>();

        foreach (var item in payload.Items)
        {
            if (!item.CanCommit)
            {
                reports.Add(new AiWriteCommitItemReport(item.Index, item.IdempotencyKey, item.Tag, AiWriteItemStatus.Skipped, item.Message));
                continue;
            }

            var validation = ValidateItem(organizationId, payload.ToolName, item);
            if (!validation.IsValid)
            {
                reports.Add(new AiWriteCommitItemReport(item.Index, item.IdempotencyKey, item.Tag, AiWriteItemStatus.Failed, validation.Errors.First().Message));
                continue;
            }

            try
            {
                CommitItem(organizationId, payload.ToolName, item);
                reports.Add(new AiWriteCommitItemReport(item.Index, item.IdempotencyKey, item.Tag, AiWriteItemStatus.Committed, "Сохранено."));
            }
            catch (Exception ex)
            {
                reports.Add(new AiWriteCommitItemReport(item.Index, item.IdempotencyKey, item.Tag, AiWriteItemStatus.Failed, ex.Message));
            }
        }

        var committed = reports.Count(i => i.Status == AiWriteItemStatus.Committed);
        var failed = reports.Count(i => i.Status == AiWriteItemStatus.Failed);
        var skipped = reports.Count(i => i.Status == AiWriteItemStatus.Skipped);
        var report = new AiWriteCommitReport(
            "v1",
            payload.ToolName,
            draftId,
            reports.Count,
            committed,
            failed,
            skipped,
            reports,
            string.Empty,
            string.Empty);

        var message = AiWriteAssistantMessages.ForCommit(report);
        return report with { Text = message, Voice = message };
    }

    private AiWriteDraftPayload? BuildPayload(
        Guid organizationId,
        AiAgentToolCall toolCall,
        IReadOnlyDictionary<string, Guid>? selectedAnimalIds)
    {
        var payload = toolCall.Name switch
        {
            AiAssistantToolNames.CreateWeight => BuildCreateWeightDraft(organizationId, toolCall, selectedAnimalIds),
            AiAssistantToolNames.CreateDailyAction => BuildCreateDailyActionDraft(organizationId, toolCall, selectedAnimalIds),
            AiAssistantToolNames.CreateInsemination => BuildCreateInseminationDraft(organizationId, toolCall, selectedAnimalIds),
            _ => null
        };

        if (payload != null)
        {
            payload.SourceArguments = toolCall.Arguments?.Clone();
            payload.SelectedAnimalIds = selectedAnimalIds?.ToDictionary(pair => pair.Key, pair => pair.Value)
                ?? new Dictionary<string, Guid>();
        }

        return payload;
    }

    private AiWriteDraftPayload BuildCreateWeightDraft(
        Guid organizationId,
        AiAgentToolCall toolCall,
        IReadOnlyDictionary<string, Guid>? selectedAnimalIds)
    {
        var payload = NewPayload(toolCall.Name, GetString(toolCall, "idempotency_key"));
        var item = NewItem(0, GetString(toolCall, "idempotency_key") ?? Guid.NewGuid().ToString("N"), GetString(toolCall, "tag"));
        payload.Items.Add(item);

        var resolved = ResolveSingleAnimal(organizationId, item, GetSelectedAnimalId(selectedAnimalIds, item));
        if (!resolved.HasValue) return payload;

        item.Weight = new WeightCreateDTO
        {
            AnimalId = resolved.Value,
            Date = GetDate(toolCall, "date") ?? default,
            Weight = GetDouble(toolCall, "weight"),
            Method = GetString(toolCall, "method") ?? string.Empty
        };

        ApplyValidation(item, _validator.ValidateCreateWeight(organizationId, item.Weight));
        if (item.CanCommit) item.Message = $"Вес {item.Weight.Weight} кг, дата {item.Weight.Date}.";
        return payload;
    }

    private AiWriteDraftPayload BuildCreateDailyActionDraft(
        Guid organizationId,
        AiAgentToolCall toolCall,
        IReadOnlyDictionary<string, Guid>? selectedAnimalIds)
    {
        var payload = NewPayload(toolCall.Name, GetString(toolCall, "batch_idempotency_key"));
        var items = GetArray(toolCall, "items");
        var index = 0;

        foreach (var element in items)
        {
            var item = NewItem(index, GetString(element, "idempotency_key") ?? $"{payload.BatchIdempotencyKey}:{index}", GetString(element, "tag"));
            payload.Items.Add(item);
            index++;

            var resolved = ResolveSingleAnimal(organizationId, item, GetSelectedAnimalId(selectedAnimalIds, item));
            if (!resolved.HasValue) continue;

            item.DailyAction = new CreateDailyActionDTO
            {
                AnimalId = resolved.Value,
                Type = GetString(element, "type"),
                Subtype = GetString(element, "subtype"),
                PerformedBy = GetString(element, "performed_by"),
                Result = GetString(element, "result"),
                Medicine = GetString(element, "medicine"),
                Dose = GetString(element, "dose"),
                OldGroupId = GetGuid(element, "old_group_id"),
                NewGroupId = ResolveGroupId(organizationId, element, "new_group_id", "new_group_name", "group_name"),
                OldType = GetString(element, "old_type"),
                NewType = GetString(element, "new_type"),
                Date = GetDate(element, "date"),
                NextDate = GetDate(element, "next_date"),
                ResearchName = GetString(element, "research_name"),
                MaterialType = GetString(element, "material_type"),
                IdentificationValue = GetString(element, "identification_value")
            };

            ApplyValidation(item, _validator.ValidateCreateDailyActions(organizationId, new[] { item.DailyAction }));
            if (item.DailyAction.Type == "Перевод" && !item.DailyAction.NewGroupId.HasValue)
            {
                item.Status = AiWriteItemStatus.Invalid;
                item.CanCommit = false;
                item.Message = "Для перевода нужно указать существующую группу.";
                item.ValidationErrors.Add(new AiValidationError(
                    AiValidationRules.RequiredField,
                    "AI group resolver",
                    "Группа для перевода не найдена. Уточните название группы.",
                    "$.items[].new_group_name",
                    RetryableByLlm: true));
            }
            if (item.CanCommit) item.Message = $"{item.DailyAction.Type}, дата {item.DailyAction.Date}.";
        }

        return payload;
    }

    private AiWriteDraftPayload BuildCreateInseminationDraft(
        Guid organizationId,
        AiAgentToolCall toolCall,
        IReadOnlyDictionary<string, Guid>? selectedAnimalIds)
    {
        var payload = NewPayload(toolCall.Name, GetString(toolCall, "batch_idempotency_key"));
        var items = GetArray(toolCall, "items");
        var index = 0;

        foreach (var element in items)
        {
            var cowTags = GetStringArray(element, "cow_tags");
            if (cowTags.Count == 0)
            {
                var missing = NewItem(index++, GetString(element, "idempotency_key") ?? $"{payload.BatchIdempotencyKey}:missing", null);
                missing.Status = AiWriteItemStatus.Invalid;
                missing.Message = "Нужно указать хотя бы одну бирку коровы.";
                payload.Items.Add(missing);
                continue;
            }

            foreach (var cowTag in cowTags)
            {
                var item = NewItem(index, $"{GetString(element, "idempotency_key") ?? payload.BatchIdempotencyKey}:{cowTag}", cowTag);
                payload.Items.Add(item);
                index++;

                var cowId = ResolveSingleAnimal(organizationId, item, GetSelectedAnimalId(selectedAnimalIds, item));
                if (!cowId.HasValue) continue;

                var bullIds = new List<Guid>();
                foreach (var bullTag in GetStringArray(element, "bull_tags"))
                {
                    var bullItem = NewItem(item.Index, item.IdempotencyKey, bullTag);
                    var bullId = ResolveSingleAnimal(organizationId, bullItem);
                    if (!bullId.HasValue)
                    {
                        item.Status = bullItem.Status;
                        item.Message = $"Бык с биркой {bullTag}: {bullItem.Message}";
                        item.Candidates = bullItem.Candidates;
                        break;
                    }
                    bullIds.Add(bullId.Value);
                }

                if (item.Status is AiWriteItemStatus.Ambiguous or AiWriteItemStatus.NotFound)
                    continue;

                item.Insemination = new InseminationItemDTO
                {
                    CowIds = new List<Guid> { cowId.Value },
                    Date = GetDate(element, "date") ?? default,
                    InseminationType = GetString(element, "insemination_type") ?? string.Empty,
                    SpermBatch = GetString(element, "sperm_batch"),
                    SpermManufacturer = GetString(element, "sperm_manufacturer"),
                    BullIds = bullIds,
                    EmbryoId = GetString(element, "embryo_id"),
                    EmbryoManufacturer = GetString(element, "embryo_manufacturer"),
                    Technician = GetString(element, "technician"),
                    BullName = GetString(element, "bull_name")
                };

                ApplyValidation(item, _validator.ValidateCreateInsemination(organizationId, new InseminationBatchDTO { Items = new List<InseminationItemDTO> { item.Insemination } }));
                if (item.CanCommit) item.Message = $"Осеменение, дата {item.Insemination.Date}.";
            }
        }

        return payload;
    }

    private Guid? ResolveSingleAnimal(Guid organizationId, AiWriteDraftItem item, Guid? selectedAnimalId = null)
    {
        if (string.IsNullOrWhiteSpace(item.Tag))
        {
            item.Status = AiWriteItemStatus.Invalid;
            item.Message = "Бирка обязательна.";
            return null;
        }

        if (selectedAnimalId.HasValue)
        {
            var selected = _readDataSource.GetAnimalById(organizationId, selectedAnimalId.Value);
            if (selected != null && string.Equals(selected.TagNumber, item.Tag, StringComparison.OrdinalIgnoreCase))
            {
                item.Status = AiWriteItemStatus.Resolved;
                item.Message = $"Выбрано животное с биркой {item.Tag}.";
                return selected.Id;
            }
        }

        var matches = _readDataSource.FindAnimalsByExactTag(organizationId, item.Tag, includeInactive: true);
        if (matches.Count == 0)
        {
            var matchedTags = AiTagMatcher.FindCandidates(
                item.Tag,
                _readDataSource.GetAnimalTags(organizationId, includeInactive: true));
            if (matchedTags.Count > 0)
            {
                matches = matchedTags
                    .SelectMany(tag => _readDataSource.FindAnimalsByExactTag(organizationId, tag, includeInactive: true))
                    .ToList();
            }
        }

        if (matches.Count == 0)
        {
            item.Status = AiWriteItemStatus.NotFound;
            item.Message = $"Животное с биркой {item.Tag} не найдено.";
            return null;
        }

        if (matches.Count > 1)
        {
            item.Status = AiWriteItemStatus.Ambiguous;
            item.Message = $"Найдено несколько животных с биркой {item.Tag}. Нужно уточнение.";
            item.Candidates = matches.Take(5).Select(ToCandidate).ToList();
            return null;
        }

        item.Status = AiWriteItemStatus.Resolved;
        item.Message = $"Животное с биркой {item.Tag} найдено.";
        return matches[0].Id;
    }

    private static Guid? GetSelectedAnimalId(IReadOnlyDictionary<string, Guid>? selectedAnimalIds, AiWriteDraftItem item)
        => selectedAnimalIds != null && selectedAnimalIds.TryGetValue(item.IdempotencyKey, out var animalId)
            ? animalId
            : null;

    private void ApplyValidation(AiWriteDraftItem item, AiToolValidationResult validation)
    {
        item.ValidationErrors = validation.Errors.ToList();
        if (!validation.IsValid)
        {
            item.Status = AiWriteItemStatus.Invalid;
            item.CanCommit = false;
            item.Message = validation.Errors.First().Message;
            return;
        }

        item.Status = AiWriteItemStatus.Resolved;
        item.CanCommit = true;
    }

    private AiToolValidationResult ValidateItem(Guid organizationId, string toolName, AiWriteDraftItem item)
        => toolName switch
        {
            AiAssistantToolNames.CreateWeight when item.Weight != null
                => _validator.ValidateCreateWeight(organizationId, item.Weight),
            AiAssistantToolNames.CreateDailyAction when item.DailyAction != null
                => _validator.ValidateCreateDailyActions(organizationId, new[] { item.DailyAction }),
            AiAssistantToolNames.CreateInsemination when item.Insemination != null
                => _validator.ValidateCreateInsemination(organizationId, new InseminationBatchDTO { Items = new List<InseminationItemDTO> { item.Insemination } }),
            _ => AiToolValidationResult.FromErrors(new[]
            {
                new AiValidationError(AiValidationRules.RequiredField, "AI write draft", "Черновик повреждён: нет backend DTO.", "$")
            }, retryAttempt: 1)
        };

    private void CommitItem(Guid organizationId, string toolName, AiWriteDraftItem item)
    {
        switch (toolName)
        {
            case AiAssistantToolNames.CreateWeight:
                _weightsService.InsertWeights(item.Weight!);
                break;
            case AiAssistantToolNames.CreateDailyAction:
                _dailyActionService.CreateDailyAction(organizationId, item.DailyAction!);
                break;
            case AiAssistantToolNames.CreateInsemination:
                _animalService.InsertInseminations(new[] { item.Insemination! });
                break;
        }
    }

    private static AiWritePreviewResponse ToPreview(AiWriteDraftPayload payload)
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

    private static AiWriteDraftPayload NewPayload(string toolName, string? batchKey)
        => new() { ToolName = toolName, BatchIdempotencyKey = batchKey };

    private static AiWriteDraftItem NewItem(int index, string idempotencyKey, string? tag)
        => new() { Index = index, IdempotencyKey = idempotencyKey, Tag = AiEntityNormalizer.NormalizeAnimalTag(tag) };

    private static AiDisambiguationCandidate ToCandidate(CAT.EF.AiAnimalReadRecord animal)
        => new(
            animal.Id.ToString(),
            $"бирка {animal.TagNumber}, {animal.Type}, рожд. {animal.BirthDate}, группа {animal.GroupName}",
            new AiAnimalCandidate(
                animal.TagNumber,
                animal.Type,
                animal.Status,
                animal.BirthDate,
                animal.GroupName,
                animal.Breed,
                animal.Identifiers.Select(i => new AiIdentifierValue(i.Name, i.Value)).ToList()));

    private static List<JsonElement> GetArray(AiAgentToolCall toolCall, string name)
        => toolCall.Arguments.HasValue && toolCall.Arguments.Value.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(e => e.Clone()).ToList()
            : new List<JsonElement>();

    private static string? GetString(AiAgentToolCall toolCall, string name)
        => toolCall.Arguments.HasValue ? GetString(toolCall.Arguments.Value, name) : null;

    private static string? GetString(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object &&
           element.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static DateOnly? GetDate(AiAgentToolCall toolCall, string name)
        => toolCall.Arguments.HasValue ? GetDate(toolCall.Arguments.Value, name) : null;

    private static DateOnly? GetDate(JsonElement element, string name)
        => GetString(element, name) is { } value && DateOnly.TryParse(value, out var date) ? date : null;

    private static double? GetDouble(AiAgentToolCall toolCall, string name)
    {
        if (!toolCall.Arguments.HasValue ||
            !toolCall.Arguments.Value.TryGetProperty(name, out var value) ||
            value.ValueKind != JsonValueKind.Number)
            return null;

        return value.TryGetDouble(out var result) ? result : null;
    }

    private static Guid? GetGuid(JsonElement element, string name)
        => GetString(element, name) is { } value && Guid.TryParse(value, out var id) ? id : null;

    private Guid? ResolveGroupId(Guid organizationId, JsonElement element, string idName, params string[] nameFields)
    {
        if (GetGuid(element, idName) is { } explicitId)
            return explicitId;

        var groupName = nameFields
            .Select(field => GetString(element, field))
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        if (string.IsNullOrWhiteSpace(groupName))
            return null;

        var groups = _readDataSource.GetGroups(organizationId, includeEmpty: true);
        var exact = groups.FirstOrDefault(group =>
            string.Equals(group.Name, groupName, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
            return exact.Id;

        var normalizedInput = NormalizeGroupName(groupName);
        if (normalizedInput.Length == 0)
            return null;

        var normalizedMatches = groups
            .Select(group => new { Group = group, Normalized = NormalizeGroupName(group.Name) })
            .Where(item => item.Normalized == normalizedInput ||
                           item.Normalized.Contains(normalizedInput, StringComparison.Ordinal) ||
                           normalizedInput.Contains(item.Normalized, StringComparison.Ordinal))
            .Take(2)
            .ToList();

        return normalizedMatches.Count == 1 ? normalizedMatches[0].Group.Id : null;
    }

    private static string NormalizeGroupName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = AiTagMatcher.Normalize(value);
        foreach (var prefix in new[] { "gruppa", "group", "grp" })
        {
            if (normalized.StartsWith(prefix, StringComparison.Ordinal))
                normalized = normalized[prefix.Length..];
        }

        return normalized;
    }

    private static List<string> GetStringArray(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object &&
           element.TryGetProperty(name, out var value) &&
           value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList()
            : new List<string>();
}
