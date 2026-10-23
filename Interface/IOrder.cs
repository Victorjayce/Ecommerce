using Ecommerce.Dtos;
using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface IOrder
    {
        public Task<List<OrderDto>> GetOrders();
        public Task<OrderDto?> GetOrder(string userId);
        public Task<bool> CancelOrder(int orderId, string userId);
        public Task<OrderDto?> CreateOrder(PlaceOrderDto placeOrderDto, string userId);
        public Task<OrderDto?> UpdateOrder(Order order, string userId);
        public OrderItem CartToOrder(CartItem cartItem);
        public OrderDto OrderToDto(Order order);
    }
}
