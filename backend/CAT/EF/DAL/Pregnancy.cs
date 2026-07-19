using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    [Table("pregnancy")]
    public class Pregnancy
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("cow_id")]
        public Guid CowId { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("expected_calving_date")] 
        public DateTime? ExpectedDate { get; set; }

        [Column("insemination_id")]
        public Guid? InseminationId { get; set; }

        [ForeignKey("CowId")]
        public virtual Animal? Cow { get; set; }

        [ForeignKey("InseminationId")]
        public virtual Insemination? Insemination { get; set; }
    }

}
