
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.Controllers.DTO
{
    public class UpdateAnimalCardDTO
    {
        [Required]
        [Column("id")]
        public Guid Id { get; set; }
        
        [Column("organization_id")]
        public Guid? OrgId { get; set; }

        [Column("type")]
        public string? Type { get; set; }

        [Column("status")]
        public string? Status { get; set; }
        
        [Column("group_id")]
        public Guid? GroupId { get; set; }

        [Column("birth_date")]
        public DateTime? BirthDate { get; set; }

        [Column("date_of_receipt")]
        public DateTime? DateOfReceipt { get; set; }

        [Column("date_of_disposal")]
        public DateTime? DateOfDisposal { get; set; }

        [Column("reason_of_disposal")]
        public string? ReasonOfDisposal { get; set; }

        [Column("identification_data")]
        public Dictionary<string, string>? IdentificationData { get; set; }
        
        [Column("breed")]
        public string? Breed { get; set; }

        [Column("origin")]
        public string? Origin { get; set; }

        [Column("origin_location")]
        public string? OriginLocation { get; set; }

        [Column("tag_number")]
        public string? TagNumber { get; set; }

        [Column("mother_id")]
        public Guid? MotherId { get; set; }
        
        [Column("mother_tag_number")]

        public string? MotherTagNumber { get; set; }

        [Column("father_id")]
        public List<string>? FatherIds { get; set; }

        [Column("father_tag_numbers")]
        public string? FatherTagNumber { get; set; }
    }
}