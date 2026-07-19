using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class RecordFeedingDTO
    {
        public DateOnly EventDate { get; set; }
        [JsonIgnore]
        public Guid OrganizationId { get; set; }
        public Guid GroupId { get; set; }
        public long AnimalCount { get; set; }
        public Guid GroupRationId { get; set; }
        public double TotalKg { get; set; }
        public double TotalKgForGroup { get; set; }
        public double FactKg { get; set; }
        public string? FeedingTime { get; set; } 
        public double? FeedingCoefficient { get; set; }
        public int? Mark { get; set; }
        public double? FeedingMark { get; set; }
    }

}
