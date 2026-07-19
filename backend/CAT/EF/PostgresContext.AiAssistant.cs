using System.Text.Json;
using CAT.EF.DAL;
using Microsoft.EntityFrameworkCore;

namespace CAT.EF;

public partial class PostgresContext
{
    public IReadOnlyList<AiAnimalReadRecord> FindAiAnimalsByExactTag(
        Guid organizationId,
        string tag,
        bool includeInactive = false)
    {
        var query = Animals
            .AsNoTracking()
            .Include(a => a.Group)
            .Include(a => a.AnimalIdentifications)
                .ThenInclude(i => i.Field)
            .Where(a => a.OrganizationId == organizationId && a.TagNumber == tag);

        if (!includeInactive)
            query = query.Where(a => a.Status == "Активное");

        return query
            .OrderBy(a => a.Status == "Активное" ? 0 : 1)
            .ThenBy(a => a.BirthDate)
            .ThenBy(a => a.Id)
            .ToList()
            .Select(ToAiAnimalReadRecord)
            .ToList();
    }

    public IReadOnlyList<string> GetAiAnimalTags(Guid organizationId, bool includeInactive = false)
    {
        var query = Animals
            .AsNoTracking()
            .Where(a => a.OrganizationId == organizationId && a.TagNumber != null && a.TagNumber != string.Empty);

        if (!includeInactive)
            query = query.Where(a => a.Status == "Активное");

        return query
            .Select(a => a.TagNumber!)
            .Distinct()
            .ToList();
    }

    public AiAnimalReadRecord? GetAiAnimalById(Guid organizationId, Guid animalId)
        => Animals
            .AsNoTracking()
            .Include(a => a.Group)
            .Include(a => a.AnimalIdentifications)
                .ThenInclude(i => i.Field)
            .Where(a => a.OrganizationId == organizationId && a.Id == animalId)
            .ToList()
            .Select(ToAiAnimalReadRecord)
            .FirstOrDefault();

    public IReadOnlyList<AiWeightReadRecord> GetAiWeightHistory(
        Guid organizationId,
        Guid animalId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        int limit)
    {
        var animalExists = Animals.AsNoTracking().Any(a => a.OrganizationId == organizationId && a.Id == animalId);
        if (!animalExists)
            return Array.Empty<AiWeightReadRecord>();

        var query = GetAnimalWeightInfo(animalId).AsNoTracking();

        if (dateFrom.HasValue)
            query = query.Where(w => w.Date >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(w => w.Date <= dateTo.Value);

        return query
            .OrderByDescending(w => w.Date)
            .Take(limit)
            .Select(w => new AiWeightReadRecord(
                w.Id,
                w.Date,
                w.Weight,
                w.Age,
                w.SUP,
                w.Method))
            .ToList();
    }

    public IReadOnlyList<AiGroupReadRecord> GetAiGroups(Guid organizationId, bool includeEmpty = true)
    {
        var groups = Groups
            .AsNoTracking()
            .Include(g => g.Type)
            .Where(g => g.OrganizationId == organizationId)
            .Select(g => new AiGroupReadRecord(
                g.Id,
                g.Name,
                g.TypeId,
                g.Type != null ? g.Type.Name : null,
                g.Location,
                g.Description,
                Animals.Count(a => a.OrganizationId == organizationId && a.GroupId == g.Id)))
            .ToList();

        if (!includeEmpty)
            groups = groups.Where(g => g.AnimalCount > 0).ToList();

        return groups
            .OrderBy(g => g.Name)
            .ToList();
    }

    private static AiAnimalReadRecord ToAiAnimalReadRecord(Animal animal)
        => new(
            animal.Id,
            animal.OrganizationId,
            animal.TagNumber,
            animal.Type,
            animal.Breed,
            animal.Status,
            animal.GroupId,
            animal.Group?.Name,
            animal.BirthDate,
            animal.MotherId,
            ExtractFatherIds(animal.FatherJson),
            animal.Origin,
            animal.OriginLocation,
            animal.DateOfReceipt,
            animal.DateOfDisposal,
            animal.ReasonOfDisposal,
            animal.AnimalIdentifications
                .Where(i => !string.IsNullOrWhiteSpace(i.Field?.FieldName) && !string.IsNullOrWhiteSpace(i.Value))
                .Select(i => new AiIdentifierReadRecord(i.Field!.FieldName!, i.Value!))
                .ToList());

    private static IReadOnlyList<Guid> ExtractFatherIds(JsonElement? fatherJson)
    {
        var result = new List<Guid>();
        if (fatherJson == null || fatherJson.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            return result;

        var root = fatherJson.Value;
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out var id))
                    result.Add(id);
            }
        }

        return result;
    }
}

public sealed record AiIdentifierReadRecord(string Name, string Value);

public sealed record AiAnimalReadRecord(
    Guid Id,
    Guid? OrganizationId,
    string? TagNumber,
    string? Type,
    string? Breed,
    string? Status,
    Guid? GroupId,
    string? GroupName,
    DateOnly? BirthDate,
    Guid? MotherId,
    IReadOnlyList<Guid> FatherIds,
    string? Origin,
    string? OriginLocation,
    DateOnly? DateOfReceipt,
    DateOnly? DateOfDisposal,
    string? ReasonOfDisposal,
    IReadOnlyList<AiIdentifierReadRecord> Identifiers);

public sealed record AiWeightReadRecord(
    Guid Id,
    DateOnly? Date,
    double? Weight,
    int? Age,
    double? Sup,
    string? Method);

public sealed record AiGroupReadRecord(
    Guid Id,
    string Name,
    Guid? TypeId,
    string? TypeName,
    string? Location,
    string? Description,
    int AnimalCount);
