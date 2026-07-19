namespace CAT.Services.Ai;

public static class AiReadResolutionState
{
    public const string Resolved = "resolved";
    public const string Ambiguous = "ambiguous";
    public const string NotFound = "not_found";
}

public sealed record AiAnimalResolutionResponse(
    string SchemaVersion,
    string Entity,
    string State,
    object Input,
    AiResolvedEntity? Resolved,
    IReadOnlyList<AiDisambiguationCandidate> Candidates,
    int TotalMatches,
    string Message);

public sealed record AiResolvedEntity(string Id, string Display, string? NormalizedValue = null);

public sealed record AiDisambiguationCandidate(
    string Id,
    string Display,
    AiAnimalCandidate Animal);

public sealed record AiAnimalCandidate(
    string? Tag,
    string? Type,
    string? Status,
    DateOnly? BirthDate,
    string? GroupName,
    string? Breed,
    IReadOnlyList<AiIdentifierValue> Identifiers);

public sealed record AiIdentifierValue(string Name, string Value);

public sealed record AiAnimalCardResponse(
    string SchemaVersion,
    AiAnimalCard Animal,
    string Text,
    string Voice);

public sealed record AiAnimalCard(
    Guid Id,
    string? Tag,
    string? Type,
    string? Status,
    DateOnly? BirthDate,
    string? GroupName,
    string? Breed,
    string? Origin,
    string? OriginLocation,
    DateOnly? DateOfReceipt,
    DateOnly? DateOfDisposal,
    string? ReasonOfDisposal,
    IReadOnlyList<AiIdentifierValue> Identifiers);

public sealed record AiAnimalParentsResponse(
    string SchemaVersion,
    AiAnimalResolutionResponse Resolution,
    AiParentAnimal? Animal,
    AiParentAnimal? Mother,
    IReadOnlyList<AiParentAnimal> Fathers,
    string Text,
    string Voice);

public sealed record AiParentAnimal(
    Guid Id,
    string? Tag,
    string? Type,
    DateOnly? BirthDate,
    string? Status);

public sealed record AiWeightHistoryResponse(
    string SchemaVersion,
    Guid AnimalId,
    IReadOnlyList<AiWeightPoint> Items,
    string Text,
    string Voice);

public sealed record AiWeightPoint(
    Guid Id,
    DateOnly? Date,
    double? Weight,
    int? Age,
    double? Sup,
    string? Method);

public sealed record AiPregnanciesToCheckResponse(
    string SchemaVersion,
    IReadOnlyList<AiPregnancyToCheck> Items,
    string Text,
    string Voice);

public sealed record AiPregnancyToCheck(
    Guid CowId,
    string? CowTag,
    string? Status,
    string? InseminationType,
    DateOnly? InseminationDate,
    Guid? BullId,
    string? BullTag);

public sealed record AiGroupsResponse(
    string SchemaVersion,
    IReadOnlyList<AiGroupItem> Items,
    string Text,
    string Voice);

public sealed record AiGroupItem(
    Guid Id,
    string Name,
    Guid? TypeId,
    string? TypeName,
    string? Location,
    string? Description,
    int AnimalCount);

