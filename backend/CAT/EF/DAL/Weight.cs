using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CAT.EF.DAL
{
    [Table("weights")]
    public class Weight
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("animal_id")]
        [Required]
        public Guid AnimalId { get; set; }

        [Column("date")]
        [Required]
        public DateOnly Date { get; set; }

        [Column("weight")]
        [Required]
        [MaxLength(255)] // Для character varying
        public string WeightValue { get; set; }

        [Column("method")]
        public string Method { get; set; }

        [Column("notes")]
        public string Notes { get; set; }
    }
}
