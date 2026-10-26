using Ecommerce.Dtos;
using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface IOrder
    {
        public Task<ServiceResult<List<OrderDto>>> GetOrders();
        public Task<ServiceResult<OrderDto?>> GetOrder(string userId);
        public Task<ServiceResult<bool>> CancelOrder(int orderId, string userId);
        public Task<ServiceResult<OrderDto?>> CreateOrder(PlaceOrderDto placeOrderDto, string userId);
        public Task<ServiceResult<OrderDto?>> UpdateOrder(Order order, string userId);
        public OrderItem CartToOrder(CartItem cartItem);
        public OrderDto OrderToDto(Order order);
    }
}
