using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    public class GroupRaw
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; }
        [Column("type_id")]
        public Guid? TypeId { get; set; }
        [Column("type_name")]
        public string? TypeName { get; set; }
        [Column("description")]
        public string? Description { get; set; }
        [Column("location")]
        public string? Location { get; set; }
    }
}
