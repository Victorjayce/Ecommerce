using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Collections.Specialized.BitVector32;

namespace Ecommerce.Models
{
    [Index(nameof(UserId), nameof(OrderId), nameof(Reference), IsUnique = true)]
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;

        public string Reference { get; set; } = null!;

        public long AmountKobo { get; set; }

        public string Currency { get; set; } = "NGN";

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        public string? AuthorizationUrl { get; set; }

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;

        public DateTime? VerifiedAt { get; set; }
    }
}
