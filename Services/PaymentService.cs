using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Data;
using Ecommerce.Dtos;
using Ecommerce.Interface;
using Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Services
{
    public class PaymentService : IPayment
    {
        private readonly DataContext _context;
        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentService> _logger;

        private enum ApplyResult { Mismatch, Applied, OrderCancelled }

        public PaymentService(
            DataContext context,
            HttpClient http,
            IConfiguration config,
            ILogger<PaymentService> logger)
        {
            _context = context;
            _http = http;
            _config = config;
            _logger = logger;

            var secretKey = _config["Paystack:SecretKey"];

            _http.BaseAddress = new Uri("https://api.paystack.co/");
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", secretKey);
        }

        public async Task<ServiceResult<PaymentResultDto?>> Initialize(int orderId, string userId)
        {
            var order = await _context.OrdersTable
                .AsNoTracking()
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.UserId == userId);

            if (order == null)
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.NotFound, "Order not found.");

            if (order.Status == OrderStatus.Cancelled)
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.BadRequest, "This order was cancelled.");

            if (order.PaymentStatus == PaymentStatus.Successful)
            {
                return ServiceResult<PaymentResultDto?>.Ok(new PaymentResultDto
                {
                    Success = true,
                    Message = "Order already paid.",
                    Status = PaymentStatus.Successful
                });
            }

            // Reuse an existing pending payment when possible.
            var existing = await _context.PaymentsTable
                .AsNoTracking()
                .Where(p =>
                    p.OrderId == orderId &&
                    p.UserId == userId &&
                    p.Status == PaymentStatus.Pending &&
                    p.AuthorizationUrl != null)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                return ServiceResult<PaymentResultDto?>.Ok(new PaymentResultDto
                {
                    Success = true,
                    Message = "Existing payment found.",
                    Reference = existing.Reference,
                    AuthorizationUrl = existing.AuthorizationUrl,
                    Status = existing.Status
                });
            }

            // Amount is calculated from the database, never from the client.
            long amountKobo = checked(
                (long)decimal.Round(
                    order.Total * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

            if (amountKobo <= 0)
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.BadRequest, "Order amount must be positive.");

            var email = await GetCustomerEmailAsync(userId);
            if (string.IsNullOrWhiteSpace(email))
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.BadRequest, "Customer email not found.");

            var reference = Guid.NewGuid().ToString("N");

            var attempt = new Payment
            {
                OrderId = orderId,
                UserId = userId,
                Reference = reference,
                AmountKobo = amountKobo,
                Currency = "NGN",
                Status = PaymentStatus.Pending
            };

            _context.PaymentsTable.Add(attempt);
            await _context.SaveChangesAsync();

            var payload = new
            {
                email,
                amount = amountKobo,
                currency = "NGN",
                reference,
                callback_url = _config["Paystack:CallbackUrl"]
            };

            // No try/catch on purpose: a network failure doesn't prove Paystack failed, so the Pending attempt is kept for the webhook/verify to settle.
            var response = await _http.PostAsJsonAsync("transaction/initialize", payload);

            var json = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<PaystackResponse>()
                : null;

            if (json?.Status != true || json.Data?.AuthorizationUrl == null)
            {
                attempt.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();

                return ServiceResult<PaymentResultDto?>.Errored(
                    ErrorType.BadRequest,
                    new PaymentResultDto
                    {
                        Success = false,
                        Message = "Payment initialization failed.",
                        Reference = reference,
                        Status = PaymentStatus.Failed
                    });
            }

            attempt.AuthorizationUrl = json.Data.AuthorizationUrl;
            await _context.SaveChangesAsync();

            return ServiceResult<PaymentResultDto?>.Ok(new PaymentResultDto
            {
                Success = true,
                Message = "Payment initialized.",
                Reference = reference,
                AuthorizationUrl = json.Data.AuthorizationUrl,
                Status = PaymentStatus.Pending
            });
        }

        public async Task<ServiceResult<PaymentResultDto?>> Verify(string reference, string userId)
        {
            var attempt = await _context.PaymentsTable
                .FirstOrDefaultAsync(p =>
                    p.Reference == reference &&
                    p.UserId == userId);

            if (attempt == null)
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.NotFound, "Payment attempt not found.");

            if (attempt.Status == PaymentStatus.Successful)
            {
                return ServiceResult<PaymentResultDto?>.Ok(new PaymentResultDto
                {
                    Success = true,
                    Message = "Payment verified.",
                    Reference = reference,
                    Status = PaymentStatus.Successful
                });
            }

            var response = await _http.GetAsync(
                $"transaction/verify/{Uri.EscapeDataString(reference)}");

            if (!response.IsSuccessStatusCode)
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.BadRequest, "Could not verify payment.");

            var json = await response.Content.ReadFromJsonAsync<PaystackResponse>();
            var data = json?.Data;

            if (json?.Status != true || data == null)
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.BadRequest, "Could not verify payment.");

            if (data.Status == "success")
            {
                var applied = await ApplySuccessfulPayment(attempt, data);

                if (applied == ApplyResult.Mismatch)
                    return ServiceResult<PaymentResultDto?>.Fail(ErrorType.BadRequest, "Payment verification data mismatch.");

                if (applied == ApplyResult.OrderCancelled)
                {
                    return ServiceResult<PaymentResultDto?>.Ok(new PaymentResultDto
                    {
                        Success = false,
                        Message = "Payment received, but this order had already expired. Payment will be refunded.",
                        Reference = reference,
                        Status = PaymentStatus.Successful
                    });
                }

                return ServiceResult<PaymentResultDto?>.Ok(new PaymentResultDto
                {
                    Success = true,
                    Message = "Payment verified.",
                    Reference = reference,
                    Status = PaymentStatus.Successful
                });
            }

            if (data.Status == "failed" || data.Status == "reversed")
            {
                attempt.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();

                return ServiceResult<PaymentResultDto?>.Errored(ErrorType.BadRequest, new PaymentResultDto
                {
                    Success = false,
                    Message = "Payment failed.",
                    Reference = reference,
                    Status = PaymentStatus.Failed
                });
            }

            //the customer hasn't finished paying
            return ServiceResult<PaymentResultDto?>.Ok(new PaymentResultDto
            {
                Success = false,
                Message = "Payment not completed yet.",
                Reference = reference,
                Status = PaymentStatus.Pending
            });
        }

        // Called by the webhook controller
        public async Task HandleChargeSuccess(PaystackTransaction data)
        {
            if (data.Reference == null) return;

            var attempt = await _context.PaymentsTable
                .FirstOrDefaultAsync(p => p.Reference == data.Reference);

            if (attempt == null) return;

            await ApplySuccessfulPayment(attempt, data);
        }

        public async Task<ServiceResult<bool>> Cancel(int orderId, string userId)
        {
            var order = await _context.OrdersTable
                .AsNoTracking()
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.UserId == userId);

            if (order == null)
                return ServiceResult<bool>.Fail(ErrorType.NotFound, "Order not found.");

            if (order.PaymentStatus == PaymentStatus.Successful)
                return ServiceResult<bool>.Fail(ErrorType.BadRequest, "Cannot cancel a successful payment.");

            var attempt = await _context.PaymentsTable
                .Where(p =>
                    p.OrderId == orderId &&
                    p.UserId == userId &&
                    p.Status == PaymentStatus.Pending)
                .FirstOrDefaultAsync();

            if (attempt == null)
                return ServiceResult<bool>.Fail(ErrorType.NotFound, "Payment attempt not found.");

            attempt.Status = PaymentStatus.Cancelled;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        private async Task<ApplyResult> ApplySuccessfulPayment(Payment attempt, PaystackTransaction data)
        {
            if (data.Reference != attempt.Reference ||
                data.Amount != attempt.AmountKobo ||
                data.Currency != attempt.Currency)
                return ApplyResult.Mismatch;

            var order = await _context.OrdersTable
                .FirstAsync(o => o.Id == attempt.OrderId);

            if (attempt.Status != PaymentStatus.Successful)
            {
                attempt.Status = PaymentStatus.Successful;
                attempt.VerifiedAt = DateTime.UtcNow;
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                // Money received for an order we already cancelled and restocked. This payment needs a manual refund.
                _logger.LogWarning(
                    "Payment {Reference} succeeded for cancelled order {OrderId}. Refund needed.",
                    attempt.Reference, order.Id);

                await _context.SaveChangesAsync();
                return ApplyResult.OrderCancelled;
            }

            order.PaymentStatus = PaymentStatus.Successful;
            if (order.Status == OrderStatus.Pending)
                order.Status = OrderStatus.Paid;

            await _context.SaveChangesAsync();
            return ApplyResult.Applied;
        }

        private async Task<string> GetCustomerEmailAsync(string userId)
        {
            var email = await _context.UsersTable
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            return email ?? string.Empty;
        }
    }
}