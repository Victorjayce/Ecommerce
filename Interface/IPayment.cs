using Ecommerce.Dtos;

namespace Ecommerce.Interface
{
    public interface IPayment
    {
        Task<ServiceResult<PaymentResultDto?>> Initialize(
        int orderId, string userId);

        Task<ServiceResult<PaymentResultDto?>> Verify(
            string reference, string userId);

        Task<ServiceResult<bool>> Cancel(
            int orderId, string userId);

        Task<bool> HandleWebhook(byte[] body, string? signature);
    }
}
