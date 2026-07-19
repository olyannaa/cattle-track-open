using CAT.Controllers.DTO;
using CAT.Services.Ai;
using Xunit;

namespace CAT.Tests.Ai;

public sealed class AiToolValidatorTests
{
    private static readonly Guid OrgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid AnimalId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BullId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid GroupId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateOnly Today = new(2026, 7, 9);

    [Fact]
    public void ValidateCreateWeight_AllRulesPass_ReturnsPreview()
    {
        var validator = CreateValidator();

        var result = validator.ValidateCreateWeight(OrgId, new WeightCreateDTO
        {
            AnimalId = AnimalId,
            Date = Today,
            Weight = 420,
            Method = "Ручное взвешивание"
        });

        Assert.True(result.IsValid);
        Assert.Equal(AiValidationRetryAction.ShowPreview, result.RetryAction);
    }

    [Fact]
    public void ValidateCreateWeight_InvalidWeightFutureDateDuplicateAndUnknownMethod_ReturnsErrors()
    {
        var dataSource = CreateDataSource();
        dataSource.ExistingWeights.Add((AnimalId, Today.AddDays(1)));
        var validator = CreateValidator(dataSource);

        var result = validator.ValidateCreateWeight(OrgId, new WeightCreateDTO
        {
            AnimalId = AnimalId,
            Date = Today.AddDays(1),
            Weight = 0,
            Method = "__unknown"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.WeightRange);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.DateNotFuture);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.EnumKnown);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.DuplicateWeight);
        Assert.Equal(AiValidationRetryAction.RetryLlmOnce, result.RetryAction);
    }

    [Fact]
    public void ValidateCreateWeight_AfterRetry_ReturnsHumanError()
    {
        var validator = CreateValidator();

        var result = validator.ValidateCreateWeight(OrgId, new WeightCreateDTO
        {
            AnimalId = AnimalId,
            Date = Today.AddDays(1),
            Weight = 420,
            Method = "Ручное взвешивание"
        }, retryAttempt: 1);

        Assert.False(result.IsValid);
        Assert.Equal(AiValidationRetryAction.ShowHumanError, result.RetryAction);
    }

    [Fact]
    public void ValidateCreateWeight_BeforeBirth_ReturnsDateBirthError()
    {
        var validator = CreateValidator();

        var result = validator.ValidateCreateWeight(OrgId, new WeightCreateDTO
        {
            AnimalId = AnimalId,
            Date = new DateOnly(2024, 12, 31),
            Weight = 420,
            Method = "Ручное взвешивание"
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.DateAfterBirth);
    }

    [Fact]
    public void ValidateCreateDailyActions_MoveRequiresOrgScopedNewGroup()
    {
        var dataSource = CreateDataSource();
        dataSource.Groups.Remove(GroupId);
        var validator = CreateValidator(dataSource);

        var result = validator.ValidateCreateDailyActions(OrgId, new[]
        {
            new CreateDailyActionDTO
            {
                AnimalId = AnimalId,
                Type = "Перевод",
                Date = Today,
                NewGroupId = GroupId
            }
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.OrgGroupScope);
    }

    [Fact]
    public void ValidateCreateDailyActions_CascadeFieldsAreRequired()
    {
        var validator = CreateValidator();

        var result = validator.ValidateCreateDailyActions(OrgId, new[]
        {
            new CreateDailyActionDTO
            {
                AnimalId = AnimalId,
                Type = "Присвоение номеров",
                Date = Today
            }
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.DailyActionCascade && e.Path.EndsWith(".subtype"));
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.DailyActionCascade && e.Path.EndsWith(".identificationValue"));
    }

    [Fact]
    public void ValidateCreateDailyActions_DuplicateIsNonRetryableButOtherErrorsStillAllowOneLlmRetry()
    {
        var dataSource = CreateDataSource();
        dataSource.ExistingDailyActions.Add((AnimalId, "Лечение", Today.AddDays(1), "Лечение"));
        var validator = CreateValidator(dataSource);

        var result = validator.ValidateCreateDailyActions(OrgId, new[]
        {
            new CreateDailyActionDTO
            {
                AnimalId = AnimalId,
                Type = "Лечение",
                Subtype = "Лечение",
                Date = Today.AddDays(1)
            }
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.DuplicateDailyAction);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.DateNotFuture);
        Assert.Equal(AiValidationRetryAction.RetryLlmOnce, result.RetryAction);
    }

    [Fact]
    public void ValidateCreateInsemination_NaturalRequiresBullAndChecksDuplicate()
    {
        var dataSource = CreateDataSource();
        dataSource.ExistingInseminations.Add((AnimalId, Today, "Естественное"));
        var validator = CreateValidator(dataSource);

        var result = validator.ValidateCreateInsemination(OrgId, new InseminationBatchDTO
        {
            Items = new List<InseminationItemDTO>
            {
                new()
                {
                    CowIds = new List<Guid> { AnimalId },
                    Date = Today,
                    InseminationType = "Естественное"
                }
            }
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.DailyActionCascade && e.Path.EndsWith(".bullIds"));
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.DuplicateInsemination);
    }

    [Fact]
    public void ValidateCreateInsemination_WhenDateIsDefault_RequiresDate()
    {
        var validator = CreateValidator();

        var result = validator.ValidateCreateInsemination(OrgId, new InseminationBatchDTO
        {
            Items = new List<InseminationItemDTO>
            {
                new()
                {
                    CowIds = new List<Guid> { AnimalId },
                    BullIds = new List<Guid> { BullId },
                    Date = default,
                    InseminationType = "Естественное"
                }
            }
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.RequiredField && e.Message == "Дата обязательна.");
        Assert.DoesNotContain(result.Errors, e => e.RuleId == AiValidationRules.DateAfterBirth);
    }

    [Fact]
    public void NormalizeAnimalTag_CompactsSpokenDigitSequence()
    {
        Assert.Equal("10015", AiEntityNormalizer.NormalizeAnimalTag("1.00. 15"));
        Assert.Equal("10015", AiEntityNormalizer.NormalizeAnimalTag("1. 0. 0. 15"));
    }

    [Fact]
    public void ValidateCreateInsemination_WithCowAndBullInOrg_IsValid()
    {
        var validator = CreateValidator();

        var result = validator.ValidateCreateInsemination(OrgId, new InseminationBatchDTO
        {
            Items = new List<InseminationItemDTO>
            {
                new()
                {
                    CowIds = new List<Guid> { AnimalId },
                    BullIds = new List<Guid> { BullId },
                    Date = Today,
                    InseminationType = "Естественное"
                }
            }
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateFeedingPercentages_RequiresHundredPercentTotal()
    {
        var validator = CreateValidator();

        var result = validator.ValidateFeedingPercentages(new[]
        {
            new AiFeedingPercentage("morning", 40),
            new AiFeedingPercentage("day", 40),
            new AiFeedingPercentage("night", 10)
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.RuleId == AiValidationRules.FeedingPercentSum);
    }

    [Fact]
    public void ValidateFeedingPercentages_HundredPercentTotal_IsValid()
    {
        var validator = CreateValidator();

        var result = validator.ValidateFeedingPercentages(new[]
        {
            new AiFeedingPercentage("morning", 40),
            new AiFeedingPercentage("day", 40),
            new AiFeedingPercentage("night", 20)
        });

        Assert.True(result.IsValid);
    }

    private static AiToolValidator CreateValidator(FakeAiToolValidationDataSource? dataSource = null)
        => new(dataSource ?? CreateDataSource(), () => Today);

    private static FakeAiToolValidationDataSource CreateDataSource()
    {
        var dataSource = new FakeAiToolValidationDataSource();
        dataSource.Animals[AnimalId] = new AiAnimalFacts(AnimalId, new DateOnly(2025, 1, 1), "Корова", "Активное");
        dataSource.Animals[BullId] = new AiAnimalFacts(BullId, new DateOnly(2024, 1, 1), "Бык", "Активное");
        dataSource.Groups.Add(GroupId);
        return dataSource;
    }

    private sealed class FakeAiToolValidationDataSource : IAiToolValidationDataSource
    {
        public Dictionary<Guid, AiAnimalFacts> Animals { get; } = new();
        public HashSet<Guid> Groups { get; } = new();
        public HashSet<(Guid AnimalId, DateOnly Date)> ExistingWeights { get; } = new();
        public HashSet<(Guid AnimalId, string? Type, DateOnly? Date, string? Subtype)> ExistingDailyActions { get; } = new();
        public HashSet<(Guid CowId, DateOnly Date, string InseminationType)> ExistingInseminations { get; } = new();

        public AiAnimalFacts? GetAnimalFacts(Guid organizationId, Guid? animalId)
            => animalId.HasValue && Animals.TryGetValue(animalId.Value, out var facts) ? facts : null;

        public bool IsGroupInOrganization(Guid organizationId, Guid? groupId)
            => groupId.HasValue && Groups.Contains(groupId.Value);

        public bool WeightExists(Guid organizationId, Guid animalId, DateOnly date)
            => ExistingWeights.Contains((animalId, date));

        public bool DailyActionExists(Guid organizationId, Guid animalId, string? type, DateOnly? date, string? subtype)
            => ExistingDailyActions.Contains((animalId, type, date, subtype));

        public bool InseminationExists(Guid organizationId, Guid cowId, DateOnly date, string inseminationType)
            => ExistingInseminations.Contains((cowId, date, inseminationType));
    }
}
