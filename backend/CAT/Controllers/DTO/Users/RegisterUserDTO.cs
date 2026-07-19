namespace CAT.Controllers.DTO
{
    public class RegisterUserDTO
    {
        public string? Name { get; init; }
        public string PhoneNumber { get; init; }
        public string Login { get; init; }
        public string Password { get; init; }
        public string? TgId { get; init; }
        public bool IsOrgAdmin { get; init; }
    }
}