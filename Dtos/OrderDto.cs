using Ecommerce.Models;

namespace Ecommerce.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal Total { get; set; }
        public int Quantity { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }
}
