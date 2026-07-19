using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CAT.Controllers.DTO
{
    public class InsertCalvingDTO
    {
        public Guid CowId { get; set; }
        public string CowTagNumber { get; set; }
        public DateOnly Date { get; set; }
        public string Type { get; set; }
        public string Method { get; set; }
        public string? Complication { get; set; }
        public string? Veterinar { get; set; }
        public string? Treatments { get; set; }
        public string? Pathology { get; set; }
        public Guid? InseminationId { get; set; }
        public Guid? BullId { get; set; }

        [NotMapped]
        public List<Guid>? BullIds { get; set; }

        public string? CalfTagNumber { get; set; }
        public double? Weight { get; set; }
        public string? Notes { get; set; }
    }
}
