using Ecommerce.Models;

namespace Ecommerce.Dtos
{
    public class PaymentResultDto
    {
        public bool Success { get; set; }

        public string Message { get; set; } = "";

        public string? Reference { get; set; }

        public string? AuthorizationUrl { get; set; }

        public PaymentStatus Status { get; set; }
    }
}
