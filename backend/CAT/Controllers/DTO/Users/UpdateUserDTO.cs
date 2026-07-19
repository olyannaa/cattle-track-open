namespace CAT.Controllers.DTO
{
    public class ChangeUserRoleDTO
    {
        public Guid RoleId { get; init; }
    }

    public class ResetUserPasswordDTO
    {
        public string Password { get; init; } = null!;
    }
}
