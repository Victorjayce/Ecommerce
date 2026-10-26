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
    public class CartController : EcommerceController
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
            return ToActionResult(cartItems);
        }

        [HttpGet("get")]
        public async Task<ActionResult<List<CartItemDto>>> GetCartItem()
        {
            var userId = UserId;
            var cartItem = await _cartService.GetCartItem(userId);
            return ToActionResult(cartItem);
        }

        [HttpPost("add")]
        public async Task<ActionResult<CartItemDto?>> AddToCart(AddToCartDto addToCartDto)
        {
            var userId = UserId;
            var cartItem = await _cartService.AddToCart(addToCartDto.ProductId, addToCartDto.Quantity, userId);
            return ToActionResult(cartItem);
        }

        [HttpPut("update")]
        public async Task<ActionResult<CartItemDto?>> UpdateCartItem(AddToCartDto updateCartItemDto)
        {
            var userId = UserId;
            var cartItem = await _cartService.UpdateCartItem(updateCartItemDto.ProductId, updateCartItemDto.Quantity, userId);
            return ToActionResult(cartItem);
        }

        [HttpDelete("remove/{productId}")]
        public async Task<ActionResult<bool>> RemoveFromCart(int productId)
        {
            var userId = UserId;
            var result = await _cartService.RemoveFromCart(productId, userId);
            return ToActionResult(result);
        }

        [HttpDelete("clear")]
        public async Task<ActionResult<bool>> ClearCart()
        {
            var userId = UserId;
            var result = await _cartService.ClearCart(userId);
            return ToActionResult(result);
        }
    }
}
