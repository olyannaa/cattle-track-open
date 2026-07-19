namespace CAT.Controllers.DTO
{
    public class CreateInvDTO
    {
        public Guid RoleId { get; init; }

        public Guid OrgId { get; init; }

        public int UsageLimit { get; set; }

        public TimeSpan ExpireTime { get; init; }
    }
}