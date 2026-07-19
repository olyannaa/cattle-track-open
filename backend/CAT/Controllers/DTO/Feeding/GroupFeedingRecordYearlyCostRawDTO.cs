using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class GroupFeedingRecordYearlyCostRawDTO
    {
        public DateTime EventDate { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public Guid GroupRationId { get; set; }
        public string GroupRationName { get; set; }
        public double RationCost { get; set; }
        public double TotalRationCost { get; set; }
        public string MonthYear { get; set; }
    }
    public class GroupFeedingRecordYearlyCostDTO
    {
        public string GroupName { get; set; }
        [JsonIgnore]
        public string GroupRationName { get; set; }
        public List<MonthlyFeedingCostStatDTO> Records { get; set; }
    }

    public class MonthlyFeedingCostStatDTO
    {
        public string MonthYear { get; set; }
        public double RationCost { get; set; }
        public double TotalRationCost { get; set; }
        public string RationName { get; set; }
    }
}
