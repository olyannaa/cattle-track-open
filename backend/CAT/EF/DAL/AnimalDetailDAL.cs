using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CAT.EF.DAL
{
    public class AnimalDetailDAL
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        [Column("tag_number")]
        public string TagNumber { get; set; }

        [Column("type")]
        public string Type { get; set; }

        [Column("breed")]
        public string? Breed { get; set; }

        [Column("mother_id")]
        public Guid? MotherId { get; set; }

        [Column("mother_tag_number")]
        public string? MotherTagNumber { get; set; }

        [Column("father_id")]
        public Guid? FatherId { get; set; }

        [Column("father_tag_number")]
        public string? FatherTagNumber { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("group_id")]
        public Guid? GroupId { get; set; }

        [NotMapped] 
        public string? GroupName { get; set; }

        [Column("origin")]
        public string? Origin { get; set; }

        [Column("origin_location")]
        public string? OriginLocation { get; set; }

        [Column("birth_date")]
        public DateOnly? BirthDate { get; set; }

        [Column("date_of_receipt")]
        public DateOnly? DateOfReceipt { get; set; }

        [Column("date_of_disposal")]
        public DateOnly? DateOfDisposal { get; set; }

        [Column("reason_of_disposal")]
        public string? ReasonOfDisposal { get; set; }

        [NotMapped]
        public string? Name { get; set; }

        [NotMapped]
        [JsonIgnore] 
        public Dictionary<string, string> IdentificationData { get; set; } = new Dictionary<string, string>();

        [Column("identification_data", TypeName = "jsonb")]
        public string IdentificationDataJson { get; set; }

        public void ParseIdentificationData()
        {
            if (!string.IsNullOrEmpty(IdentificationDataJson))
            {
                try
                {
                    var jsonData = JObject.Parse(IdentificationDataJson);
                    foreach (var property in jsonData.Properties())
                    {
                        IdentificationData[property.Name] = property.Value.ToString();
                    }
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine($"Error parsing identification data: {ex.Message}");
                }
            }
        }
    }
    public class AnimalDetail2DAL
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        [Column("tag_number")]
        public string TagNumber { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("breed")]
        public string? Breed { get; set; }

        [Column("mother_id")]
        public Guid? MotherId { get; set; }

        [Column("mother_tag_number")]
        public string? MotherTagNumber { get; set; }
        [NotMapped]
        public string? Name { get; set; }

        [Column("father_id", TypeName = "jsonb")]
        public JsonElement? FatherJson { get; set; }

        [Column("father_tag_numbers", TypeName = "jsonb")]
        public JsonElement? FatherTagNumbersJson { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("group_id")]
        public Guid? GroupId { get; set; }

        [Column("group_name")]
        public string? GroupName { get; set; }

        [Column("origin")]
        public string? Origin { get; set; }

        [Column("origin_location")]
        public string? OriginLocation { get; set; }

        [Column("birth_date")]
        public DateOnly? BirthDate { get; set; }

        [Column("date_of_receipt")]
        public DateOnly? DateOfReceipt { get; set; }

        [Column("date_of_disposal")]
        public DateOnly? DateOfDisposal { get; set; }

        [Column("reason_of_disposal")]
        public string? ReasonOfDisposal { get; set; }


        [NotMapped]
        [JsonIgnore]
        public Dictionary<string, string> IdentificationData { get; set; } = new();

        [Column("identification_data", TypeName = "jsonb")]
        public string IdentificationDataJson { get; set; }

        public void ParseIdentificationData()
        {
            if (!string.IsNullOrEmpty(IdentificationDataJson))
            {
                try
                {
                    var jsonData = JObject.Parse(IdentificationDataJson);
                    foreach (var property in jsonData.Properties())
                        IdentificationData[property.Name] = property.Value?.ToString();
                }
                catch (JsonReaderException ex)
                {
                    Console.WriteLine($"Error parsing identification data: {ex.Message}");
                }
            }
        }

        [NotMapped]
        public List<Guid> FatherIds
            => ExtractGuidsFromJson(FatherJson);

        [NotMapped]
        public List<string> FatherTagNumbers
            => ExtractStringsFromJson(FatherTagNumbersJson);

        private static List<Guid> ExtractGuidsFromJson(JsonElement? el)
        {
            var res = new List<Guid>();
            if (el is null || el.Value.ValueKind == JsonValueKind.Null || el.Value.ValueKind == JsonValueKind.Undefined)
                return res;

            var root = el.Value;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out var g))
                        res.Add(g);
            }
            else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("fathers", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.Object &&
                        item.TryGetProperty("id", out var idProp) &&
                        idProp.ValueKind == JsonValueKind.String &&
                        Guid.TryParse(idProp.GetString(), out var g))
                        res.Add(g);
            }

            return res;
        }

        private static List<string> ExtractStringsFromJson(JsonElement? el)
        {
            var res = new List<string>();
            if (el is null || el.Value.ValueKind == JsonValueKind.Null || el.Value.ValueKind == JsonValueKind.Undefined)
                return res;

            var root = el.Value;
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String)
                        res.Add(item.GetString()!);
            }
            return res;
        }
    }

    public class AnimalDetail2Response
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public string TagNumber { get; set; }
        public string Type { get; set; }
        public string? Breed { get; set; }

        public Guid? MotherId { get; set; }
        public string? MotherTagNumber { get; set; }

        public List<Guid> FatherIds { get; set; } = new();
        public List<string> FatherTagNumbers { get; set; } = new();

        public string? Status { get; set; }
        public Guid? GroupId { get; set; }
        public string? GroupName { get; set; }

        public string? Origin { get; set; }
        public string? OriginLocation { get; set; }
        public DateOnly? BirthDate { get; set; }
        public DateOnly? DateOfReceipt { get; set; }
        public DateOnly? DateOfDisposal { get; set; }
        public string? ReasonOfDisposal { get; set; }

        public Dictionary<string, string> IdentificationData { get; set; } = new();
    }

}