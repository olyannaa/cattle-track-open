namespace CAT.Controllers.DTO.Feeding
{
    public class GroupedFeedingByRationDTO
    {
        public string RationName { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public double DailyFactKg { get; set; }
        public List<FeedingEventDTO> Events { get; set; }
    }

    public class FeedingEventDTO
    {
        public DateTime EventDate { get; set; }
        public double TotalFactKg { get; set; }
        public List<string> FeedingTimes { get; set; }
    }
}
