using CAT.EF;
using CAT.Services.Interfaces;

namespace CAT.Services.Ai;

public interface IAiReadToolDataSource
{
    IReadOnlyList<AiAnimalReadRecord> FindAnimalsByExactTag(
        Guid organizationId,
        string tag,
        bool includeInactive = false);

    IReadOnlyList<string> GetAnimalTags(Guid organizationId, bool includeInactive = false);

    AiAnimalReadRecord? GetAnimalById(Guid organizationId, Guid animalId);

    IReadOnlyList<AiWeightReadRecord> GetWeightHistory(
        Guid organizationId,
        Guid animalId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int limit);

    IReadOnlyList<AiPregnancyToCheck> GetPregnanciesToCheck(Guid organizationId, DateOnly? dueBefore);

    IReadOnlyList<AiGroupReadRecord> GetGroups(Guid organizationId, bool includeEmpty);
}

public sealed class EfAiReadToolDataSource : IAiReadToolDataSource
{
    private readonly PostgresContext _db;
    private readonly IAnimalService _animalService;

    public EfAiReadToolDataSource(PostgresContext db, IAnimalService animalService)
    {
        _db = db;
        _animalService = animalService;
    }

    public IReadOnlyList<AiAnimalReadRecord> FindAnimalsByExactTag(
        Guid organizationId,
        string tag,
        bool includeInactive = false)
        => _db.FindAiAnimalsByExactTag(organizationId, tag, includeInactive);

    public IReadOnlyList<string> GetAnimalTags(Guid organizationId, bool includeInactive = false)
        => _db.GetAiAnimalTags(organizationId, includeInactive);

    public AiAnimalReadRecord? GetAnimalById(Guid organizationId, Guid animalId)
        => _db.GetAiAnimalById(organizationId, animalId);

    public IReadOnlyList<AiWeightReadRecord> GetWeightHistory(
        Guid organizationId,
        Guid animalId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int limit)
        => _db.GetAiWeightHistory(organizationId, animalId, dateFrom, dateTo, limit);

    public IReadOnlyList<AiPregnancyToCheck> GetPregnanciesToCheck(Guid organizationId, DateOnly? dueBefore)
        => _animalService.GetPregnanciesForInsert(organizationId)
            .Where(p => !dueBefore.HasValue || p.InseminationDate <= dueBefore.Value)
            .Select(p => new AiPregnancyToCheck(
                p.CowId,
                p.CowTagNumber,
                p.Status,
                p.InseminationType,
                p.InseminationDate,
                p.BullId,
                p.BullTagNumber))
            .ToList();

    public IReadOnlyList<AiGroupReadRecord> GetGroups(Guid organizationId, bool includeEmpty)
        => _db.GetAiGroups(organizationId, includeEmpty);
}
