using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL;

public partial class Group
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public string Name { get; set; } = null!;

    [Column("type_id")]
    public Guid? TypeId { get; set; }

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Animal> Animals { get; set; } = new List<Animal>();

    public virtual Organization? Organization { get; set; }

    public virtual GroupType Type { get; set; }
}
