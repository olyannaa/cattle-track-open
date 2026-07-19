namespace CAT.Controllers.DTO.Feeding
{
    public class GroupFeedingDailyDTO
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int AnimalCount { get; set; }
        public double? TotalFactKg { get; set; }
        public double? TotalFactCost { get; set; }

        public List<FalbackFeedingDetailDTO>? FeedingDetails { get; set; }

        public string DataSource { get; set; } = string.Empty;
    }


}
