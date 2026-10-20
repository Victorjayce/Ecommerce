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
    }
}
