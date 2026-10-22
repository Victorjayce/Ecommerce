using Ecommerce.Models;

namespace Ecommerce.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal Total { get; set; }
        public int Quantity { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
