using Ecommerce.Dtos;
using Ecommerce.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrder _orderService;
        public OrderController(IOrder orderService)
        {
            _orderService = orderService;
        }
        [HttpGet("get")]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _orderService.GetOrders();
            return Ok(orders);
        }
        [HttpGet("get/{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            var order = await _orderService.GetOrderById(orderId);
            if (order == null) return NotFound("Order not found");
            return Ok(order);
        }
        [HttpDelete("cancel/{orderId}")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var result = await _orderService.CancelOrder(orderId);
            if (!result) return NotFound("Order not found or already shipped");
            return Ok("Order cancelled successfully");
        }
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] PlaceOrderDto placeOrderDto)
        {
            var order = await _orderService.CreateOrder(placeOrderDto);
            if (order == null) return BadRequest("Failed to create order");
            return Ok(order);
        }
    }
}
