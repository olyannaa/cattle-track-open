namespace CAT.Controllers.DTO
{
    public class ManageUserDTO : UserDTO
    {    
        public string? Login { get; set; }

        public Guid? RoleId { get; set; }

        public ManageUserDTO(Guid id, string? name, string? login, Guid? roleId) : base(id, name)
        {
            Login = login;
            RoleId = roleId;
        }
    }
}