namespace CAT.Controllers.DTO
{
    public class UserDTO
    {
        public Guid Id { get; set; }

        public string? Name { get; set; }

        public UserDTO(Guid id, string? name)
        {
            Id = id;
            Name = name;
        }
    }
}