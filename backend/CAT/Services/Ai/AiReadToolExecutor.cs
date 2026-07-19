using System.Text.Json;
using CAT.Controllers.DTO.AiAssistant;
using CAT.EF;

namespace CAT.Services.Ai;

public sealed class AiReadToolExecutor : IAiToolExecutor
{
    private const string SchemaVersion = "v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiReadToolDataSource _dataSource;
    private readonly IAiWriteToolService _writeToolService;

    public AiReadToolExecutor(IAiReadToolDataSource dataSource, IAiWriteToolService writeToolService)
    {
        _dataSource = dataSource;
        _writeToolService = writeToolService;
    }

    public Task<AiAgentToolResult> ExecuteAsync(
        Guid organizationId,
        AiAgentToolCall toolCall,
        CancellationToken cancellationToken = default)
    {
        var result = toolCall.Name switch
        {
            AiAssistantToolNames.FindAnimal => FindAnimal(organizationId, toolCall),
            AiAssistantToolNames.GetAnimalCard => GetAnimalCard(organizationId, toolCall),
            AiAssistantToolNames.GetAnimalParents => GetAnimalParents(organizationId, toolCall),
            AiAssistantToolNames.GetWeightHistory => GetWeightHistory(organizationId, toolCall),
            AiAssistantToolNames.GetPregnanciesToCheck => GetPregnanciesToCheck(organizationId, toolCall),
            AiAssistantToolNames.ListGroups => ListGroups(organizationId, toolCall),
            AiAssistantToolNames.CreateWeight => _writeToolService.CreatePreview(organizationId, toolCall),
            AiAssistantToolNames.CreateDailyAction => _writeToolService.CreatePreview(organizationId, toolCall),
            AiAssistantToolNames.CreateInsemination => _writeToolService.CreatePreview(organizationId, toolCall),
            _ => AiAgentToolResult.Fail(
                toolCall.Name,
                AiAgentError.Create("AI_TOOL_UNKNOWN", $"Инструмент {toolCall.Name} не поддержан.", path: "$.toolCall.name"))
        };

        return Task.FromResult(result);
    }

    private AiAgentToolResult FindAnimal(Guid organizationId, AiAgentToolCall toolCall)
    {
        var tag = GetRequiredString(toolCall, "tag");
        if (tag.Error != null) return tag.Error;

        var includeInactive = GetBoolean(toolCall, "include_inactive") ?? false;
        var response = ResolveAnimalByTag(organizationId, tag.Value!, includeInactive);
        return TerminalOk(toolCall.Name, response.Message, response);
    }

    private AiAgentToolResult GetAnimalCard(Guid organizationId, AiAgentToolCall toolCall)
    {
        var animalId = GetAnimalIdFromTagOrId(organizationId, toolCall);
        if (animalId.Error != null) return animalId.Error;

        var animal = _dataSource.GetAnimalById(organizationId, animalId.Value);
        if (animal == null)
        {
            return TerminalOk(
                toolCall.Name,
                "Животное не найдено в вашей организации.",
                new
                {
                    schema_version = SchemaVersion,
                    state = AiReadResolutionState.NotFound,
                    animal_id = animalId.Value,
                    message = "Животное не найдено в вашей организации."
                });
        }

        var card = ToAnimalCard(animal);
        var text = $"Карточка животного {card.Tag}: тип {card.Type ?? "не указан"}, статус {card.Status ?? "не указан"}, группа {card.GroupName ?? "не указана"}.";
        var response = new AiAnimalCardResponse(SchemaVersion, card, text, text);
        return TerminalOk(toolCall.Name, response.Voice, response);
    }

    private AiAgentToolResult GetAnimalParents(Guid organizationId, AiAgentToolCall toolCall)
    {
        AiAnimalResolutionResponse resolution;
        AiAnimalReadRecord? animal;

        if (TryGetSelectedAnimalId(toolCall, out var selectedAnimalId))
        {
            animal = _dataSource.GetAnimalById(organizationId, selectedAnimalId);
            resolution = animal == null
                ? new AiAnimalResolutionResponse(
                    SchemaVersion,
                    "animal",
                    AiReadResolutionState.NotFound,
                    new { kind = "animal", animal_id = selectedAnimalId },
                    null,
                    Array.Empty<AiDisambiguationCandidate>(),
                    0,
                    "Животное не найдено в вашей организации.")
                : new AiAnimalResolutionResponse(
                    SchemaVersion,
                    "animal",
                    AiReadResolutionState.Resolved,
                    new { kind = "animal", animal_id = selectedAnimalId },
                    new AiResolvedEntity(animal.Id.ToString(), BuildAnimalDisplay(animal), animal.TagNumber),
                    Array.Empty<AiDisambiguationCandidate>(),
                    1,
                    $"Нашла животное: {BuildAnimalDisplay(animal)}.");
        }
        else
        {
            var tag = GetRequiredString(toolCall, "tag");
            if (tag.Error != null) return tag.Error;

            resolution = ResolveAnimalByTag(organizationId, tag.Value!, includeInactive: true);
            animal = resolution.State == AiReadResolutionState.Resolved && resolution.Resolved != null && Guid.TryParse(resolution.Resolved.Id, out var resolvedId)
                ? _dataSource.GetAnimalById(organizationId, resolvedId)
                : null;
        }

        if (resolution.State != AiReadResolutionState.Resolved || resolution.Resolved == null)
        {
            var unresolvedResponse = new AiAnimalParentsResponse(SchemaVersion, resolution, null, null, Array.Empty<AiParentAnimal>(), resolution.Message, resolution.Message);
            return TerminalOk(toolCall.Name, unresolvedResponse.Voice, unresolvedResponse);
        }

        if (animal == null)
        {
            var missingResponse = new AiAnimalParentsResponse(SchemaVersion, resolution, null, null, Array.Empty<AiParentAnimal>(), "Животное не найдено в организации.", "Животное не найдено в организации.");
            return TerminalOk(toolCall.Name, missingResponse.Voice, missingResponse);
        }

        var mother = animal.MotherId.HasValue
            ? _dataSource.GetAnimalById(organizationId, animal.MotherId.Value)
            : null;

        var fathers = animal.FatherIds
            .Select(id => _dataSource.GetAnimalById(organizationId, id))
            .Where(a => a != null)
            .Select(a => ToParentAnimal(a!))
            .ToList();

        var motherParent = mother != null ? ToParentAnimal(mother) : null;
        var animalParent = ToParentAnimal(animal);
        var motherText = motherParent?.Tag ?? "не указана";
        var fatherText = fathers.Count == 0 ? "не указан" : string.Join(", ", fathers.Select(f => f.Tag));
        var text = $"У животного {animal.TagNumber} мать: {motherText}; отец: {fatherText}.";

        var response = new AiAnimalParentsResponse(SchemaVersion, resolution, animalParent, motherParent, fathers, text, text);
        return TerminalOk(toolCall.Name, response.Voice, response);
    }

    private AiAgentToolResult GetWeightHistory(Guid organizationId, AiAgentToolCall toolCall)
    {
        var animalId = GetAnimalIdFromTagOrId(organizationId, toolCall);
        if (animalId.Error != null) return animalId.Error;

        var dateFrom = GetDate(toolCall, "date_from");
        if (dateFrom.Error != null) return dateFrom.Error;

        var dateTo = GetDate(toolCall, "date_to");
        if (dateTo.Error != null) return dateTo.Error;

        var limit = Math.Clamp(GetInt(toolCall, "limit") ?? 20, 1, 100);
        var weights = _dataSource.GetWeightHistory(organizationId, animalId.Value, dateFrom.Value, dateTo.Value, limit)
            .Select(w => new AiWeightPoint(w.Id, w.Date, w.Weight, w.Age, w.Sup, w.Method))
            .ToList();

        var voice = weights.Count switch
        {
            0 => "История веса не найдена.",
            1 => $"Найдена одна запись веса: {weights[0].Weight} кг от {weights[0].Date}.",
            _ => $"Найдено записей веса: {weights.Count}. Последний вес {weights[0].Weight} кг от {weights[0].Date}."
        };

        var response = new AiWeightHistoryResponse(SchemaVersion, animalId.Value, weights, voice, voice);
        return TerminalOk(toolCall.Name, response.Voice, response);
    }

    private AiAgentToolResult GetPregnanciesToCheck(Guid organizationId, AiAgentToolCall toolCall)
    {
        var dueBefore = GetDate(toolCall, "due_before");
        if (dueBefore.Error != null) return dueBefore.Error;

        var pregnancies = _dataSource.GetPregnanciesToCheck(organizationId, dueBefore.Value);

        var voice = pregnancies.Count == 0
            ? "Коров для диагностики стельности не найдено."
            : $"Для диагностики стельности найдено: {pregnancies.Count}.";

        var response = new AiPregnanciesToCheckResponse(SchemaVersion, pregnancies, voice, voice);
        return TerminalOk(toolCall.Name, response.Voice, response);
    }

    private AiAgentToolResult ListGroups(Guid organizationId, AiAgentToolCall toolCall)
    {
        var includeEmpty = GetBoolean(toolCall, "include_empty") ?? true;
        var groups = _dataSource.GetGroups(organizationId, includeEmpty)
            .Select(g => new AiGroupItem(g.Id, g.Name, g.TypeId, g.TypeName, g.Location, g.Description, g.AnimalCount))
            .ToList();

        var voice = groups.Count == 0
            ? "Группы не найдены."
            : $"Найдено групп: {groups.Count}.";

        var response = new AiGroupsResponse(SchemaVersion, groups, voice, voice);
        return TerminalOk(toolCall.Name, response.Voice, response);
    }

    private (Guid Value, AiAgentToolResult? Error) GetAnimalIdFromTagOrId(Guid organizationId, AiAgentToolCall toolCall)
    {
        if (TryGetSelectedAnimalId(toolCall, out var animalId))
        {
            return (animalId, null);
        }

        var tag = GetRequiredString(toolCall, "tag");
        if (tag.Error != null) return (Guid.Empty, tag.Error);

        var resolution = ResolveAnimalByTag(organizationId, tag.Value!, includeInactive: true);
        if (resolution.State != AiReadResolutionState.Resolved || resolution.Resolved == null)
        {
            return (Guid.Empty, TerminalOk(toolCall.Name, resolution.Message, resolution));
        }

        return Guid.TryParse(resolution.Resolved.Id, out var resolvedId)
            ? (resolvedId, null)
            : (Guid.Empty, AiAgentToolResult.Fail(
                toolCall.Name,
                AiAgentError.Create("AI_ENTITY_RESOLUTION_INVALID_ID", "Resolver вернул некорректный id животного.")));
    }

    private static bool TryGetSelectedAnimalId(AiAgentToolCall toolCall, out Guid animalId)
    {
        animalId = Guid.Empty;
        return TryGetProperty(toolCall, "animal_id", out var animalIdProperty) &&
               animalIdProperty.ValueKind == JsonValueKind.String &&
               Guid.TryParse(animalIdProperty.GetString(), out animalId);
    }

    private AiAnimalResolutionResponse ResolveAnimalByTag(Guid organizationId, string tag, bool includeInactive)
    {
        var matches = _dataSource.FindAnimalsByExactTag(organizationId, tag, includeInactive);
        var input = new { kind = "animal", tag };

        if (matches.Count == 0)
        {
            var matchedTags = AiTagMatcher.FindCandidates(
                tag,
                _dataSource.GetAnimalTags(organizationId, includeInactive));
            if (matchedTags.Count > 0)
            {
                var candidateMatches = matchedTags
                    .SelectMany(candidateTag => _dataSource.FindAnimalsByExactTag(organizationId, candidateTag, includeInactive))
                    .ToList();

                if (candidateMatches.Count == 1)
                {
                    var animal = candidateMatches[0];
                    return new AiAnimalResolutionResponse(
                        SchemaVersion,
                        "animal",
                        AiReadResolutionState.Resolved,
                        input,
                        new AiResolvedEntity(animal.Id.ToString(), BuildAnimalDisplay(animal), animal.TagNumber),
                        Array.Empty<AiDisambiguationCandidate>(),
                        1,
                        $"Нашла животное с биркой {animal.TagNumber}: {BuildAnimalDisplay(animal)}.");
                }

                if (candidateMatches.Count > 1)
                {
                    return new AiAnimalResolutionResponse(
                        SchemaVersion,
                        "animal",
                        AiReadResolutionState.Ambiguous,
                        input,
                        null,
                        candidateMatches.Take(5).Select(ToDisambiguationCandidate).ToList(),
                        candidateMatches.Count,
                        $"По фразе «{tag}» нашлось несколько похожих бирок. Выберите нужное животное из списка.");
                }
            }

            return new AiAnimalResolutionResponse(
                SchemaVersion,
                "animal",
                AiReadResolutionState.NotFound,
                input,
                null,
                Array.Empty<AiDisambiguationCandidate>(),
                0,
                $"Животное с биркой {tag} не найдено в вашей организации.");
        }

        if (matches.Count == 1)
        {
            var animal = matches[0];
            return new AiAnimalResolutionResponse(
                SchemaVersion,
                "animal",
                AiReadResolutionState.Resolved,
                input,
                new AiResolvedEntity(animal.Id.ToString(), BuildAnimalDisplay(animal), animal.TagNumber),
                Array.Empty<AiDisambiguationCandidate>(),
                1,
                $"Нашла животное с биркой {tag}: {BuildAnimalDisplay(animal)}.");
        }

        var candidates = matches
            .Take(5)
            .Select(ToDisambiguationCandidate)
            .ToList();

        var message = matches.Count > 5
            ? $"Нашла больше пяти животных с биркой {tag}. Показываю первые пять вариантов, выберите нужное животное."
            : $"Нашла {matches.Count} животных с биркой {tag}. Выберите нужное животное из списка.";

        return new AiAnimalResolutionResponse(
            SchemaVersion,
            "animal",
            AiReadResolutionState.Ambiguous,
            input,
            null,
            candidates,
            matches.Count,
            message);
    }

    private static AiDisambiguationCandidate ToDisambiguationCandidate(AiAnimalReadRecord animal)
        => new(
            animal.Id.ToString(),
            BuildAnimalDisplay(animal),
            new AiAnimalCandidate(
                animal.TagNumber,
                animal.Type,
                animal.Status,
                animal.BirthDate,
                animal.GroupName,
                animal.Breed,
                animal.Identifiers.Select(i => new AiIdentifierValue(i.Name, i.Value)).ToList()));

    private static AiAnimalCard ToAnimalCard(AiAnimalReadRecord animal)
        => new(
            animal.Id,
            animal.TagNumber,
            animal.Type,
            animal.Status,
            animal.BirthDate,
            animal.GroupName,
            animal.Breed,
            animal.Origin,
            animal.OriginLocation,
            animal.DateOfReceipt,
            animal.DateOfDisposal,
            animal.ReasonOfDisposal,
            animal.Identifiers.Select(i => new AiIdentifierValue(i.Name, i.Value)).ToList());

    private static AiParentAnimal ToParentAnimal(AiAnimalReadRecord animal)
        => new(animal.Id, animal.TagNumber, animal.Type, animal.BirthDate, animal.Status);

    private static string BuildAnimalDisplay(AiAnimalReadRecord animal)
    {
        var parts = new List<string> { $"бирка {animal.TagNumber}" };
        if (!string.IsNullOrWhiteSpace(animal.Type)) parts.Add(animal.Type);
        if (animal.BirthDate.HasValue) parts.Add($"рожд. {animal.BirthDate}");
        if (!string.IsNullOrWhiteSpace(animal.GroupName)) parts.Add($"группа {animal.GroupName}");
        if (!string.IsNullOrWhiteSpace(animal.Status)) parts.Add($"статус {animal.Status}");
        return string.Join(", ", parts);
    }

    private static AiAgentToolResult TerminalOk(string toolName, string summary, object data)
        => AiAgentToolResult.Ok(toolName, summary, JsonSerializer.SerializeToElement(data, JsonOptions), isTerminal: true);

    private static (string? Value, AiAgentToolResult? Error) GetRequiredString(AiAgentToolCall toolCall, string propertyName)
    {
        if (!TryGetProperty(toolCall, propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            if (propertyName == "tag" && TryGetTagFallback(toolCall, out var fallbackTag))
                return (fallbackTag, null);

            return (null, RequiredError(toolCall.Name, propertyName, "строка"));
        }

        var value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            if (propertyName == "tag" && TryGetTagFallback(toolCall, out var fallbackTag))
                return (fallbackTag, null);

            return (null, RequiredError(toolCall.Name, propertyName, "непустая строка"));
        }

        var normalized = propertyName == "tag"
            ? AiEntityNormalizer.NormalizeAnimalTag(value)
            : value.Trim();

        return (normalized, null);
    }

    private static bool TryGetTagFallback(AiAgentToolCall toolCall, out string tag)
    {
        tag = string.Empty;

        if (!TryGetProperty(toolCall, "animal_id", out var animalIdProperty))
            return false;

        if (animalIdProperty.ValueKind == JsonValueKind.Number)
        {
            tag = animalIdProperty.GetRawText().Trim();
            return tag.Length > 0;
        }

        if (animalIdProperty.ValueKind != JsonValueKind.String)
            return false;

        var value = animalIdProperty.GetString();
        if (string.IsNullOrWhiteSpace(value) || Guid.TryParse(value, out _))
            return false;

        tag = AiEntityNormalizer.NormalizeAnimalTag(value) ?? value.Trim();
        return !string.IsNullOrWhiteSpace(tag);
    }

    private static (Guid Value, AiAgentToolResult? Error) GetRequiredGuid(AiAgentToolCall toolCall, string propertyName)
    {
        if (!TryGetProperty(toolCall, propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            !Guid.TryParse(property.GetString(), out var value))
        {
            return (Guid.Empty, RequiredError(toolCall.Name, propertyName, "uuid"));
        }

        return (value, null);
    }

    private static (DateOnly? Value, AiAgentToolResult? Error) GetDate(AiAgentToolCall toolCall, string propertyName)
    {
        if (!TryGetProperty(toolCall, propertyName, out var property) || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return (null, null);

        if (property.ValueKind == JsonValueKind.String && DateOnly.TryParse(property.GetString(), out var value))
            return (value, null);

        return (null, RequiredError(toolCall.Name, propertyName, "date yyyy-MM-dd"));
    }

    private static bool? GetBoolean(AiAgentToolCall toolCall, string propertyName)
    {
        if (!TryGetProperty(toolCall, propertyName, out var property))
            return null;

        return property.ValueKind == JsonValueKind.True
            ? true
            : property.ValueKind == JsonValueKind.False
                ? false
                : null;
    }

    private static int? GetInt(AiAgentToolCall toolCall, string propertyName)
    {
        if (!TryGetProperty(toolCall, propertyName, out var property))
            return null;

        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static bool TryGetProperty(AiAgentToolCall toolCall, string propertyName, out JsonElement property)
    {
        property = default;
        return toolCall.Arguments.HasValue &&
               toolCall.Arguments.Value.ValueKind == JsonValueKind.Object &&
               toolCall.Arguments.Value.TryGetProperty(propertyName, out property);
    }

    private static AiAgentToolResult RequiredError(string toolName, string propertyName, string expected)
        => AiAgentToolResult.Fail(
            toolName,
            AiAgentError.Create(
                "AI_TOOL_ARGUMENT_INVALID",
                $"Поле {propertyName} обязательно, ожидается {expected}.",
                path: $"$.{propertyName}"));
}
