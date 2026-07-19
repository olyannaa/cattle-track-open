using CAT.EF.DAL;

namespace CAT.Controllers.DTO
{
    public class UserInfoDTO
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public string? Login { get; set; }

        public Guid? OrgId { get; set; }

        public string? OrgName { get; set; }

        public Guid? RoleId { get; set; }

        public string? RoleName { get; set; }

        public string[]? Permissions { get; set; }

        public UserInfoDTO(User? user)
        {
            Id = user?.Id ?? Guid.Empty;
            Name = user?.Name;
            OrgId = user?.OrganizationId;
            OrgName = user?.Organization?.Name;
            RoleId = user?.RoleId;
            RoleName = user?.Role?.Name;
            Login = user?.Username;
        }
    }
}