using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class GroupFeedingRecordCostDTO
    {
        public string GroupName { get; set; }
        [JsonIgnore]
        public string GroupRationName { get; set; }
        public List<DailyFeedingCostStatDTO> Records { get; set; }
    }

    public class DailyFeedingCostStatDTO
    {
        public DateTime EventDate { get; set; }
        public double RationCost { get; set; }
        public double TotalRationCost { get; set; }
        public string RationName { get; set; }
    }

    public class FlatFeedingCostRecord
    {
        public DateTime EventDate { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public Guid GroupRationId { get; set; }
        public string GroupRationName { get; set; }
        public double RationCost { get; set; }
        public double TotalRationCost { get; set; }
    }

}
