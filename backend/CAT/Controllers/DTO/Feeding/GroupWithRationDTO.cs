namespace CAT.Controllers.DTO.Feeding
{
    public class GroupWithRationDTO
    {
        public Guid GroupId { get; set; }
        public string GroupName { get; set; }
        public string? GroupDescription { get; set; }
        public string? GroupLocation { get; set; }
        public long ActiveAnimalsCount { get; set; }
        public Guid? RationId { get; set; }
        public string? RationName { get; set; }
        public string? RationDescription { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
