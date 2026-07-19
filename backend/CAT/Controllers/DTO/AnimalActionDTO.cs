namespace CAT.Controllers.DTO
{
    public class AnimalActionDTO
    {
        public Guid ActionId { get; set; }
        public Guid AnimalId { get; set; }
        public string EventType { get; set; }
        public Dictionary<string, object>? Fields { get; set; } = new Dictionary<string, object>();
        public DateOnly? EventDate { get; set; }
        public string? PerformedBy { get; set; }
        public Guid? BullId { get; set; }
        public Guid? CalfId { get; set; }
    }
}
