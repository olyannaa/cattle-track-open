using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class CreateRationRequestDTO
    {
        public string RationName { get; set; }
        public string? Description { get; set; }
        public Guid? GroupId { get; set; }
        [JsonIgnore]
        public Guid OrganizationId { get; set; }
        public List<RationComponentInputDTO> Components { get; set; }
    }
}
