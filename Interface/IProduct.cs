using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface IProduct
    {
        public Task<List<Product>> GetProducts(int fetched);
        public Task<Product> AddProducts(Product product);
    }
}