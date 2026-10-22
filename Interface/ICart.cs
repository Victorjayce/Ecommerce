using Ecommerce.Dtos;
using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface ICart
    {
        public Task<List<CartItemDto>> GetCartItems();
        public Task<CartItemDto?> AddToCart(int productId, int newQuantity);
        public Task<CartItemDto?> UpdateCartItem(int productId, int newQuantity);
        public Task<bool> RemoveFromCart(int productId);
        public Task<bool> ClearCart();
        public CartItemDto _mapToDto(CartItem cartItem);
    }
}
