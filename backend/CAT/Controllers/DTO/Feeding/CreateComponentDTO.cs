using System;
using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO.Feeding
{
    public class CreateComponentDTO
    {
        [JsonIgnore]
        public Guid? OrganizationId { get; set; }
        public string Name { get; set; }
        public double? Cost { get; set; }
        public int? SV { get; set; }
        public int? SP { get; set; }
        public float? CEP { get; set; }
        public int? NDK { get; set; }
    }
}
