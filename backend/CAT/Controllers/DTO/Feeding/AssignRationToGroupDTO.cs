using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class AssignRationToGroupDTO
    {
        [JsonIgnore]
        public Guid OrganizationId { get; set; }
        public Guid RationId { get; set; }
        public Guid GroupId { get; set; }
        public double MorningFeeding { get; set; }
        public double DayFeeding { get; set; }
        public double NightFeeding { get; set; }
    }
}

