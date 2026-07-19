namespace CAT.Controllers.DTO
{
    public class InsertPregnancyDTO
    {
        public Guid CowId { get; set; }
        public DateOnly Date { get; set; }
        public string Status { get; set; }
        public DateOnly? ExpectedCalvingDate { get; set; }
        public Guid InseminationId { get; set; }
    }
}
