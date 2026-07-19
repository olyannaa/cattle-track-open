using CAT.EF.DAL;

namespace CAT.Controllers.DTO
{
    public class RoleDTO
    {
        public Guid Id { get; init; }
        public string? Role { get; init; }

        public RoleDTO(Role role)
        {
            Id = role.Id;
            Role = role.Name;    
        }
    }
}