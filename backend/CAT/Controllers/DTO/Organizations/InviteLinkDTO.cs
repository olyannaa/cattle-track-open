namespace CAT.Controllers.DTO
{
    public class InviteLinkDTO
    {
        public string Link { get; init; }

        public InviteLinkDTO(string link)
        {
            Link = link;
        }
    }
}