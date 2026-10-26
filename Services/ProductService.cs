using Ecommerce.Data;
using Ecommerce.Interface;
using Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Services
{
    public class ProductService : IProduct
    {
        private readonly DataContext _context;
        public ProductService(DataContext context)
        {
            _context = context;
        }
        public async Task<ServiceResult<List<Product>>> SearchProducts(string query)
        {
            var q = query.Trim();

            if (query.Length > 100)
                query = query[..100];


            if (q.Length == 0)
            {
                return ServiceResult<List<Product>>.Ok(new List<Product>());
            }

            var result = await _context.ProductsTable
                .AsNoTracking()
                .Where(p => p.Name.Contains(q) || p.Description.Contains(q))
                .OrderBy(p =>
                    p.Name.StartsWith(q) ? 0 : 
                    p.Name.Contains(q) ? 1 :  
                    p.Description.StartsWith(q) ? 2 :  
                                                  3) 
                .ThenBy(p => p.Name)
                .ThenBy(p => p.Id)
                .Take(50)
                .ToListAsync();

            return ServiceResult<List<Product>>.Ok(result);
        }
        public async Task<ServiceResult<List<Product>>> GetProducts(int fetched)
        {
            var products = await _context.ProductsTable.Skip(fetched).Take(50).ToListAsync();
            return ServiceResult<List<Product>>.Ok(products);
        }
        public async Task<ServiceResult<Product>> AddProducts(Product product)
        {
            product.Id = 0;
            _context.ProductsTable.Add(product);
            await _context.SaveChangesAsync();
            return ServiceResult<Product>.Ok(product);
        }
        public async Task<ServiceResult<Product?>> UpdateProduct(Product product)
        {
            var existingProduct = _context.ProductsTable.FirstOrDefault(p => p.Id == product.Id);
            if (existingProduct == null) return ServiceResult<Product?>.Fail(ErrorType.NotFound, "Product not found");

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            await _context.SaveChangesAsync();
            return ServiceResult<Product?>.Ok(existingProduct);
        }
        public async Task<ServiceResult<Product?>> RemoveProduct(int productId)
        {
            var product = _context.ProductsTable.FirstOrDefault(p => p.Id == productId);
            if (product == null) return ServiceResult<Product?>.Fail(ErrorType.BadRequest, "Product not found");

            _context.ProductsTable.Remove(product);
            await _context.SaveChangesAsync();
            return ServiceResult<Product?>.Ok(product);
        }

        }
    }
}
