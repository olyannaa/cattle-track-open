using System.Text.Json;

namespace CAT.Controllers.DTO;

public class ExportAnimalCsvDTO
{
    public string? TagNumber { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string? Breed { get; set; }
    public string? GroupName { get; set; }
    public string? Status { get; set; }
    public string? Origin { get; set; }
    public string? OriginLocation { get; set; }
    public string? MotherTagNumber { get; set; }
    public string? FatherTagNumbers { get; set; }
    
    public DateOnly? LastVaccinationDate { get; set; }
    
    public string? IdentificationFields { get; set; }

    public ExportAnimalCsvDTO(string? tagNumber, DateOnly? birthDate, string? breed, string? groupName,
        string? status, string? origin, string? originLoc, string? motherTagNumber, List<string>? fatherTagNumbers, 
        DateOnly? lastVaccinationDate, IdentificationFieldNameDTO[] idFields)
    {
        TagNumber = tagNumber;
        BirthDate = birthDate;
        Breed = breed;
        GroupName = groupName;
        Status = status;
        Origin = origin;
        OriginLocation = originLoc;
        MotherTagNumber = motherTagNumber;
        FatherTagNumbers = fatherTagNumbers is { Count: > 0 }
            ? string.Join(", ", fatherTagNumbers)
            : "";;
        LastVaccinationDate = lastVaccinationDate;
        IdentificationFields = JsonSerializer.Serialize(
            idFields,
            new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

    }
}