using Ecommerce.Interface;
using Ecommerce.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        public readonly IProduct _productService;
        public ProductsController(IProduct productService)
        {
            _productService = productService;
        }

        [HttpGet("get-products")]
        public async Task<ActionResult<List<Product>>> GetProducts(int fetched)
        {
            var products = await _productService.GetProducts(fetched);
            return Ok(products);
        }
        [HttpPost("add-products")]
        public async Task<ActionResult<Product>> AddProducts(Product product)
        {
            var newProduct = await _productService.AddProducts(product);
            return Ok(newProduct);
        }
    }
}
