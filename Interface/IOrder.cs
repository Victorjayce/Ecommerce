using Ecommerce.Dtos;
using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface IOrder
    {
        public Task<List<Order>> GetOrders();
        public Task<bool> ClearOrders();
        public Task<Order?> CreateOrder(PlaceOrderDto placeOrderDto);
        public Task<Order?> UpdateOrder(Order order);
    }
}
