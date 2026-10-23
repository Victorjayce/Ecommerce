using Ecommerce.Dtos;
using Ecommerce.Interface;
using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers
{
    [Route("api/[controller]"), ApiController, Authorize]
    public class OrderController : ApiControllerBase
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
            return Ok(orders);
        }
        [HttpGet("get/{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var userId = UserId;
            if (userId == null)
            {
                return Unauthorized("User not authenticated");
            }
            var order = await _orderService.GetOrder(userId);
            if (order == null) return NotFound("Order not found");
            return Ok(order);
        }
        [HttpDelete("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = UserId;
            if (userId == null)
            {
                return Unauthorized("User not authenticated");
            }
            var result = await _orderService.CancelOrder(orderId, userId);
            if (!result) return NotFound("Order not found or already shipped");
            return Ok("Order cancelled successfully");
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] PlaceOrderDto placeOrderDto)
        {
            var userId = UserId;
            if (userId == null)
            {
                return Unauthorized("User not authenticated");
            }
            var order = await _orderService.CreateOrder(placeOrderDto, userId);
            if (order == null) return BadRequest("Failed to create order");
            return Ok(order);
        }
    }
}
