namespace CAT.Controllers.DTO
{
    public class EditGroupDTO
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? TypeId { get; set; }
        public string? Location { get; set; }
        public string? Description { get; set; }
    }
}