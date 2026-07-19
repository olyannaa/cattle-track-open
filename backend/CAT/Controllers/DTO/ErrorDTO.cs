namespace CAT.Controllers.DTO
{
    public class ErrorDTO
    {
        public string ErrorText { get; set; }
        public ErrorDTO(string errorText)
        {
            ErrorText = errorText;
        }
    }
}
