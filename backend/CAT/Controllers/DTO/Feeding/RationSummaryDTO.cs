namespace CAT.Controllers.DTO.Feeding
{
    public class RationSummaryDTO
    {
        public Guid RationId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public Guid OrganizationId { get; set; }
        public DateTime CreatedAt { get; set; }
        public double? TotalDryMatter { get; set; }
        public double? TotalNEMaintenance { get; set; }
        public double? TotalNEGain { get; set; }
        public double? TotalCrudeProtein { get; set; }
        public double? TotalDegradableProtein { get; set; }
        public double? TotalCrudeFat { get; set; }
        public double? TotalByproduct { get; set; }
        public double? TotalRoughage { get; set; }
        public double? TotalNDF { get; set; }
        public double? TotalForageNDF { get; set; }
        public double? TotalStarch { get; set; }
        public double? TotalCalcium { get; set; }
        public double? TotalPhosphorus { get; set; }
        public double? TotalSalt { get; set; }
        public double? TotalPotassium { get; set; }
        public double? TotalSulfur { get; set; }
        public double? TotalCost { get; set; }
        public double ComponentsCount { get; set; }
    }
}
