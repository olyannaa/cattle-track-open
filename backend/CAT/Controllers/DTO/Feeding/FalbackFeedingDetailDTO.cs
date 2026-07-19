using System.Text.Json.Serialization;
namespace CAT.Controllers.DTO.Feeding
{
    

    public class FalbackFeedingDetailDTO
    {
        [JsonPropertyName("ration_id")]
        public Guid? RationId { get; set; }

        [JsonPropertyName("ration_name")]
        public string? RationName { get; set; }

        [JsonPropertyName("feeding_time")]
        public string FeedingTime { get; set; } = string.Empty;

        [JsonPropertyName("feeding_coefficient")]
        public double FeedingCoefficient { get; set; }

        [JsonPropertyName("fact_kg")]
        public double FactKg { get; set; }

        [JsonPropertyName("mark")]
        public int? Mark { get; set; }

        [JsonPropertyName("feeding_mark")]
        public int? FeedingMark { get; set; }
    }

}
