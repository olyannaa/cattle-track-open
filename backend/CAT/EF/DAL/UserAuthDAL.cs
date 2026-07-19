using System.ComponentModel.DataAnnotations.Schema;

namespace CAT.EF.DAL
{
    public class UserAuthDAL
    {
        [Column("user_id")]
        public Guid Id { get; set; }

        [Column("user_phone")]
        public string? PhoneNumber { get; set; } = null!;

        [Column("user_login")]
        public string Login { get; set; } = null!;

        [Column("user_password")]
        public string Password { get; set; } = null!;

        [Column("user_organization")]
        public Guid? OrganizationId { get; set; }

        [Column("user_role_id")]
        public Guid? RoleId { get; set; }

        [Column("user_permissions_name")]
        public string[] PermissionIds { get; set; } = null!;
    }
}