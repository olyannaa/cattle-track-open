using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class GroupedFeedingRecordDTO
    {
        public string GroupName { get; set; }
        [JsonIgnore]
        public string GroupRationName { get; set; }
        public List<DailyFeedingStatDTO> Records { get; set; }
    }

    public class DailyFeedingStatDTO
    {
        public DateTime EventDate { get; set; }
        public double DailyFactKg { get; set; }
        public string RationName { get; set; }
    }
}
