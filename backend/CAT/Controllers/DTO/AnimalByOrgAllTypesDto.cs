using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CAT.EF.DAL;

namespace CAT.Controllers.DTO;

public sealed class AnimalByOrgAllTypesDto
{
    public Guid Id { get; init; }
    public string TagNumber { get; init; } = null!;

    public string? Type { get; init; }                
    public DateOnly? BirthDate { get; init; }
    public string? Breed { get; init; }
    public string? GroupName { get; init; }
    public string? Status { get; init; }
    public string? Origin { get; init; }
    public string? OriginLocation { get; init; }

    public string? MotherTagNumber { get; init; }
    public JsonNode? FatherTagNumbers { get; init; }

    public DateOnly? LastVaccinationDate { get; init; }

    public IdentificationFieldNameDTO[] IdentificationFields { get; set; }
    
    [JsonIgnore]
    public List<string> FatherTagNumbersList { get; init; }

    public static List<AnimalByOrgAllTypesDto> Parse(IEnumerable<IGrouping<Guid, AnimalCensus>> census)
    {
        return census.Select(g =>
        {
            var e = g.First();

            return new AnimalByOrgAllTypesDto
            {
                Id = e.Id,
                TagNumber = e.TagNumber,
                BirthDate = e.BirthDate,
                Breed = e.Breed,
                Type = e.Type,
                GroupName = e.GroupName,
                Status = e.Status,
                Origin = e.Origin,
                OriginLocation = e.OriginLocation,
                MotherTagNumber = e.MotherTagNumber,
                FatherTagNumbers = new JsonArray(
                    e.FatherTagNumbersList.Select(s => JsonValue.Create(s)).ToArray<JsonNode?>()),
                LastVaccinationDate = e.LastVaccinationDate,
                IdentificationFields = IdentificationFieldNameDTO.FromDictionary(e.IdentFields),
                FatherTagNumbersList = e.FatherTagNumbersList
            };
        }).ToList();
    }
}
