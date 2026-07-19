using System;
using System.Collections.Generic;

namespace CAT.EF.DAL;

public partial class Organization
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Inn { get; set; }

    public string? Ogrn { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Animal> Animals { get; set; } = new List<Animal>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<IdentificationField> IdentificationFields { get; set; } = new List<IdentificationField>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
