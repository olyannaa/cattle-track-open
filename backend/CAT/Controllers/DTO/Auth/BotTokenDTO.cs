namespace CAT.Controllers.DTO
{
    public class BotTokenDTO
    {
        public Guid SessionToken { get; set; }

        public BotTokenDTO(Guid sessionToken)
        {
            SessionToken = sessionToken;
        }
    }
}