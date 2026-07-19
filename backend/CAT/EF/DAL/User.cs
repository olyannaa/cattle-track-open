using System;
using System.Collections.Generic;

namespace CAT.EF.DAL;

public partial class User
{
    public Guid Id { get; set; }

    public Guid? OrganizationId { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public Guid? RoleId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Name { get; set; }

    public string? PhoneNumber { get; set; }

    public string? TgId { get; set; }

    public virtual Organization? Organization { get; set; }

    public virtual Role? Role { get; set; } = null!;
}
