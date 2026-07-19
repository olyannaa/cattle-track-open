namespace CAT.Controllers.DTO
{
    public class PregnancyDTO
    {
        public Guid CowId { get; set; }
        public DateOnly Date { get; set; }
        public string Status { get; set; }
        public DateOnly? ExpectedCalvingDate { get; set; }
    }
}
