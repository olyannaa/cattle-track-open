using System.Text.Json;
using CAT.Controllers.DTO.AiAssistant;
using CAT.EF;
using CAT.Services.Ai;
using Xunit;

namespace CAT.Tests.Ai;

public sealed class AiReadToolExecutorTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AnimalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SecondAnimalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbc");
    private static readonly Guid MotherId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid FatherId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public async Task FindAnimal_WhenSingleExactTag_ReturnsResolved()
    {
        var executor = CreateExecutor(new[]
        {
            Animal(AnimalId, "523", "Корова", birthDate: new DateOnly(2024, 1, 2))
        });

        var result = await executor.ExecuteAsync(OrgId, ToolCall(AiAssistantToolNames.FindAnimal, new { schema_version = "v1", tag = "523" }));

        Assert.True(result.Success);
        Assert.True(result.IsTerminal);
        Assert.Equal(AiAssistantToolNames.FindAnimal, result.ToolName);
        Assert.Equal("resolved", result.Data!.Value.GetProperty("state").GetString());
    }

    [Fact]
    public async Task FindAnimal_WhenDuplicateTags_ReturnsAmbiguousCandidates()
    {
        var executor = CreateExecutor(new[]
        {
            Animal(AnimalId, "523", "Корова", birthDate: new DateOnly(2024, 1, 2), groupName: "Дойные"),
            Animal(SecondAnimalId, "523", "Бык", birthDate: new DateOnly(2023, 5, 1), groupName: "Быки")
        });

        var result = await executor.ExecuteAsync(OrgId, ToolCall(AiAssistantToolNames.FindAnimal, new { schema_version = "v1", tag = "523" }));

        Assert.True(result.Success);
        Assert.Equal("ambiguous", result.Data!.Value.GetProperty("state").GetString());
        Assert.Equal(2, result.Data.Value.GetProperty("totalMatches").GetInt32());
        Assert.Equal(2, result.Data.Value.GetProperty("candidates").GetArrayLength());
    }

    [Fact]
    public async Task FindAnimal_WhenMissing_ReturnsNotFound()
    {
        var executor = CreateExecutor(Array.Empty<AiAnimalReadRecord>());

        var result = await executor.ExecuteAsync(OrgId, ToolCall(AiAssistantToolNames.FindAnimal, new { schema_version = "v1", tag = "0000" }));

        Assert.True(result.Success);
        Assert.Equal("not_found", result.Data!.Value.GetProperty("state").GetString());
    }

    [Fact]
    public async Task FindAnimal_WhenOneZeroWasLikelyDropped_ReturnsNotFoundBecauseDbResolverIsExactOnly()
    {
        var executor = CreateExecutor(new[]
        {
            Animal(AnimalId, "10007", "Корова", birthDate: new DateOnly(2024, 1, 2))
        });

        var result = await executor.ExecuteAsync(OrgId, ToolCall(AiAssistantToolNames.FindAnimal, new { schema_version = "v1", tag = "1007" }));

        Assert.True(result.Success);
        Assert.Equal("not_found", result.Data!.Value.GetProperty("state").GetString());
        Assert.Equal(0, result.Data.Value.GetProperty("candidates").GetArrayLength());
    }

    [Fact]
    public async Task FindAnimal_WhenCyrillicTagSoundsLikeExistingLatinTag_UsesExistingOrganizationTag()
    {
        var executor = CreateExecutor(new[]
        {
            Animal(AnimalId, "TAG", "Корова", birthDate: new DateOnly(2024, 1, 2))
        });

        var result = await executor.ExecuteAsync(OrgId, ToolCall(AiAssistantToolNames.FindAnimal, new { schema_version = "v1", tag = "тег" }));

        Assert.True(result.Success);
        Assert.Equal("resolved", result.Data!.Value.GetProperty("state").GetString());
        Assert.Equal("TAG", result.Data.Value.GetProperty("resolved").GetProperty("normalizedValue").GetString());
    }

    [Fact]
    public async Task FindAnimal_WhenSpokenTextTagHasNoise_UsesExistingOrganizationTag()
    {
        var executor = CreateExecutor(new[]
        {
            Animal(AnimalId, "NewTagTest", "Корова", birthDate: new DateOnly(2024, 1, 2))
        });

        var result = await executor.ExecuteAsync(OrgId, ToolCall(AiAssistantToolNames.FindAnimal, new { schema_version = "v1", tag = "new tegt test" }));

        Assert.True(result.Success);
        Assert.Equal("resolved", result.Data!.Value.GetProperty("state").GetString());
        Assert.Equal("NewTagTest", result.Data.Value.GetProperty("resolved").GetProperty("normalizedValue").GetString());
    }

    [Fact]
    public async Task GetAnimalParents_ResolvesTagAndReturnsParentTags()
    {
        var executor = CreateExecutor(new[]
        {
            Animal(AnimalId, "523", "Корова", motherId: MotherId, fatherIds: new[] { FatherId }),
            Animal(MotherId, "100", "Корова"),
            Animal(FatherId, "200", "Бык")
        });

        var result = await executor.ExecuteAsync(OrgId, ToolCall(AiAssistantToolNames.GetAnimalParents, new { schema_version = "v1", tag = "523" }));

        Assert.True(result.Success);
        Assert.Contains("мать: 100", result.Summary);
        Assert.Contains("отец: 200", result.Summary);
        Assert.Equal("100", result.Data!.Value.GetProperty("mother").GetProperty("tag").GetString());
        Assert.Equal("200", result.Data.Value.GetProperty("fathers")[0].GetProperty("tag").GetString());
    }

    [Fact]
    public async Task ListGroups_ReturnsVoiceFriendlyCount()
    {
        var dataSource = new FakeReadToolDataSource();
        dataSource.Groups.Add(new AiGroupReadRecord(Guid.NewGuid(), "Дойные", null, "Производственная", "Корпус 1", null, 12));
        dataSource.Groups.Add(new AiGroupReadRecord(Guid.NewGuid(), "Сухостой", null, "Производственная", "Корпус 2", null, 0));
        var executor = new AiReadToolExecutor(dataSource, new FakeWriteToolService());

        var result = await executor.ExecuteAsync(OrgId, ToolCall(AiAssistantToolNames.ListGroups, new { schema_version = "v1", include_empty = true }));

        Assert.True(result.Success);
        Assert.Equal("Найдено групп: 2.", result.Summary);
        Assert.Equal(2, result.Data!.Value.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task ReadTool_WhenRequiredArgumentMissing_ReturnsUnifiedError()
    {
        var executor = CreateExecutor(Array.Empty<AiAnimalReadRecord>());

        var result = await executor.ExecuteAsync(OrgId, ToolCall(AiAssistantToolNames.FindAnimal, new { schema_version = "v1" }));

        Assert.False(result.Success);
        Assert.Equal("AI_TOOL_ARGUMENT_INVALID", result.Error?.Code);
        Assert.Equal("$.tag", result.Error?.Path);
    }

    private static AiReadToolExecutor CreateExecutor(IEnumerable<AiAnimalReadRecord> animals)
    {
        var dataSource = new FakeReadToolDataSource();
        dataSource.Animals.AddRange(animals);
        return new AiReadToolExecutor(dataSource, new FakeWriteToolService());
    }

    private static AiAgentToolCall ToolCall(string name, object args)
        => new()
        {
            Name = name,
            Arguments = JsonSerializer.SerializeToElement(args, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

    private static AiAnimalReadRecord Animal(
        Guid id,
        string tag,
        string type,
        DateOnly? birthDate = null,
        string? groupName = null,
        Guid? motherId = null,
        IReadOnlyList<Guid>? fatherIds = null)
        => new(
            id,
            OrgId,
            tag,
            type,
            null,
            "Активное",
            null,
            groupName,
            birthDate,
            motherId,
            fatherIds ?? Array.Empty<Guid>(),
            null,
            null,
            null,
            null,
            null,
            Array.Empty<AiIdentifierReadRecord>());

    private sealed class FakeReadToolDataSource : IAiReadToolDataSource
    {
        public List<AiAnimalReadRecord> Animals { get; } = new();
        public List<AiGroupReadRecord> Groups { get; } = new();
        public List<AiWeightReadRecord> Weights { get; } = new();
        public List<AiPregnancyToCheck> Pregnancies { get; } = new();

        public IReadOnlyList<AiAnimalReadRecord> FindAnimalsByExactTag(Guid organizationId, string tag, bool includeInactive = false)
            => Animals
                .Where(a => a.OrganizationId == organizationId && a.TagNumber == tag)
                .Where(a => includeInactive || a.Status == "Активное")
                .ToList();

        public AiAnimalReadRecord? GetAnimalById(Guid organizationId, Guid animalId)
            => Animals.FirstOrDefault(a => a.OrganizationId == organizationId && a.Id == animalId);

        public IReadOnlyList<string> GetAnimalTags(Guid organizationId, bool includeInactive = false)
            => Animals
                .Where(a => a.OrganizationId == organizationId)
                .Where(a => includeInactive || a.Status == "Активное")
                .Select(a => a.TagNumber!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        public IReadOnlyList<AiWeightReadRecord> GetWeightHistory(Guid organizationId, Guid animalId, DateOnly? dateFrom, DateOnly? dateTo, int limit)
            => Weights.Take(limit).ToList();

        public IReadOnlyList<AiPregnancyToCheck> GetPregnanciesToCheck(Guid organizationId, DateOnly? dueBefore)
            => Pregnancies.ToList();

        public IReadOnlyList<AiGroupReadRecord> GetGroups(Guid organizationId, bool includeEmpty)
            => Groups.Where(g => includeEmpty || g.AnimalCount > 0).ToList();
    }

    private sealed class FakeWriteToolService : IAiWriteToolService
    {
        public AiAgentToolResult CreatePreview(Guid organizationId, AiAgentToolCall toolCall)
            => AiAgentToolResult.Fail(toolCall.Name, AiAgentError.Create("unused", "unused"));

        public AiWriteDraftPayload SelectCandidate(Guid organizationId, AiWriteDraftPayload payload, int itemIndex, Guid candidateId)
            => throw new NotImplementedException();

        public AiWriteCommitReport Commit(Guid organizationId, Guid draftId, AiWriteDraftPayload payload)
            => throw new NotImplementedException();
    }
}
