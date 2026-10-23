using Azure.Core;
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
        public async Task<List<OrderDto>> GetOrders()
        {
            var orders = await _context.OrdersTable
                .Include(o => o.Items)
                .AsNoTracking()
                .ToListAsync();
            
            return orders.Select(OrderToDto).ToList();
        }
        public async Task<OrderDto?> GetOrder(string userId)
        {
            var order = await _context.OrdersTable
                .Include(o => o.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.UserId == userId);
            if(order == null) return null;

            return OrderToDto(order);
        }
        public async Task<bool> CancelOrder(int orderId, string userId)
        {
            var order = await _context.OrdersTable
                                .Include(o => o.Items)
                                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order == null) return false;
            if(order.Status == OrderStatus.Cancelled ||  order.Status == OrderStatus.Delivered) return false;

            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await _context.ProductsTable
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // put the stock back
            foreach (var item in order.Items)
                products[item.ProductId].Stock += item.Quantity;

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<OrderDto?> CreateOrder(PlaceOrderDto request, string userId)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Phone) ||
                string.IsNullOrWhiteSpace(request.DeliveryAddress))
                return null;

            var cartItems = await _context.CartItemsTable.Where(c => c.UserId == userId)
                                .Include(c => c.Product)
                                .ToListAsync();

            if (cartItems.Count == 0)
                return null;

            foreach (var ci in cartItems)
            {
                if (ci.Quantity > ci.Product.Stock)
                    return null;
            }

            var order = new Order
            {
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                FullName = request.FullName,
                Phone = request.Phone,
                DeliveryAddress = request.DeliveryAddress,
                Items = cartItems.Select(CartToOrder).ToList(),
                UserId = userId
            };
            order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            foreach (var ci in cartItems)
                ci.Product.Stock -= ci.Quantity; 

            _context.OrdersTable.Add(order);
            _context.CartItemsTable.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            return OrderToDto(order);
        }
        public async Task<OrderDto?> UpdateOrder(Order order, string userId)
        {
            var Findorder = await _context.OrdersTable
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == order.Id && o.UserId == userId);
            if(Findorder == null) return null;
            Findorder.Status = order.Status;
            await _context.SaveChangesAsync();
            return OrderToDto(Findorder);
        }
        public OrderItem CartToOrder(CartItem cartItem)
        {
            return new OrderItem
            {
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product.Name,   
                UnitPrice = cartItem.Product.Price,     
                Quantity = cartItem.Quantity
            };
        }
        public OrderDto OrderToDto(Order order)
        {
            var orderdto = new OrderDto
            {
                Id = order.Id,
                FullName = order.FullName,
                Phone = order.Phone,
                Address = order.DeliveryAddress,
                Status = order.Status,
                Total = order.Total,
                Quantity = order.Items.Sum(i => i.Quantity)
            };
            var items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList();
            orderdto.Items = items;
            return orderdto;
        }
    }
}
