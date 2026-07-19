namespace CAT.Controllers.DTO
{
    public class CreateGroupDTO
    {
        public string Name { get; set;}
        public Guid? TypeId { get; set;}
        public string? Location { get; set;}
        public string? Description { get; set;}
    }
}
