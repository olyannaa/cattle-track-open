using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    [Table("calvings")]
    public class Calving
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }
        [Column("cow_id")]
        public Guid CowId { get; set; }
        [Column("date")]
        public DateOnly Date { get; set; }
        [Column("complication")]
        public string Complication { get; set; }
        [Column("type")]
        public string Type { get; set; }
        [Column("veterinar")]
        public string? Veterinar { get; set; }
        [Column("treatments")]
        public string? Treatments { get; set; }
        [Column("pathology")]
        public string? Pathology { get; set; }
        [Column("calf_id")]
        public string CalfId { get; set; }

        [ForeignKey("cow_id")]
        public virtual Animal? Cow { get; set; }
    }

}

