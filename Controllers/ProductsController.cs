using Ecommerce.Interface;
using Ecommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers
{
    [Route("api/[controller]"), ApiController, Authorize]
    public class ProductsController : EcommerceController
    {
        public readonly IProduct _productService;
        public ProductsController(IProduct productService)
        {
            _productService = productService;
        }

        [HttpGet("search"), AllowAnonymous]
        public async Task<ActionResult> Search([FromQuery] string? query)
        {
            query = query?.Trim() ?? string.Empty;
            return ToActionResult(await _productService.SearchProducts(query));
        }

        [HttpGet("get"), AllowAnonymous]
        public async Task<ActionResult<List<Product>>> GetProducts(int fetched)
        {
            var products = await _productService.GetProducts(fetched);
            return ToActionResult(products);
        }
        [HttpPost("add")]
        public async Task<ActionResult<Product>> AddProducts(Product product)
        {
            var newProduct = await _productService.AddProducts(product);
            return ToActionResult(newProduct);
        }
        [HttpPut("update")]
        public async Task<ActionResult<Product?>> UpdateProduct(Product product)
        {
            var updatedProduct = await _productService.UpdateProduct(product);
            return ToActionResult(updatedProduct);
        }
        [HttpDelete("remove")]
        public async Task<ActionResult<Product?>> RemoveProduct(int productId)
        {
            var removedProduct = await _productService.RemoveProduct(productId);
            return ToActionResult(removedProduct);
        }
    }
}
