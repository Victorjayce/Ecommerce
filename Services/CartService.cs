using Ecommerce.Data;
using Ecommerce.Dtos;
using Ecommerce.Interface;
using Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Services
{
    public class CartService: ICart
    {
        private readonly DataContext _context;
        public CartService(DataContext context)
        {
            _context = context;
        }
        public async Task<ServiceResult<List<CartItemDto>>> GetCartItems()
        {
            var cartItems = await _context.CartItemsTable.Include(c => c.Product).ToListAsync();
            var Items = cartItems.Select(ci => new CartItemDto
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                Name = ci.Product.Name,
                UnitPrice = ci.Product.Price,
                Quantity = ci.Quantity,
                Total = ci.Quantity * ci.Product.Price,
                ImageUrl = ci.Product.ImageUrl

            }).ToList();
            return ServiceResult<List<CartItemDto>>.Ok(Items);

        }

        public async Task<ServiceResult<CartItemDto?>> GetCartItem(string userId)
        {
            var cartItem = await _context.CartItemsTable.Include(c => c.Product).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cartItem == null)
            {
                return ServiceResult<CartItemDto?>.Fail(ErrorType.NotFound, "Cart item not found");
            }
            return ServiceResult<CartItemDto?>.Ok(_mapToDto(cartItem));
        }

        public async Task<ServiceResult<CartItemDto?>> AddToCart(int productId, int newQuantity, string userId)
        {
            if (newQuantity < 1)
                return ServiceResult<CartItemDto?>.Fail(ErrorType.NotFound, "Invalid quantity");

            var product = await _context.ProductsTable.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null || newQuantity > product.Stock)
                return ServiceResult<CartItemDto?>.Fail(ErrorType.NotFound, "Invalid product or quantity");

            var cartItem = await _context.CartItemsTable.FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    ProductId = productId,
                    Product = product,    
                    Quantity = newQuantity,
                    UserId = userId
                };
                _context.CartItemsTable.Add(cartItem);
            }
            else
            {
                cartItem.Quantity = newQuantity;  
            }

            await _context.SaveChangesAsync(); 
            return ServiceResult<CartItemDto?>.Ok(_mapToDto(cartItem));
        }
        public async Task<ServiceResult<CartItemDto?>> UpdateCartItem(int productId, int newQuantity, string userId)
        {
            var cartItem = await _context.CartItemsTable.Include(c => c.Product).FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId);
            if (cartItem == null)
            {
                return ServiceResult<CartItemDto?>.Fail(ErrorType.NotFound, "Cart item not found");
            }

            if(newQuantity <= 0)
            {
                _context.CartItemsTable.Remove(cartItem);
                await _context.SaveChangesAsync();
                return ServiceResult<CartItemDto?>.NoContent();
            }

            cartItem.Quantity = newQuantity;
            await _context.SaveChangesAsync();
            return ServiceResult<CartItemDto?>.Ok(_mapToDto(cartItem!));
        }
        public async Task<ServiceResult<bool>> RemoveFromCart(int productId, string userId)
        {
            var cartItem = await _context.CartItemsTable.FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId);
            if (cartItem == null)
            {
                return ServiceResult<bool>.Fail(ErrorType.NotFound, "Cart item not found");
            }

            _context.CartItemsTable.Remove(cartItem);
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }
        public async Task<ServiceResult<bool>> ClearCart(string userId)

        {
            await _context.CartItemsTable.Where(c => c.UserId == userId).ExecuteDeleteAsync();
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true);
        }

        public CartItemDto _mapToDto(CartItem cartItem)
        {
            return new CartItemDto
            {
                Id = cartItem.Id,
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                Name = cartItem.Product.Name,
                UnitPrice = cartItem.Product.Price,
                Total = cartItem.Quantity * cartItem.Product.Price,
                ImageUrl = cartItem.Product.ImageUrl
            };
        }
    }
}
