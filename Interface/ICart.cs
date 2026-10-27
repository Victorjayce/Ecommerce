using Ecommerce.Dtos;
using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface ICart
    {
        public Task<ServiceResult<List<CartItemDto>>> GetAllCartItems();
        public Task<ServiceResult<List<CartItemDto?>>> GetCartItems(string userId);
        public Task<ServiceResult<CartItemDto?>> AddToCart(int productId, int newQuantity, string userId);
        public Task<ServiceResult<CartItemDto?>> UpdateCartItem(int productId, int newQuantity, string userId);
        public Task<ServiceResult<bool>> RemoveFromCart(int productId, string userId);
        public Task<ServiceResult<bool>> ClearCart(string userId);
        public CartItemDto _mapToDto(CartItem cartItem);
    }
}
