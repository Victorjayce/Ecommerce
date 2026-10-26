using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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

        public PaymentService(
            DataContext context,
            HttpClient http,
            IConfiguration config)
        {
            _context = context;
            _http = http;
            _config = config;

            var secretKey = _config["Paystack:SecretKey"];

            _http.BaseAddress =
                new Uri("https://api.paystack.co/");

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer", secretKey);
        }

        public async Task<ServiceResult<PaymentResultDto?>> Initialize(
            int orderId, string userId)
        {
            var order = await _context.OrdersTable
                .FirstOrDefaultAsync(o =>
                    o.Id == orderId &&
                    o.UserId == userId);

            if (order == null)
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.NotFound, "Order not found.");

            if (order.PaymentStatus == PaymentStatus.Successful)
            {
                var result = new PaymentResultDto
                {
                    Success = true,
                    Message = "Order already paid.",
                    Status = PaymentStatus.Successful,
                };
                return ServiceResult<PaymentResultDto?>.Ok(result);
            }

            // Reuse an existing pending payment when possible.
            var existing = await _context.PaymentsTable
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

            // Amount is calculated from the database.
            long amountKobo = checked(
                (long)decimal.Round(
                    order.Total * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

            if (amountKobo <= 0)
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.BadRequest, "Order amount must be positive.");

            // TODO: Retrieve the email from your user record.
            var email = await GetCustomerEmailAsync(userId);

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
                callback_url =
                    _config["Paystack:CallbackUrl"]
            };

            try
            {
                var response = await _http.PostAsJsonAsync(
                    "transaction/initialize", payload);

                var json = await response.Content
                    .ReadFromJsonAsync<PaystackResponse>();

                if (!response.IsSuccessStatusCode ||
                    json?.Status != true ||
                    json.Data?.AuthorizationUrl == null)
                {
                    attempt.Status = PaymentStatus.Failed;
                    await _context.SaveChangesAsync();
                    var result3 = new PaymentResultDto
                    {
                        Success = false,
                        Message = "Payment initialization failed.",
                        Reference = reference,
                        Status = PaymentStatus.Failed
                    };

                    return ServiceResult<PaymentResultDto?>.Errored(ErrorType.BadRequest,result3,  "Payment initialization failed.");
                }

                attempt.AuthorizationUrl =
                    json.Data.AuthorizationUrl;
                attempt.Status = PaymentStatus.Pending;

                await _context.SaveChangesAsync();

                var result = new PaymentResultDto
                {
                    Success = true,
                    Message = "Payment initialized.",
                    Reference = reference,
                    AuthorizationUrl =
                        json.Data.AuthorizationUrl,
                    Status = PaymentStatus.Pending
                };
                return ServiceResult<PaymentResultDto?>.Ok(result);
            }
            catch
            {
                // Preserve the attempt for reconciliation.
                // A network failure does not prove Paystack failed to initialize the transaction.
                throw;
            }
        }

        public async Task<ServiceResult<PaymentResultDto?>> Verify(
        string reference, string userId)
        {
            var attempt = await _context.PaymentsTable
                .FirstOrDefaultAsync(p =>
                    p.Reference == reference &&
                    p.UserId == userId);

            if (attempt == null)
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.NotFound, "Payment attempt not found.");

            var response = await _http.GetAsync(
                $"transaction/verify/{Uri.EscapeDataString(reference)}");

            response.EnsureSuccessStatusCode();

            var json = await response.Content
                .ReadFromJsonAsync<PaystackResponse>();

            var data = json?.Data;

            if (json?.Status != true || data == null)
            {
                var result1 = new PaymentResultDto
                {
                    Success = false,
                    Message = "Could not verify payment.",
                    Reference = reference,
                    Status = attempt.Status
                };
                return ServiceResult<PaymentResultDto?>.Errored(ErrorType.BadRequest, result1, "Could not verify payment.");

            }

            // Never trust the frontend's payment status.
            if (data.Reference != attempt.Reference ||
                data.Amount != attempt.AmountKobo ||
                data.Currency != attempt.Currency)
            {
                return ServiceResult<PaymentResultDto?>.Fail(ErrorType.BadRequest, "Payment verification data mismatch.");
            }

            if (data.Status == "success")
            {
                var order = await _context.OrdersTable
                    .FirstAsync(o =>
                        o.Id == attempt.OrderId &&
                        o.UserId == userId);

                attempt.Status = PaymentStatus.Successful;
                attempt.VerifiedAt = DateTime.UtcNow;
                order.PaymentStatus = PaymentStatus.Successful;

                await _context.SaveChangesAsync();

                var result2 =new PaymentResultDto
                {
                    Success = true,
                    Message = "Payment verified.",
                    Reference = reference,
                    Status = PaymentStatus.Successful
                };
                return ServiceResult<PaymentResultDto?>.Ok(result2);
            }

            var result = new PaymentResultDto
            {
                Success = false,
                Message = "Payment is not successful.",
                Reference = reference,
                Status = PaymentStatus.Failed
            };
            return ServiceResult<PaymentResultDto?>.Ok(result);
        }

        public async Task<ServiceResult<bool>> Cancel(
            int orderId, string userId)
        {
            var order = await _context.OrdersTable
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
                    (p.Status == PaymentStatus.Pending))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (attempt == null)
                return ServiceResult<bool>.Fail(ErrorType.NotFound, "Payment attempt not found.");

            attempt.Status = PaymentStatus.Cancelled;

            // Keep the order unpaid. Your order workflow
            // decides whether to cancel the order itself.
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Ok(true);
        }

        private async Task<string> GetCustomerEmailAsync(
            string userId)
        {
            var email = await _context.UsersTable
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();
            if(email != null)
            {
                return email;
            }
            return string.Empty;
        }
    }
}
