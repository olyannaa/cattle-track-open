using System.ComponentModel.DataAnnotations;

namespace CAT.Controllers.DTO
{
    public class LoginDTO
    {
        /// <example>demo-user</example>>
        [Required]
        public string Login { get; set; } = null!;

        /// <example>change-me</example>>
        [Required]
        public string Password { get; set; } = null!;
    }
}
