using Ecommerce.Dtos;
using Ecommerce.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICart _cartService;
        public CartController(ICart cartService)
        {
            _cartService = cartService;
        }
        [HttpGet("get")]
        public async Task<ActionResult<List<CartItemDto>>> GetCartItems()
        {
            var cartItems = await _cartService.GetCartItems();
            return Ok(cartItems);
        }
        [HttpPost("add")]
        public async Task<ActionResult<CartItemDto?>> AddToCart(AddToCartDto addToCartDto)
        {
            var cartItem = await _cartService.AddToCart(addToCartDto.ProductId, addToCartDto.Quantity);
            if (cartItem == null)
            {
                return BadRequest();
            }
            return Ok(cartItem);
        }
        [HttpPut("update")]
        public async Task<ActionResult<CartItemDto?>> UpdateCartItem(AddToCartDto updateCartItemDto)
        {
            var cartItem = await _cartService.UpdateCartItem(updateCartItemDto.ProductId, updateCartItemDto.Quantity);
            if (cartItem == null)
            {
                return BadRequest();
            }
            return Ok(cartItem);
        }
        [HttpDelete("remove/{productId}")]
        public async Task<ActionResult<bool>> RemoveFromCart(int productId)
        {
            var result = await _cartService.RemoveFromCart(productId);
            if (!result)
            {
                return BadRequest("Item not found");
            }
            return Ok(result);
        }
        [HttpDelete("clear")]
        public async Task<ActionResult<bool>> ClearCart()
        {
            var result = await _cartService.ClearCart();
            return Ok("Cart cleared successfully");
        }
    }
}
