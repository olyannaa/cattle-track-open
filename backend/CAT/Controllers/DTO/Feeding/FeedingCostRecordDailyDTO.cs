namespace CAT.Controllers.DTO.Feeding
{
    public class FeedingCostRecordDailyDTO
    {
        public DateTime EventDate { get; set; }
        public string GroupName { get; set; }
        public double DailyFactKg { get; set; }
        public List<FeedingCostDetailDTO> FeedingDetails { get; set; }
    }

    public class FeedingCostDetailDTO
    {
        public string FeedingTime { get; set; }
        public double FeedingCoefficient { get; set; }
        public double FactKg { get; set; }
        public Guid RationId { get; set; }
        public string RationName { get; set; }
    }
}
