using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace CAT.Controllers.DTO
{
    [NotMapped]
    [ExcludeFromCodeCoverage]
    public class CowInseminationDTO
    {
        [NotMapped]
        public int Id { get; set; }

        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("cow_id")]
        public Guid CowId { get; set; }

        [Column("cow_tag_number")]
        public string? CowTagNumber { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("insemination_type")]
        public string? InseminationType { get; set; }

        [Column("insemination_date")]
        public DateOnly? InseminationDate { get; set; }

        [Column("bull_id")]
        public Guid? BullId { get; set; }

        [Column("bull_tag_number")]
        public string? BullTagNumber { get; set; }

        [NotMapped]
        public List<Guid>? BullIds { get; set; } = new List<Guid>();

        [NotMapped]
        public List<string> BullTagNumbers
        {
            get
            {
                
                if (!string.IsNullOrEmpty(BullTagNumber))
                    return new List<string> { BullTagNumber };

                return new List<string>();
            }
            set
            {
                // Берем первый тег из списка
                BullTagNumber = value?.FirstOrDefault();
            }
        }

        [NotMapped]
        public Guid? PregnancyId { get; set; }

        [NotMapped]
        public Guid? InseminationId { get; set; }

        [NotMapped]
        public string Name { get; set; }
    }

    [NotMapped]
    [ExcludeFromCodeCoverage]
    public class PregnancyStatusDTO
    {
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }

        [Column("cow_id")]
        public Guid CowId { get; set; }

        [Column("cow_tag_number")]
        public string? CowTagNumber { get; set; }

        [Column("status")]
        public string? Status { get; set; }

        [Column("insemination_type")]
        public string? InseminationType { get; set; }

        [Column("insemination_date")]
        public DateOnly? InseminationDate { get; set; }

        [Column("bull_id")]
        public Guid? BullId { get; set; }

        [Column("bull_tag_number")]
        public string? BullTagNumber { get; set; }
    }
}