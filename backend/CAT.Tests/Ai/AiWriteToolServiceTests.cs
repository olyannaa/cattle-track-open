using System.Text.Json;
using CAT.Controllers.DTO;
using CAT.Controllers.DTO.AiAssistant;
using CAT.EF;
using CAT.EF.DAL;
using CAT.Models;
using CAT.Services.Ai;
using CAT.Services.Interfaces;
using Xunit;

namespace CAT.Tests.Ai;

public sealed class AiWriteToolServiceTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AnimalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid SecondAnimalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbc");
    private static readonly Guid GroupId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateOnly Today = new(2026, 7, 11);

    [Fact]
    public void CreateWeightPreview_ResolvedValidItem_CanCommit()
    {
        var service = CreateService(new[] { Animal(AnimalId, "523") });

        var result = service.CreatePreview(OrgId, ToolCall(AiAssistantToolNames.CreateWeight, new
        {
            schema_version = "v1",
            idempotency_key = "weight-1",
            tag = "523",
            weight = 420,
            date = "2026-07-10",
            method = "Ручное взвешивание"
        }));

        Assert.True(result.Success);
        Assert.True(result.CanCommit);
        var payload = result.Data!.Value.Deserialize<AiWriteDraftPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(1, payload.CommitReadyCount);
        Assert.Equal(AiWriteItemStatus.Resolved, payload.Items[0].Status);
    }

    [Fact]
    public void CreateWeightPreview_AmbiguousTag_DoesNotCommitAndKeepsCandidates()
    {
        var service = CreateService(new[]
        {
            Animal(AnimalId, "523"),
            Animal(SecondAnimalId, "523")
        });

        var result = service.CreatePreview(OrgId, ToolCall(AiAssistantToolNames.CreateWeight, new
        {
            schema_version = "v1",
            idempotency_key = "weight-1",
            tag = "523",
            weight = 420,
            date = "2026-07-10",
            method = "Ручное взвешивание"
        }));

        Assert.False(result.CanCommit);
        var payload = result.Data!.Value.Deserialize<AiWriteDraftPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(AiWriteItemStatus.Ambiguous, payload.Items[0].Status);
        Assert.Equal(2, payload.Items[0].Candidates.Count);
    }

    [Fact]
    public void CreateWeightPreview_WhenOneZeroWasLikelyDropped_ReturnsNotFoundBecauseDbResolverIsExactOnly()
    {
        var service = CreateService(new[]
        {
            Animal(AnimalId, "10007")
        });

        var result = service.CreatePreview(OrgId, ToolCall(AiAssistantToolNames.CreateWeight, new
        {
            schema_version = "v1",
            idempotency_key = "weight-1",
            tag = "1007",
            weight = 420,
            date = "2026-07-10",
            method = "Ручное взвешивание"
        }));

        Assert.False(result.CanCommit);
        var payload = result.Data!.Value.Deserialize<AiWriteDraftPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(AiWriteItemStatus.NotFound, payload.Items[0].Status);
        Assert.Empty(payload.Items[0].Candidates);
    }

    [Fact]
    public void CreateWeightPreview_WhenSpokenTextTagHasNoise_UsesExistingOrganizationTag()
    {
        var service = CreateService(new[]
        {
            Animal(AnimalId, "NewTagTest")
        });

        var result = service.CreatePreview(OrgId, ToolCall(AiAssistantToolNames.CreateWeight, new
        {
            schema_version = "v1",
            idempotency_key = "weight-1",
            tag = "new tegt test",
            weight = 420,
            date = "2026-07-10",
            method = "Ручное взвешивание"
        }));

        Assert.True(result.CanCommit);
        var payload = result.Data!.Value.Deserialize<AiWriteDraftPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(AiWriteItemStatus.Resolved, payload.Items[0].Status);
        Assert.Equal(AnimalId, payload.Items[0].Weight!.AnimalId);
    }

    [Fact]
    public void SelectCandidate_RebuildsAmbiguousWriteDraftWithoutCallingLlm()
    {
        var service = CreateService(new[]
        {
            Animal(AnimalId, "523"),
            Animal(SecondAnimalId, "523")
        });
        var source = ToolCall(AiAssistantToolNames.CreateWeight, new
        {
            schema_version = "v1",
            idempotency_key = "weight-1",
            tag = "523",
            weight = 420,
            date = "2026-07-10",
            method = "Ручное взвешивание"
        });
        var ambiguous = service.CreatePreview(OrgId, source).Data!.Value
            .Deserialize<AiWriteDraftPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var selected = service.SelectCandidate(OrgId, ambiguous, 0, SecondAnimalId);

        Assert.Equal(1, selected.CommitReadyCount);
        Assert.True(selected.Items[0].CanCommit);
        Assert.Equal(SecondAnimalId, selected.Items[0].Weight!.AnimalId);
        Assert.Equal(SecondAnimalId, selected.SelectedAnimalIds["weight-1"]);
    }

    [Fact]
    public void CreateDailyActionPreview_SplitsResolvedNotFoundAndInvalidItems()
    {
        var service = CreateService(new[] { Animal(AnimalId, "523") });

        var result = service.CreatePreview(OrgId, ToolCall(AiAssistantToolNames.CreateDailyAction, new
        {
            schema_version = "v1",
            batch_idempotency_key = "daily-batch",
            items = new object[]
            {
                new { idempotency_key = "daily-1", tag = "523", type = "Перевод", date = "2026-07-10", new_group_id = GroupId },
                new { idempotency_key = "daily-2", tag = "0000", type = "Осмотры", date = "2026-07-10" },
                new { idempotency_key = "daily-3", tag = "523", type = "Перевод", date = "2026-07-10" }
            }
        }));

        var payload = result.Data!.Value.Deserialize<AiWriteDraftPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(3, payload.Items.Count);
        Assert.Equal(1, payload.CommitReadyCount);
        Assert.Equal(AiWriteItemStatus.NotFound, payload.Items[1].Status);
        Assert.Equal(AiWriteItemStatus.Invalid, payload.Items[2].Status);
    }

    [Fact]
    public void CreateDailyActionPreview_TransferResolvesExistingGroupByName()
    {
        var service = CreateService(
            new[] { Animal(AnimalId, "523") },
            groups: new[] { new AiGroupReadRecord(GroupId, "Группа 345", null, null, null, null, 0) });

        var result = service.CreatePreview(OrgId, ToolCall(AiAssistantToolNames.CreateDailyAction, new
        {
            schema_version = "v1",
            batch_idempotency_key = "daily-batch",
            items = new object[]
            {
                new
                {
                    idempotency_key = "daily-1",
                    tag = "523",
                    type = "Перевод",
                    subtype = "Перевод",
                    date = "2026-07-10",
                    new_group_name = "345"
                }
            }
        }));

        Assert.True(result.Success);
        var payload = result.Data!.Value.Deserialize<AiWriteDraftPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(1, payload.CommitReadyCount);
        Assert.True(payload.Items[0].CanCommit);
        Assert.Equal(GroupId, payload.Items[0].DailyAction!.NewGroupId);
    }

    [Fact]
    public void Commit_CommitsReadyItemsAndSkipsInvalid()
    {
        var weights = new FakeWeightsService();
        var service = CreateService(new[] { Animal(AnimalId, "523") }, weightsService: weights);
        var preview = service.CreatePreview(OrgId, ToolCall(AiAssistantToolNames.CreateWeight, new
        {
            schema_version = "v1",
            idempotency_key = "weight-1",
            tag = "523",
            weight = 420,
            date = "2026-07-10",
            method = "Ручное взвешивание"
        }));
        var payload = preview.Data!.Value.Deserialize<AiWriteDraftPayload>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        payload.Items.Add(new AiWriteDraftItem
        {
            Index = 1,
            IdempotencyKey = "bad",
            Tag = "0000",
            Status = AiWriteItemStatus.NotFound,
            Message = "not found",
            CanCommit = false
        });

        var report = service.Commit(OrgId, Guid.NewGuid(), payload);

        Assert.Equal(1, report.Committed);
        Assert.Equal(1, report.Skipped);
        Assert.Single(weights.Inserted);
    }

    private static AiWriteToolService CreateService(
        IEnumerable<AiAnimalReadRecord> animals,
        FakeWeightsService? weightsService = null,
        IEnumerable<AiGroupReadRecord>? groups = null)
    {
        var readData = new FakeReadToolDataSource();
        readData.Animals.AddRange(animals);
        if (groups != null)
            readData.Groups.AddRange(groups);
        var validationData = new FakeValidationDataSource();
        foreach (var animal in animals)
            validationData.Animals[animal.Id] = new AiAnimalFacts(animal.Id, animal.BirthDate, animal.Type, animal.Status);
        validationData.Groups.Add(GroupId);

        return new AiWriteToolService(
            readData,
            new AiToolValidator(validationData, () => Today),
            weightsService ?? new FakeWeightsService(),
            new FakeDailyActionService(),
            new FakeAnimalService());
    }

    private static AiAgentToolCall ToolCall(string name, object args)
        => new()
        {
            Name = name,
            Arguments = JsonSerializer.SerializeToElement(args, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        };

    private static AiAnimalReadRecord Animal(Guid id, string tag)
        => new(id, OrgId, tag, "Корова", null, "Активное", null, null, new DateOnly(2025, 1, 1), null, Array.Empty<Guid>(), null, null, null, null, null, Array.Empty<AiIdentifierReadRecord>());

    private sealed class FakeReadToolDataSource : IAiReadToolDataSource
    {
        public List<AiAnimalReadRecord> Animals { get; } = new();
        public List<AiGroupReadRecord> Groups { get; } = new();
        public IReadOnlyList<AiAnimalReadRecord> FindAnimalsByExactTag(Guid organizationId, string tag, bool includeInactive = false)
            => Animals.Where(a => a.OrganizationId == organizationId && a.TagNumber == tag).ToList();
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
            => Array.Empty<AiWeightReadRecord>();
        public IReadOnlyList<AiPregnancyToCheck> GetPregnanciesToCheck(Guid organizationId, DateOnly? dueBefore)
            => Array.Empty<AiPregnancyToCheck>();
        public IReadOnlyList<AiGroupReadRecord> GetGroups(Guid organizationId, bool includeEmpty)
            => Groups.ToList();
    }

    private sealed class FakeValidationDataSource : IAiToolValidationDataSource
    {
        public Dictionary<Guid, AiAnimalFacts> Animals { get; } = new();
        public HashSet<Guid> Groups { get; } = new();
        public AiAnimalFacts? GetAnimalFacts(Guid organizationId, Guid? animalId)
            => animalId.HasValue && Animals.TryGetValue(animalId.Value, out var facts) ? facts : null;
        public bool IsGroupInOrganization(Guid organizationId, Guid? groupId)
            => groupId.HasValue && Groups.Contains(groupId.Value);
        public bool WeightExists(Guid organizationId, Guid animalId, DateOnly date) => false;
        public bool DailyActionExists(Guid organizationId, Guid animalId, string? type, DateOnly? date, string? subtype) => false;
        public bool InseminationExists(Guid organizationId, Guid cowId, DateOnly date, string inseminationType) => false;
    }

    private sealed class FakeWeightsService : IWeightsService
    {
        public List<WeightCreateDTO> Inserted { get; } = new();
        public IEnumerable<WeightInfoDTO> GetWeightsInfo(Guid animalId, WeightsSortInfoDTO? sort = default) => throw new NotImplementedException();
        public IEnumerable<WeightInfoDTO> GetWeightsInfoByPage(Guid animalId, WeightsSortInfoDTO? sort = default, int page = 1, bool isMoblile = default) => throw new NotImplementedException();
        public OkDTO InsertWeights(WeightCreateDTO weightInfo)
        {
            Inserted.Add(weightInfo);
            return new OkDTO("ok");
        }
        public double? ComputeSUP(Guid animalId, DateOnly date, double weight) => null;
        public WeightStatisticsDTO GetWeightsStatistics(Guid animalId) => throw new NotImplementedException();
    }

    private sealed class FakeDailyActionService : IDailyActionService
    {
        public IEnumerable<dynamic> GetDailyActions(Guid organizationId, string type, DailyActionsSortInfoDTO sort) => throw new NotImplementedException();
        public IEnumerable<dynamic> GetDailyActionsByPage(Guid organizationId, string type, DailyActionsSortInfoDTO sort, int page = 1, bool isMoblile = default) => throw new NotImplementedException();
        public void DeleteDailyAction(Guid dailyActionId) => throw new NotImplementedException();
        public void DeleteResearch(Guid researchId) => throw new NotImplementedException();
        public void CreateDailyAction(Guid organizationId, CreateDailyActionDTO dto) { }
        public void CreateDailyActionWithMedicine(Guid organizationId, Guid animalId, DailyActionMedicineItemDTO dto) => throw new NotImplementedException();
    }

    private sealed class FakeAnimalService : IAnimalService
    {
        public Guid RegisterAnimal(AnimalRegistrationDTO animal, Guid organizationId) => throw new NotImplementedException();
        public void UpdateAnimal(UpdateAnimalDTO updateInfo) => throw new NotImplementedException();
        public List<GroupInfoDTO>? GetGroupsInfo(Guid org_id) => throw new NotImplementedException();
        public List<IdentificationInfoDTO>? GetIdentificationsFields(Guid org_id) => throw new NotImplementedException();
        public ImportAnimalsInfo ImportAnimalsFromXLSX(List<AnimalInfoDTO> animals, Guid org_id) => throw new NotImplementedException();
        public IEnumerable<dynamic> GetAnimalCensus(Guid organisationId, string? animalType = default, string? search = default, CensusSortInfoDTO? sortInfo = default) => throw new NotImplementedException();
        public IEnumerable<dynamic> GetAnimalCensusByPage(Guid organisationId, string? animalType = default, string? search = default, CensusSortInfoDTO? sortInfo = default, int page = 1, bool isMobile = default) => throw new NotImplementedException();
        public IEnumerable<AnimalByOrgAllTypesDto> GetAnimalCensusWithFilters(Guid organizationId, AnimalFiltersDTO? filters = null, CensusSortInfoDTO? sortInfo = default) => throw new NotImplementedException();
        public int CountAnimalCensusWithFilters(Guid organizationId, AnimalFiltersDTO? filters = null, CensusSortInfoDTO? sortInfo = default) => throw new NotImplementedException();
        public IEnumerable<dynamic> GetAnimalCensusByPageWithFilters(Guid organizationId, AnimalFiltersDTO? filters = null, CensusSortInfoDTO? sortInfo = default, int page = 1, bool isMobile = default) => throw new NotImplementedException();
        public IEnumerable<ActiveAnimalDAL> GetAnimalsForDA(Guid organizationId, DailyAnimalsDTO filters, int? page = default, bool isMobile = default) => throw new NotImplementedException();
        public AnimalDTO? GetAnimalInfo(Guid organizationId, Guid animalId) => throw new NotImplementedException();
        public Dictionary<string, int> GetMainPageInfo(Guid organizationId) => throw new NotImplementedException();
        public IEnumerable<CowDTO> GetCows(Guid organizationId) => throw new NotImplementedException();
        public IEnumerable<BullDTO> GetBulls(Guid organizationId) => throw new NotImplementedException();
        public void InsertInsemination(InseminationDTO dto) => throw new NotImplementedException();
        public IReadOnlyList<Guid> InsertInseminations(IEnumerable<InseminationItemDTO> items) => items.Select(_ => Guid.NewGuid()).ToList();
        public IEnumerable<CowInseminationDTO> GetPregnanciesForInsert(Guid organizationId) => throw new NotImplementedException();
        public IEnumerable<CowInseminationDTO> GetPregnanciesForCalving(Guid organizationId) => throw new NotImplementedException();
        public void InsertPregnancy(InsertPregnancyDTO dto) => throw new NotImplementedException();
        public Guid InsertCalving(InsertCalvingDTO dto, Guid organizationId) => throw new NotImplementedException();
        public IEnumerable<BreedDTO> GetAllBreeds() => throw new NotImplementedException();
        public bool RemoveCowFromBarren(Guid animalId) => throw new NotImplementedException();
        public IEnumerable<AnimalReproductionDTO> GetAnimalReproductions(Guid organizationId) => throw new NotImplementedException();
        public void UpdatePregnancy(UpdatePregnancyDTO dto) => throw new NotImplementedException();
        public IEnumerable<AnimalByOrgAllTypesDto> GetAnimalCensusByPageWithIF(IEnumerable<AnimalByOrgAllTypesDto> census) => throw new NotImplementedException();
        public string[] GetPlaceOfOrigin(Guid organizationId) => throw new NotImplementedException();
        public string[] GetOrigins(Guid organizationId) => throw new NotImplementedException();
    }
}
