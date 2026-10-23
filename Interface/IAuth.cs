using Ecommerce.Dtos.AuthDtos;
using Ecommerce.Models;

namespace Ecommerce.Interface
{
    public interface IAuth
    {
        Task<ServiceResult<UserDto>> SignUp(RegisterDto payload);
        Task<ServiceResult<AuthResultDto>> Login(LoginDto payload);
        Task<ServiceResult<AuthResultDto>> RefreshToken(TokenRequestDto payload);
        Task Logout(string userId, string refreshToken);
        Task<AuthResultDto?> VerifyAndGenerateToken(TokenRequestDto payload);
        Task<AuthResultDto> GenerateJwtToken(User user);
    }
}
