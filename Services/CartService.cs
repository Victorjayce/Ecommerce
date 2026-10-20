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
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                Name = ci.Product.Name,
                UnitPrice = ci.Product.Price
            }).ToList();
            return Items;

        }
        public async Task<CartItemDto?> AddToCart(int productId, int quantity)
        {
            var productexists = await _context.ProductsTable.AnyAsync(p => p.Id == productId);
            if (!productexists)
            {
                return null;
            }
            
            var existingCartItem = await _context.CartItemsTable.FindAsync(productId);
            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
                return _mapToDto(existingCartItem);
            }
            await _context.CartItemsTable.AddAsync(new CartItem
            {
                    ProductId = productId,
                    Quantity = quantity
            });
            var cartItem = await _context.CartItemsTable.Include(c => c.Product).FirstOrDefaultAsync(c => c.ProductId == productId);
            await _context.SaveChangesAsync();
            return _mapToDto(cartItem!);
        }
        public async Task<CartItemDto?> UpdateCartItem(int productId, int quantity)
        {
            var cartItem = await _context.CartItemsTable.Include(c => c.Product).FirstOrDefaultAsync(c => c.ProductId == productId);
            if (cartItem == null)
            {
                return null;
            }

            if(quantity <= 0)
            {
                _context.CartItemsTable.Remove(cartItem);
                await _context.SaveChangesAsync();
                return null;
            }

            cartItem.Quantity = quantity;
            await _context.SaveChangesAsync();
            return _mapToDto(cartItem!);
        }
        public async Task<bool> RemoveFromCart(int productId)
        {
            var cartItem = await _context.CartItemsTable.FindAsync(productId);
            if (cartItem == null)
            {
                return false;
            }

            _context.CartItemsTable.Remove(cartItem);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> ClearCart()
        {
            await _context.CartItemsTable.ExecuteDeleteAsync();
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
