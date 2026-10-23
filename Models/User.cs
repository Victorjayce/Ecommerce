using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Models
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }= string.Empty;
        public DateTime DateOfBirth { get; set; } = DateTime.MinValue;
    }
}
