using Ecommerce.Data;
using Ecommerce.Interface;
using Ecommerce.Models;

namespace Ecommerce.Services
{
    public class ProductService : IProduct
    {
        private readonly DataContext _context;
        public ProductService(DataContext context)
        {
            _context = context;
        }
        public Task<List<Product>> GetProducts(int fetched)
        {
            var products = _context.ProductsTable.Skip(fetched).Take(50).ToList();
            return Task.FromResult(products);
        }
        public Task<Product> AddProducts(Product product)
        {
            product.Id = 0;
            _context.ProductsTable.Add(product);
            _context.SaveChanges();
            return Task.FromResult(product);
        }
        public async Task<Product?> UpdateProduct(Product product)
        {
            var existingProduct = _context.ProductsTable.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct == null) return null;

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            await _context.SaveChangesAsync();
            return existingProduct;
        }
        public async Task<Product?> RemoveProduct(int productId)
        {
            var product = _context.ProductsTable.FirstOrDefault(p => p.Id == productId);
            if (product == null) return null;

            _context.ProductsTable.Remove(product);
            await _context.SaveChangesAsync();
            return product;
        }

        }
    }
}
