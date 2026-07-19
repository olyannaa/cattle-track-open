using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class RationUpdateComponentJson
    {
        [JsonPropertyName("component_id")]
        public Guid ComponentId { get; set; }
        [JsonPropertyName("kg")]
        public double Kg { get; set; }
        [JsonPropertyName("cost")]
        public double Cost { get; set; }
        [JsonPropertyName("count")]
        public double Count { get; set; }
    }
}