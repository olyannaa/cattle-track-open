using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CAT.EF.DAL
{
    [Table("insemination")]
    public class Insemination
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }
        [Column("cow_id")]
        public Guid? CowId { get; set; }
        [Column("bull_id")]
        public Guid? BullId { get; set; }
        [Column("date")]
        public DateTime? Date { get; set; }
        [Column("insemination_type")]
        public string? Type { get; set; }
        [Column("sperm_batch")]
        public string? SpermBatch { get; set; }
        [Column("sperm_manufacturer")]
        public string? SpermManufacturer { get; set; }
        [Column("embryo_id")]
        public string? EmbryoId { get; set; }
        [Column("embryo_manufacturer")]
        public string? EmbryoManufacturer { get; set; }

        [Column("technician")]
        public string? Technician { get; set; }
        [Column("notes")]
        public string? Notes { get; set; }


        [ForeignKey(nameof(CowId))]
        public virtual Animal? Cow{ get; set; }
        [ForeignKey(nameof(BullId))]
        public virtual Animal? Bull { get; set; }
    }

}
