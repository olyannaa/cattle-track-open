namespace CAT.Controllers.DTO
{
    public class CalvingDTO
    {
        public Guid CowId { get; set; }
        public DateOnly Date { get; set; }
        public string Complication { get; set; }
        public string Type { get; set; }
        public string? Veterinar { get; set; }
        public string? Treatments { get; set; }
        public string? Pathology { get; set; }
        public string? CalfId { get; set; }
        public Guid InseminationId { get; internal set; }
    }
}
