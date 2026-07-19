using System;
using System.Collections.Generic;

namespace CAT.EF.DAL;

public partial class Permission
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }


}
