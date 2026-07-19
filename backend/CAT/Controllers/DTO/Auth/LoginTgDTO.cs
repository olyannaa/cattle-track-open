namespace CAT.Controllers.DTO
{
    public class LoginTgDTO
    {
        public string AuthDate { get; set; }
        public string? FirstName { get; set; }
        public string Hash { get; set; } = null!;
        public string Id { get; set; } = null!;
        public string? LastName { get; set; }
        public string? PhotoUrl { get; set; }
        public string UserName { get; set; } = null!;
    }
}