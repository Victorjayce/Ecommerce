using Ecommerce.Dtos;
using Ecommerce.Interface;
using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers
{
    [Route("api/[controller]"), ApiController, Authorize]
    public class OrderController : EcommerceController
    {
        private readonly IOrder _orderService;
        public OrderController(IOrder orderService)
        {
            _orderService = orderService;
        }
        [HttpGet("get"), Authorize(Roles = UserRoles.Admin)]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _orderService.GetOrders();
            return ToActionResult(orders);
        }
        [HttpGet("get/{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var userId = UserId;
            var order = await _orderService.GetOrder(userId);
            return ToActionResult(order);
        }
        [HttpDelete("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = UserId;
            var result = await _orderService.CancelOrder(orderId, userId);
            return ToActionResult(result);
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] PlaceOrderDto placeOrderDto)
        {
            var userId = UserId;
            var order = await _orderService.CreateOrder(placeOrderDto, userId);
            return ToActionResult(order);
        }
    }
}
