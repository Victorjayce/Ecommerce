using Ecommerce.Data;
using Ecommerce.Dtos.AuthDtos;
using Ecommerce.Interface;
using Ecommerce.Models;
using Ecommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ApiControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DataContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly IAuth _authService;

        public AuthController(UserManager<User> userManager, IConfiguration configuration, DataContext context, TokenValidationParameters tokenValidationParameters, IAuth authService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
            _authService = authService;
        }

        [HttpPost("customer/signup")]
        public async Task<IActionResult> Register([FromBody] RegisterDto payload)
        {
            return ToActionResult(await _authService.SignUp(payload));
        }

        [HttpPost("customer/login")]
        public async Task<IActionResult> Login([FromBody] LoginDto payload)
        {
            if (!ModelState.IsValid) return BadRequest("provide all required details");
            return ToActionResult(await _authService.Login(payload));
        }

        [HttpPost("customer/refresh-token")]
        public async Task<IActionResult> TokenRefresh([FromBody] TokenRequestDto payload)
        {
            if (!ModelState.IsValid) return BadRequest("provide all required details");

            return ToActionResult(await _authService.RefreshToken(payload));
        }

        [HttpPost("logout/{refreshToken}"), Authorize]
        public async Task<IActionResult> Logout(string refreshToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return Unauthorized();

            await _authService.Logout(userId, refreshToken);
            return NoContent();
        }
    }
}
