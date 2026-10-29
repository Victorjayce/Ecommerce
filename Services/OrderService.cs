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
        private readonly IPayment _payment;
        public OrderService(DataContext context, IPayment payment)
        {
            _context = context;
            _payment = payment;
        }
        public async Task<ServiceResult<List<OrderDto>>> GetOrders()
        {
            var orders = await _context.OrdersTable
                .Include(o => o.Items)
                .AsNoTracking()
                .ToListAsync();
            
            return ServiceResult<List<OrderDto>>.Ok(orders.Select(OrderToDto).ToList());
        }
        public async Task<ServiceResult<List<OrderDto?>>> GetUserOrders(string userId)
        {
            var orders = await _context.OrdersTable
                .Include(o => o.Items)
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .ToListAsync();

            if(orders == null) return ServiceResult<List<OrderDto?>>.Fail(ErrorType.NotFound, "No orders found for this user");

            var ordersDto = orders.Select(OrderToDto).ToList();

            return ServiceResult<List<OrderDto?>>.Ok(ordersDto!);
        }
        public async Task<ServiceResult<bool>> CancelOrder(int orderId, string userId)
        {
            var order = await _context.OrdersTable
                                .Include(o => o.Items)
                                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order == null) return ServiceResult<bool>.Fail(ErrorType.NotFound, "Order not found");
            if(order.Status == OrderStatus.Cancelled ||  order.Status == OrderStatus.Delivered) return ServiceResult<bool>.Fail(ErrorType.BadRequest, "Order already cancelled or delivered");

            var productIds = order.Items.Select(i => i.ProductId).ToList();
            var products = await _context.ProductsTable
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // put the stock back
            foreach (var item in order.Items)
                products[item.ProductId].Stock += item.Quantity;

            order.Status = OrderStatus.Cancelled;

            if (order.PaymentStatus == PaymentStatus.Successful)
                return ServiceResult<bool>.Fail(ErrorType.BadRequest, "Cannot cancel order with successful payment.");

            var attempt = await _context.PaymentsTable
                .Where(p =>
                    p.OrderId == orderId &&
                    p.UserId == userId &&
                    p.Status == PaymentStatus.Pending)
                .FirstOrDefaultAsync();

            if (attempt == null)
                return ServiceResult<bool>.Fail(ErrorType.NotFound, "Payment attempt not found.");

            attempt.Status = PaymentStatus.Cancelled;

            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
        public async Task<ServiceResult<OrderDto?>> CreateOrder(PlaceOrderDto request, string userId)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Phone) ||
                string.IsNullOrWhiteSpace(request.DeliveryAddress))
                return ServiceResult<OrderDto?>.Fail(ErrorType.BadRequest, "Please provide all required details");

            var cartItems = await _context.CartItemsTable.Where(c => c.UserId == userId)
                                .Include(c => c.Product)
                                .ToListAsync();

            if (cartItems.Count == 0)
                return ServiceResult<OrderDto?>.Fail(ErrorType.NotFound, "Cart is empty");

            foreach (var ci in cartItems)
            {
                if (ci.Quantity > ci.Product.Stock)
                    return ServiceResult<OrderDto?>.Fail(ErrorType.BadRequest, "Insufficient stock for some items");
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

            var result = OrderToDto(order);

            var payment = await _payment.Initialize(order.Id, userId);
            if (payment.Success)
            {
                result.PaymentReference = payment.Data?.Reference;
                result.AuthorizationUrl = payment.Data?.AuthorizationUrl;
            }

            return ServiceResult<OrderDto?>.Ok(result);
        }
        private OrderItem CartToOrder(CartItem cartItem)
        {
            return new OrderItem
            {
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product.Name,   
                UnitPrice = cartItem.Product.Price,     
                Quantity = cartItem.Quantity
            };
        }
        private OrderDto OrderToDto(Order order)
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
