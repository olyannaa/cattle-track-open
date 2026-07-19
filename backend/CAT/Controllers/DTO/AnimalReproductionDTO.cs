using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CAT.Controllers.DTO
{
    public class AnimalReproductionDTO
    {
        [Column("animal_id")]
        public Guid AnimalId { get; set; }

        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("tag_number")]
        public string TagNumber { get; set; }

        [Column("animal_type")]
        public string AnimalType { get; set; }

        [Column("animal_status")]
        public string AnimalStatus { get; set; }

        // В БД: date → лучше DateOnly? (если Npgsql >= 6)
        [Column("birth_date")]
        public DateOnly? BirthDate { get; set; }

        [Column("is_barren"), JsonIgnore]
        public bool IsBarren { get; set; }

        [Column("insemination_id"), JsonIgnore]
        public Guid? InseminationId { get; set; }

        [Column("insemination_date"), JsonIgnore]
        public DateOnly? InseminationDate { get; set; }

        [Column("insemination_type"), JsonIgnore]
        public string? InseminationType { get; set; }

        // НОВОЕ: jsonb
        [Column("bull_id", TypeName = "jsonb"), JsonIgnore]
        public JsonElement? BullJson { get; set; }

        [Column("bull_tag_numbers", TypeName = "jsonb"), JsonIgnore]
        public JsonElement? BullTagNumbersJson { get; set; }

        [Column("pregnancy_id")]
        public Guid? PregnancyId { get; set; }

        [Column("pregnancy_date"), JsonIgnore]
        public DateOnly? PregnancyDate { get; set; }

        [Column("pregnancy_status"), JsonIgnore]
        public string? PregnancyStatus { get; set; }

        [Column("expected_calving_date"), JsonIgnore]
        public DateOnly? ExpectedCalvingDate { get; set; }

        [Column("calving_id"), JsonIgnore]
        public Guid? CalvingId { get; set; }

        [Column("calving_date"), JsonIgnore]
        public DateOnly? CalvingDate { get; set; }

        [Column("calving_complication"), JsonIgnore]
        public string? CalvingComplication { get; set; }

        [Column("calving_type"), JsonIgnore]
        public string? CalvingType { get; set; }

        [Column("calf_id"), JsonIgnore]
        public Guid? CalfId { get; set; }

        [NotMapped]
        public string Name { get; set; }


        [NotMapped]
        public Guid? FirstBullId => TryExtractFirstBullId(BullJson);


        [NotMapped]
        public List<string> BullTagNumbers => ExtractStringsFromJsonArray(BullTagNumbersJson);


        private static Guid? TryExtractFirstBullId(JsonElement? el)
        {
            if (el is null || el.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return null;

            var root = el.Value;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(item.GetString(), out var g))
                        return g;
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("fathers", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object &&
                            item.TryGetProperty("id", out var idProp) &&
                            idProp.ValueKind == JsonValueKind.String &&
                            Guid.TryParse(idProp.GetString(), out var g))
                            return g;
                    }
                }
            }
            return null;
        }

        private static List<string> ExtractStringsFromJsonArray(JsonElement? el)
        {
            var res = new List<string>();
            if (el is null || el.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                return res;

            var root = el.Value;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && item.GetString() is string s && !string.IsNullOrEmpty(s))
                        res.Add(s);
                }
            }
            return res;
        }
    }
}