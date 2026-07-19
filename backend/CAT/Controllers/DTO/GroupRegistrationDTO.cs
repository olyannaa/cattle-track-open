namespace CAT.Controllers.DTO
{
    public class GroupRegistrationDTO
    {
        public string Name { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
        public Guid TypeId { get; set; }
    }
}
