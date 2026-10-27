using Ecommerce.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Controllers
{
    [Route("api/[controller]"), ApiController, Authorize]
    public class PaymentController : EcommerceController
    {
        private readonly IPayment _payment;
        private readonly IConfiguration _config;

        public PaymentController(IPayment payment, IConfiguration config)
        {
            _payment = payment;
            _config = config;
        }

        [HttpPost("initialize/{orderId}")]
        public async Task<ActionResult> Initialize(int orderId)
        {
            var userId = UserId;
            var result = await _payment.Initialize(orderId, userId);
            return ToActionResult(result);
        }

        [HttpGet("verify/{reference}")]
        public async Task<ActionResult> Verify(string reference)
        {
            var userId = UserId;
            var result = await _payment.Verify(reference, userId);
            return ToActionResult(result);
        }

        [HttpDelete("cancel/{orderId}")]
        public async Task<ActionResult> Cancel(int orderId)
        {
            var userId = UserId;
            var result = await _payment.Cancel(orderId, userId);
            return ToActionResult(result);
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            using var ms = new MemoryStream();
            await Request.Body.CopyToAsync(ms);

            var valid = await _payment.HandleWebhook(
                ms.ToArray(), Request.Headers["x-paystack-signature"]);

            return valid ? Ok() : Unauthorized();
        }
    }
}
