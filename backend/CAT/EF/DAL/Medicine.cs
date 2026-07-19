using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    [Table("medicine")]
    public class Medicine
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }
        [Column("organization_id")]
        public Guid OrganizationId { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("substance")]
        public string? Substance { get; set; }
        [Column("drug_elimination_period")]
        public string? DrugEliminatior { get; set; }
        [Column("shelf_life")]
        public string? ShelfLife { get; set; }
        [Column("factory")]
        public string? Factory { get; set; }
    }
}
