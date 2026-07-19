using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CAT.EF.DAL
{

    [Table("rations_components", Schema = "public")]
    public class RationComponent
    {
        [Column("ration_id")]
        public Guid RationId { get; set; }

        [Column("component_id")]
        public Guid ComponentId { get; set; }

        [Column("count")]
        public double Count { get; set; }

        [Column("measure")]
        public string Measure { get; set; } = "kg";

        [Column("cost")]
        public double? Cost { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }
}
