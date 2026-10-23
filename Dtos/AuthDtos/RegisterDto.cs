using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Dtos.AuthDtos
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "Username is required.")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8)]
        public string Password { get; set; }
    }
}
