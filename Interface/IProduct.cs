using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface IProduct
    {
        public Task<List<Product>> GetProducts(int fetched);
        public Task<Product> AddProducts(Product product);
        public Task<Product?> UpdateProduct(Product product);
        public Task<Product?> RemoveProduct(int productId);
    }
}