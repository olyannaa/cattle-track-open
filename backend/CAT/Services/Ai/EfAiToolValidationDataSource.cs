using CAT.EF;
using Microsoft.EntityFrameworkCore;

namespace CAT.Services.Ai;

public sealed class EfAiToolValidationDataSource : IAiToolValidationDataSource
{
    private readonly PostgresContext _db;

    public EfAiToolValidationDataSource(PostgresContext db)
    {
        _db = db;
    }

    public AiAnimalFacts? GetAnimalFacts(Guid organizationId, Guid? animalId)
    {
        if (animalId == null) return null;

        return _db.Animals
            .Where(a => a.Id == animalId && a.OrganizationId == organizationId)
            .Select(a => new AiAnimalFacts(a.Id, a.BirthDate, a.Type, a.Status))
            .SingleOrDefault();
    }

    public bool IsGroupInOrganization(Guid organizationId, Guid? groupId)
    {
        if (groupId == null) return false;
        return _db.Groups.Any(g => g.Id == groupId && g.OrganizationId == organizationId);
    }

    public bool WeightExists(Guid organizationId, Guid animalId, DateOnly date)
    {
        if (!_db.Animals.Any(a => a.Id == animalId && a.OrganizationId == organizationId))
            return false;

        return _db.Weights
            .Any(w => w.AnimalId == animalId && w.Date == date);
    }

    public bool DailyActionExists(Guid organizationId, Guid animalId, string? type, DateOnly? date, string? subtype)
    {
        return _db.DailyActions
            .Include(a => a.Animal)
            .Any(a => a.AnimalId == animalId &&
                      a.Type == type &&
                      a.Date == date &&
                      a.Subtype == subtype &&
                      a.Animal != null &&
                      a.Animal.OrganizationId == organizationId);
    }

    public bool InseminationExists(Guid organizationId, Guid cowId, DateOnly date, string inseminationType)
    {
        var dateTime = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        return _db.Inseminations
            .Any(i => i.CowId == cowId &&
                      i.Date.HasValue &&
                      i.Date.Value.Date == dateTime.Date &&
                      i.Type == inseminationType &&
                      _db.Animals.Any(a => a.Id == i.CowId && a.OrganizationId == organizationId));
    }
}
