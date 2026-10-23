using Ecommerce.Dtos;
using Ecommerce.Interface;
using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce.Controllers
{
    [Route("api/[controller]"), ApiController, Authorize]
    public class CartController : ApiControllerBase
    {
        private readonly ICart _cartService;
        public CartController(ICart cartService)
        {
            _cartService = cartService;
        }

        [HttpGet("get-all"), Authorize(Roles = UserRoles.Admin)]
        public async Task<ActionResult<List<CartItemDto>>> GetCartItems()
        {
            var cartItems = await _cartService.GetCartItems();
            return Ok(cartItems);
        }

        [HttpGet("get")]
        public async Task<ActionResult<List<CartItemDto>>> GetCartItem()
        {
            var userId = UserId;
            if(userId == null)
            {
                return Unauthorized("User not authenticated");
            }
            var cartItem = await _cartService.GetCartItem(userId);
            if (cartItem == null)
            {
                return NotFound("Item not found");
            }
            return Ok(cartItem);
        }

        [HttpPost("add")]
        public async Task<ActionResult<CartItemDto?>> AddToCart(AddToCartDto addToCartDto)
        {
            var userId = UserId;
            if(userId == null)
            {
                return Unauthorized("User not authenticated");
            }
            var cartItem = await _cartService.AddToCart(addToCartDto.ProductId, addToCartDto.Quantity, userId);
            if (cartItem == null)
            {
                return BadRequest("Item not found or insufficient stock");
            }
            return Ok(cartItem);
        }

        [HttpPut("update")]
        public async Task<ActionResult<CartItemDto?>> UpdateCartItem(AddToCartDto updateCartItemDto)
        {
            var userId = UserId;
            if(userId == null)
            {
                return Unauthorized("User not authenticated");
            }
            var cartItem = await _cartService.UpdateCartItem(updateCartItemDto.ProductId, updateCartItemDto.Quantity, userId);
            if (cartItem == null)
            {
                return BadRequest("Item not found or insufficient stock");
            }
            return Ok(cartItem);
        }

        [HttpDelete("remove/{productId}")]
        public async Task<ActionResult<bool>> RemoveFromCart(int productId)
        {
            var userId = UserId;
            if(userId == null)
            {
                return Unauthorized("User not authenticated");
            }
            var result = await _cartService.RemoveFromCart(productId, userId);
            if (!result)
            {
                return BadRequest("Item not found");
            }
            return Ok(result);
        }

        [HttpDelete("clear")]
        public async Task<ActionResult<bool>> ClearCart()
        {
            var userId = UserId;
            if(userId == null)
            {
                return Unauthorized("User not authenticated");
            }
            var result = await _cartService.ClearCart(userId);
            return Ok("Cart cleared successfully");
        }
    }
}
