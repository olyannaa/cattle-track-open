

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    [Table("breeds")]
    public class Breed
    {
        [Column("breed_id"), Key]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
    }
}
