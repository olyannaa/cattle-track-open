using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Medicine
{
    public class CreateMedicineDTO
    {
        [JsonIgnore]
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Substance { get; set; }
        public string? DrugEliminationPeriod { get; set; }
        public string? ShelfLife { get; set; }
        public string? Factory { get; set; }
    }
}
