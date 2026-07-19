using System;
using System.Collections.Generic;

namespace CAT.EF.DAL;

public partial class RolesPermission
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
