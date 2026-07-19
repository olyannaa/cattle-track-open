namespace CAT.Controllers.DTO
{
    public class InvitationDTO
    {
        public Guid RoleId { get; init; }

        public Guid OrgId { get; init; }

        public int Usages { get; set; }
    }
}