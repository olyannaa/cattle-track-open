namespace CAT.Controllers.DTO
{
    public class BullDTO
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public string TagNumber { get; set; }
        public string? Type { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? Status { get; set; }
    }
}
