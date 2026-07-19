namespace CAT.Controllers.DTO.Feeding
{
    public class GroupFeedingRecordDTO
    {
        public DateTime EventDate { get; set; }
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public Guid RationId { get; set; }
        public string RationName { get; set; }
        public double DailyFactKg { get; set; }
    }
}
