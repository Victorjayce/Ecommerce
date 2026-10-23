using Ecommerce.Dtos;
using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface ICart
    {
        public Task<List<CartItemDto>> GetCartItems();
        public Task<CartItemDto?> GetCartItem(string userId);
        public Task<CartItemDto?> AddToCart(int productId, int newQuantity, string userId);
        public Task<CartItemDto?> UpdateCartItem(int productId, int newQuantity, string userId);
        public Task<bool> RemoveFromCart(int productId, string userId);
        public Task<bool> ClearCart(string userId);
        public CartItemDto _mapToDto(CartItem cartItem);
    }
}
