using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface IProduct
    {
        Task<ServiceResult<List<Product>>> SearchProducts(string query);
        public Task<ServiceResult<List<Product>>> GetProducts(int fetched);
        public Task<ServiceResult<Product>> AddProducts(Product product);
        public Task<ServiceResult<Product?>> UpdateProduct(Product product);
        public Task<ServiceResult<Product?>> RemoveProduct(int productId);
    }
}