namespace CAT.Controllers.DTO.Feeding
{
    public class GroupFeedingStatsDTO
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public int AnimalCount { get; set; }
        public Guid? GroupRationId { get; set; }
        public string GroupRationName { get; set; }
        public double MorningFeeding { get; set; }
        public double DayFeeding { get; set; }
        public double NightFeeding { get; set; }
        public double TotalKg { get; set; }
        public double TotalKgForGroup { get; set; }
        public double TotalCost { get; set; }
    }


}
