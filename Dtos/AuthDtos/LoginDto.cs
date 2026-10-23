using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Dtos.AuthDtos
{
    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(8)]
        public string Password { get; set; }
    }
}
