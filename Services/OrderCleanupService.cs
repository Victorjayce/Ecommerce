using Ecommerce.Data;
using Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Services
{
    public class OrderCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderCleanupService> _logger;

        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
        private static readonly TimeSpan Expiry = TimeSpan.FromDays(7);

        public OrderCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<OrderCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await CleanUp(ct);
                    await Task.Delay(Interval, ct);
                }
                catch (OperationCanceledException)
                {
                    break; // app is shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unpaid order cleanup failed.");

                    try { await Task.Delay(Interval, ct); }
                    catch (OperationCanceledException) { break; }
                }
            }
        }

        private async Task CleanUp(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DataContext>();

            var cutoff = DateTime.UtcNow - Expiry;

            var orders = await db.OrdersTable
                .Include(o => o.Items)
                .Where(o => o.Status == OrderStatus.Pending
                         && o.PaymentStatus != PaymentStatus.Successful
                         && o.CreatedAt < cutoff)
                .ToListAsync(ct);

            if (orders.Count == 0) return;

            var productIds = orders
                .SelectMany(o => o.Items)
                .Select(i => i.ProductId)
                .Distinct()
                .ToList();

            var products = await db.ProductsTable
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);

            var orderIds = orders.Select(o => o.Id).ToList();

            var attempts = await db.PaymentsTable
                .Where(p => orderIds.Contains(p.OrderId)
                         && p.Status == PaymentStatus.Pending)
                .ToListAsync(ct);

            foreach (var order in orders)
            {
                foreach (var item in order.Items)
                {
                    if (products.TryGetValue(item.ProductId, out var product))
                        product.Stock += item.Quantity;
                }

                order.Status = OrderStatus.Cancelled;
                order.PaymentStatus = PaymentStatus.Cancelled;
            }

            foreach (var attempt in attempts)
                attempt.Status = PaymentStatus.Cancelled;

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Cancelled {Count} unpaid orders and restored stock.", orders.Count);
        }
    }
}