using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class GroupFeedingNutritionDTO
    {
        public string GroupName { get; set; }
        [JsonIgnore]
        public string GroupRationName { get; set; }
        public List<DailyFeedingNutritionDTO> Records { get; set; }
    }

    public class DailyFeedingNutritionDTO
    {
        public DateTime EventDate { get; set; }
        public double TotalSv { get; set; }
        public double TotalSp { get; set; }
        public double TotalCep { get; set; }
        public double TotalNdk { get; set; }
        public string RationName { get; set; }
    }

    public class GroupFeedingNutritionRawDTO
    {
        public DateTime EventDate { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public Guid GroupRationId { get; set; }
        public string GroupRationName { get; set; }
        public double TotalSv { get; set; }
        public double TotalSp { get; set; }
        public double TotalCep { get; set; }
        public double TotalNdk { get; set; }
    }

}
