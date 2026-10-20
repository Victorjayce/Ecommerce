using Ecommerce.Data;
using Ecommerce.Interface;

namespace Ecommerce.Services
{
    public class OrderService: IOrder
    {
        private readonly DataContext _context;
        public OrderService(DataContext context)
        {
            _context = context;
        }
    }
}
