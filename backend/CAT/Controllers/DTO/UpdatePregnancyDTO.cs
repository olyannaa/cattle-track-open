namespace CAT.Controllers.DTO
{
    public class UpdatePregnancyDTO
    {
        public Guid Id { get; set; }
        public DateOnly Date { get; set; }
        public string Status { get; set; }
        public DateOnly? ExceptedDate { get; set; }
    }
}