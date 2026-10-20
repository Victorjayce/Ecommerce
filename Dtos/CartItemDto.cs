using Ecommerce.Models;

namespace Ecommerce.Dtos
{
    public class CartItemDto
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
        public string ImageUrl { get; set; }
    }
}
