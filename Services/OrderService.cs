using Ecommerce.Data;
using Ecommerce.Dtos;
using Ecommerce.Interface;
using Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Services
{
    public class OrderService: IOrder
    {
        private readonly DataContext _context;
        public OrderService(DataContext context)
        {
            _context = context;
        }
        public async Task<List<Order>> GetOrders()
        {
            var orders = await _context.OrdersTable
                .Include(o => o.Items)
                .AsNoTracking()
                .ToListAsync();
            
            return orders;
        }
        public async Task<bool> ClearOrders()
        {
            await _context.OrdersTable.ExecuteDeleteAsync();
            return true;
        }
        public Task<Order?> CreateOrder(PlaceOrderDto placeOrderDto)
        {
            return null;
        }
        public Task<Order?> UpdateOrder(Order order)
        {
            return null;
        }
    }
}
