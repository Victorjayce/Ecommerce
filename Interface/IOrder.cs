using Ecommerce.Dtos;
using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface IOrder
    {
        public Task<List<OrderDto>> GetOrders();
        public Task<OrderDto?> GetOrderById(int orderId);
        public Task<bool> CancelOrder(int orderId);
        public Task<OrderDto?> CreateOrder(PlaceOrderDto placeOrderDto);
        public Task<OrderDto?> UpdateOrder(Order order);
        public OrderItem CartToOrder(CartItem cartItem);
        public OrderDto OrderToDto(Order order);
    }
}
