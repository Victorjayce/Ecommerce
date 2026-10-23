using Ecommerce.Data;
using Ecommerce.Dtos.AuthDtos;
using Ecommerce.Interface;
using Ecommerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Ecommerce.Services
{
    public class AuthService : IAuth
    {

        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly DataContext _context;
        private readonly TokenValidationParameters _tokenValidationParameters;
        public AuthService(UserManager<User> userManager, IConfiguration configuration, DataContext context, TokenValidationParameters tokenValidationParameters)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
            _tokenValidationParameters = tokenValidationParameters;
        }
        
        public async Task<ServiceResult<UserDto>> SignUp(RegisterDto payload)
        {
            var userExists = await _userManager.FindByEmailAsync(payload.Email);
            if (userExists != null) return ServiceResult<UserDto>.Fail(ErrorType.BadRequest, "user already exists");

            User newUser = new User()
            {
                FullName = payload.FullName,
                UserName = payload.UserName,
                Email = payload.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
            };
            var result = await _userManager.CreateAsync(newUser, payload.Password);
            var roleResult = await _userManager.AddToRoleAsync(newUser, UserRoles.User);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(newUser);
                return ServiceResult<UserDto>.Fail(ErrorType.BadRequest, roleResult.Errors.Select(e => e.Description).FirstOrDefault()!);
            }
            if(!result.Succeeded) return ServiceResult<UserDto>.Fail(ErrorType.BadRequest, "Failed to create user");

            return ServiceResult<UserDto>.Ok(new UserDto
            {
                Id = newUser.Id,
                UserName = newUser.UserName,
                FullName = newUser.FullName,
                Email = newUser.Email,
                PhoneNumber = newUser.PhoneNumber,
                Role = UserRoles.User
            });
        }

        public async Task<ServiceResult<AuthResultDto>> Login(LoginDto payload)
        {
            var user = await _userManager.FindByEmailAsync(payload.Email);
            if (user != null && await _userManager.CheckPasswordAsync(user, payload.Password))
            {
                var token = await GenerateJwtToken(user);
                return ServiceResult<AuthResultDto>.Ok(token);
            }
            return ServiceResult<AuthResultDto>.Fail(ErrorType.Unauthorized, "Invalid email or password");
        }

        public async Task<ServiceResult<AuthResultDto>> RefreshToken(TokenRequestDto payload)
        {
            var result = await VerifyAndGenerateToken(payload);
            if (result == null) return ServiceResult<AuthResultDto>.Fail(ErrorType.Unauthorized, "Invalid or expired refresh token");
            return ServiceResult<AuthResultDto>.Ok(result);
        }

        public async Task Logout(string userId, string refreshToken)
        {
           var stored = await _context.RefreshTokensTable
                .FirstOrDefaultAsync(x => x.Token == refreshToken && x.UserId == userId);

            if (stored != null)
            {
                stored.IsRevoked = true;
                await _context.SaveChangesAsync();
            }
            return;
        }
        public async Task<AuthResultDto?> VerifyAndGenerateToken(TokenRequestDto payload)
        {
            var handler = new JwtSecurityTokenHandler();

            // validate signature/issuer/audience, but allow an expired access token
            var validationParams = _tokenValidationParameters.Clone();
            validationParams.ValidateLifetime = false;

            SecurityToken validatedToken;
            try
            {
                handler.ValidateToken(payload.Token, validationParams, out validatedToken);
            }
            catch (SecurityTokenException)
            {
                return null; // malformed, bad signature, wrong issuer...
            }

            if (validatedToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;

            var storedToken = await _context.RefreshTokensTable
                .FirstOrDefaultAsync(x => x.Token == payload.RefreshToken);

            if (storedToken == null) return null;                        // doesn't exist
            if (storedToken.JwtId != jwt.Id) return null;                // belongs to a different access token
            if (storedToken.IsRevoked) return null;                      // already used/revoked
            if (storedToken.ExpiryDate < DateTime.UtcNow) return null;   // refresh token expired -> must log in again

            var user = await _userManager.FindByIdAsync(storedToken.UserId);
            if (user == null) return null;

            // rotate: invalidate the old refresh token, issue a fresh pair
            storedToken.IsRevoked = true;
            return await GenerateJwtToken(user);   // saves the new refresh token (and the revoke) in one SaveChanges
        }
        public async Task<AuthResultDto> GenerateJwtToken(User user)
        {
            var authClaims = new List<Claim>
    {
        new(ClaimTypes.Name, user.UserName!),
        new(ClaimTypes.NameIdentifier, user.Id),
        new(JwtRegisteredClaimNames.Email, user.Email!),
        new(JwtRegisteredClaimNames.Sub, user.Id),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    };

            var roles = await _userManager.GetRolesAsync(user);
            authClaims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                expires: DateTime.UtcNow.AddMinutes(15),
                claims: authClaims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                UserId = user.Id,
                JwtId = token.Id,
                IsRevoked = false,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6)
            };

            await _context.RefreshTokensTable.AddAsync(refreshToken);
            await _context.SaveChangesAsync();   // also commits the old token's IsRevoked = true on refresh

            return new AuthResultDto
            {
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                ExpiresAt = token.ValidTo
            };
        }
    }
}
