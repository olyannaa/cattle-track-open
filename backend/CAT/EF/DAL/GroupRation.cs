using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CAT.EF.DAL
{
    [Table("group_rations", Schema = "public")]
    public class GroupRation
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("group_id")]
        public Guid GroupId { get; set; }

        [Required]
        [Column("ration_id")]
        public Guid RationId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("morning_feeding")]
        public double? MorningFeeding { get; set; }

        [Column("day_feeding")]
        public double? DayFeeding { get; set; }

        [Column("night_feeding")]
        public double? NightFeeding { get; set; }

        public virtual Group Group { get; set; }
        public virtual Ration Ration { get; set; }
    }
}
