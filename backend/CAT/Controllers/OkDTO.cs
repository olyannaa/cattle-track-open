namespace CAT.Controllers.DTO
{
    public class OkDTO
    {
        public string Message { get; set; }
        public OkDTO(string message)
        {
            Message = message;
        }
    }
}