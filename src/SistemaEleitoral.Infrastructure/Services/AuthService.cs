using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Infrastructure.DTOs;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Infrastructure.Services
{
    public interface IAuthService
    {
        Task<object> LoginAsync(LoginDto dto);
        Task<object> RefreshTokenAsync(RefreshTokenDto dto);
        Task<bool> LogoutAsync(string userId);
    }

    public interface IJwtService
    {
        string GenerateJwtToken(ApplicationUser user);
        string GenerateRefreshToken();
        bool ValidateToken(string token);
    }

    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IJwtService jwtService,
            ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<object> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning($"Login failed: User not found - {dto.Email}");
                return null;
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
            if (!result.Succeeded)
            {
                _logger.LogWarning($"Login failed: Invalid password - {dto.Email}");
                return null;
            }

            var token = _jwtService.GenerateJwtToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            user.UltimoAcesso = DateTime.Now;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation($"User logged in successfully: {user.Email}");

            return new
            {
                token,
                refreshToken,
                expiration = DateTime.UtcNow.AddHours(2),
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    nome = user.NomeCompleto
                }
            };
        }

        public async Task<object> RefreshTokenAsync(RefreshTokenDto dto)
        {
            // Implementação simplificada
            return await Task.FromResult(new { token = "new-token", refreshToken = "new-refresh-token" });
        }

        public async Task<bool> LogoutAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _userManager.UpdateAsync(user);
                await _signInManager.SignOutAsync();
                return true;
            }
            return false;
        }
    }
}