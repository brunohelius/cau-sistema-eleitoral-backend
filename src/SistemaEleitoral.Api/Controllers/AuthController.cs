using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using SistemaEleitoral.Application.DTOs.Authentication;
using SistemaEleitoral.Application.Services;

namespace SistemaEleitoral.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("AuthPolicy")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza login com email e senha
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            // Adicionar informações da requisição
            loginDto.EnderecoIp = ObterEnderecoIp();
            loginDto.UserAgent = ObterUserAgent();

            var result = await _authService.LoginAsync(loginDto);
            
            if (result == null)
            {
                return BadRequest(new { 
                    message = "Credenciais inválidas ou conta bloqueada",
                    success = false 
                });
            }

            // Log de sucesso (sem dados sensíveis)
            _logger.LogInformation("Login realizado com sucesso para {Email} de {IP}", 
                loginDto.Email, loginDto.EnderecoIp);

            return Ok(new { 
                data = result,
                success = true,
                message = "Login realizado com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante processo de login");
            return StatusCode(500, new { 
                message = "Erro interno do servidor",
                success = false 
            });
        }
    }

    /// <summary>
    /// Realiza login com token do sistema CAU
    /// </summary>
    [HttpPost("login-cau")]
    [EnableRateLimiting("LoginPolicy")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginCAU([FromBody] LoginCAUDto loginDto)
    {
        try
        {
            loginDto.EnderecoIp = ObterEnderecoIp();
            loginDto.UserAgent = ObterUserAgent();

            var result = await _authService.LoginCAUAsync(loginDto);
            
            if (result == null)
            {
                return BadRequest(new { 
                    message = "Token CAU inválido ou expirado",
                    success = false 
                });
            }

            return Ok(new { 
                data = result,
                success = true,
                message = "Login CAU realizado com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante login CAU");
            return StatusCode(500, new { 
                message = "Erro interno do servidor",
                success = false 
            });
        }
    }

    /// <summary>
    /// Renova o token de acesso usando refresh token
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("RefreshPolicy")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            refreshTokenDto.EnderecoIp = ObterEnderecoIp();
            refreshTokenDto.UserAgent = ObterUserAgent();

            var result = await _authService.RefreshTokenAsync(refreshTokenDto);
            
            if (result == null)
            {
                return Unauthorized(new { 
                    message = "Refresh token inválido ou expirado",
                    success = false 
                });
            }

            return Ok(new { 
                data = result,
                success = true,
                message = "Token renovado com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante renovação de token");
            return StatusCode(500, new { 
                message = "Erro interno do servidor",
                success = false 
            });
        }
    }

    /// <summary>
    /// Realiza logout do usuário atual
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutDto? logoutDto = null)
    {
        try
        {
            var usuarioId = ObterUsuarioId();
            
            logoutDto ??= new LogoutDto();
            logoutDto.EnderecoIp = ObterEnderecoIp();
            logoutDto.UserAgent = ObterUserAgent();

            var result = await _authService.LogoutAsync(usuarioId, logoutDto);
            
            if (!result)
            {
                return BadRequest(new { 
                    message = "Erro durante logout",
                    success = false 
                });
            }

            // Revogar o refresh token se fornecido
            var refreshToken = Request.Headers["Refresh-Token"].FirstOrDefault();
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _authService.RevogarRefreshTokenAsync(refreshToken);
            }

            return Ok(new { 
                success = true,
                message = "Logout realizado com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante logout");
            return StatusCode(500, new { 
                message = "Erro interno do servidor",
                success = false 
            });
        }
    }

    /// <summary>
    /// Altera a senha do usuário atual
    /// </summary>
    [HttpPost("alterar-senha")]
    [Authorize]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaDto alterarSenhaDto)
    {
        try
        {
            var usuarioId = ObterUsuarioId();
            
            var result = await _authService.AlterarSenhaAsync(usuarioId, alterarSenhaDto);
            
            if (!result)
            {
                return BadRequest(new { 
                    message = "Senha atual incorreta",
                    success = false 
                });
            }

            return Ok(new { 
                success = true,
                message = "Senha alterada com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante alteração de senha");
            return StatusCode(500, new { 
                message = "Erro interno do servidor",
                success = false 
            });
        }
    }

    /// <summary>
    /// Inicia processo de recuperação de senha
    /// </summary>
    [HttpPost("recuperar-senha")]
    [EnableRateLimiting("RecoveryPolicy")]
    [AllowAnonymous]
    public async Task<IActionResult> RecuperarSenha([FromBody] RecuperarSenhaDto recuperarSenhaDto)
    {
        try
        {
            // Sempre retorna sucesso por questões de segurança (não revelar se email existe)
            await _authService.IniciarRecuperacaoSenhaAsync(recuperarSenhaDto);
            
            return Ok(new { 
                success = true,
                message = "Se o email existir em nossa base, você receberá instruções para redefinir sua senha"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante recuperação de senha");
            return Ok(new { 
                success = true,
                message = "Se o email existir em nossa base, você receberá instruções para redefinir sua senha"
            });
        }
    }

    /// <summary>
    /// Redefine senha usando token de recuperação
    /// </summary>
    [HttpPost("redefinir-senha")]
    [EnableRateLimiting("RecoveryPolicy")]
    [AllowAnonymous]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDto redefinirSenhaDto)
    {
        try
        {
            var result = await _authService.RedefinirSenhaAsync(redefinirSenhaDto);
            
            if (!result)
            {
                return BadRequest(new { 
                    message = "Token inválido ou expirado",
                    success = false 
                });
            }

            return Ok(new { 
                success = true,
                message = "Senha redefinida com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante redefinição de senha");
            return StatusCode(500, new { 
                message = "Erro interno do servidor",
                success = false 
            });
        }
    }

    /// <summary>
    /// Obtém informações do usuário atual
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        try
        {
            var usuarioId = ObterUsuarioId();
            
            var userInfo = await _authService.ObterInformacoesUsuarioAsync(usuarioId);
            
            if (userInfo == null)
            {
                return NotFound(new { 
                    message = "Usuário não encontrado",
                    success = false 
                });
            }

            return Ok(new { 
                data = userInfo,
                success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter informações do usuário");
            return StatusCode(500, new { 
                message = "Erro interno do servidor",
                success = false 
            });
        }
    }

    /// <summary>
    /// Verifica se o token atual está válido
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    public async Task<IActionResult> ValidateToken()
    {
        try
        {
            var jwtId = User.FindFirst("jti")?.Value;
            
            if (string.IsNullOrEmpty(jwtId))
            {
                return Unauthorized(new { 
                    message = "Token inválido",
                    success = false 
                });
            }

            var sessaoAtiva = await _authService.SessaoAtivaAsync(jwtId);
            
            if (!sessaoAtiva)
            {
                return Unauthorized(new { 
                    message = "Sessão expirada",
                    success = false 
                });
            }

            return Ok(new { 
                success = true,
                message = "Token válido",
                expiresAt = _jwtService.ObterDataExpiracao(ObterTokenAtual())
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante validação de token");
            return StatusCode(500, new { 
                message = "Erro interno do servidor",
                success = false 
            });
        }
    }

    /// <summary>
    /// Revoga todos os refresh tokens do usuário atual
    /// </summary>
    [HttpPost("revoke-all")]
    [Authorize]
    public async Task<IActionResult> RevokeAllTokens()
    {
        try
        {
            var usuarioId = ObterUsuarioId();
            
            var result = await _authService.RevogarTodosRefreshTokensAsync(usuarioId);
            
            if (!result)
            {
                return BadRequest(new { 
                    message = "Erro ao revogar tokens",
                    success = false 
                });
            }

            return Ok(new { 
                success = true,
                message = "Todos os tokens foram revogados com sucesso"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao revogar todos os tokens");
            return StatusCode(500, new { 
                message = "Erro interno do servidor",
                success = false 
            });
        }
    }

    #region Métodos Auxiliares

    private int ObterUsuarioId()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
            
        throw new UnauthorizedAccessException("Token inválido - usuário não identificado");
    }

    private string? ObterEnderecoIp()
    {
        // Verificar se há proxy/load balancer
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }
        
        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }
        
        return Request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? ObterUserAgent()
    {
        return Request.Headers.UserAgent.FirstOrDefault();
    }

    private string ObterTokenAtual()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            return authHeader["Bearer ".Length..];
        }
        
        return string.Empty;
    }

    #endregion
}