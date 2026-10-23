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
        public async Task<List<CartItemDto>> GetCartItems()
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
            return Items;

        }

        public async Task<CartItemDto?> GetCartItem(string userId)
        {
            var cartItem = await _context.CartItemsTable.Include(c => c.Product).FirstOrDefaultAsync(c => c.UserId == userId);
            if (cartItem == null)
            {
                return null;
            }

            return _mapToDto(cartItem);
        }

        public async Task<CartItemDto?> AddToCart(int productId, int newQuantity, string userId)
        {
            if (newQuantity < 1)
                return null;

            var product = await _context.ProductsTable.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null || newQuantity > product.Stock)
                return null;

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
            return _mapToDto(cartItem);
        }
        public async Task<CartItemDto?> UpdateCartItem(int productId, int newQuantity, string userId)
        {
            var cartItem = await _context.CartItemsTable.Include(c => c.Product).FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId);
            if (cartItem == null)
            {
                return null;
            }

            if(newQuantity <= 0)
            {
                _context.CartItemsTable.Remove(cartItem);
                await _context.SaveChangesAsync();
                return null;
            }

            cartItem.Quantity = newQuantity;
            await _context.SaveChangesAsync();
            return _mapToDto(cartItem!);
        }
        public async Task<bool> RemoveFromCart(int productId, string userId)
        {
            var cartItem = await _context.CartItemsTable.FirstOrDefaultAsync(c => c.ProductId == productId && c.UserId == userId);
            if (cartItem == null)
            {
                return false;
            }

            _context.CartItemsTable.Remove(cartItem);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> ClearCart(string userId)

        {
            await _context.CartItemsTable.Where(c => c.UserId == userId).ExecuteDeleteAsync();
            await _context.SaveChangesAsync();
            return true;
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
