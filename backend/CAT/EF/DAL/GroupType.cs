using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL;

[Table("group_types")]
public partial class GroupType
{
    public Guid Id { get; set; }

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    public string Name { get; set; } = null!;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    public virtual Organization? Organization { get; set; }

    public virtual ICollection<Group>? Groups { get; set; }
}