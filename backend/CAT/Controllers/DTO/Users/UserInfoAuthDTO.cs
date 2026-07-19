using CAT.EF.DAL;

namespace CAT.Controllers.DTO
{
    public class UserInfoAuthDTO
    {
        public string? Id { get; set; }

        public string? Name { get; set; }

        public string? OrganizationId { get; set; }

        public string? OrganizationName { get; set; }

        public string? RoleId { get; set; }

        public string[]? PermissionIds { get; set; }
        public UserInfoAuthDTO() { }
        public UserInfoAuthDTO(UserInfoDTO user)
        {
            Id = user.Id.ToString();
            Name = user.Name;
            OrganizationId = user.OrgId.ToString();
            OrganizationName = user.OrgName;
            RoleId = user.RoleId.ToString();
            PermissionIds = user.Permissions;
        }
    }
}
