using CAT.Controllers.DTO.Attributes;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class FeedingRecordDailyDTO
    {
        public DateTime EventDate { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public double DailyFactKg { get; set; }
        public List<FallbackFeedingDetailDTO> FeedingDetails { get; set; }
    }

    public class FallbackFeedingDetailDTO
    {
        [JsonPropertyName("feeding_time")]
        public string FeedingTime { get; set; } 

        [JsonPropertyName("feeding_coefficient")]
        public double FeedingCoefficient { get; set; }

        [JsonPropertyName("fact_kg")]
        public double FactKg { get; set; }

        [JsonPropertyName("ration_id")]
        public Guid RationId { get; set; }

        [JsonPropertyName("ration_name")]
        public string RationName { get; set; }
    }

    public class GetFeedingDTO
    {
        [FromHeader]
        public Guid organizationId { get; set; }

        [FromQuery]
        public string date { get; set; } // Используем string

        [JsonIgnore]
        public DateOnly DateParsed
        {
            get
            {
                if (string.IsNullOrEmpty(date))
                    return DateOnly.FromDateTime(DateTime.Today);

                var formats = new[] { "dd-MM-yyyy", "dd.MM.yyyy", "yyyy-MM-dd", "dd/MM/yyyy" };
                foreach (var format in formats)
                {
                    if (DateOnly.TryParseExact(date, format, CultureInfo.GetCultureInfo("ru-RU"), DateTimeStyles.None, out var result))
                        return result;
                }

                return DateOnly.FromDateTime(DateTime.Today);
            }
        }
    }
}
