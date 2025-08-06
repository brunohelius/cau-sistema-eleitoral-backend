using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEleitoral.Application.DTOs.Auth;
using SistemaEleitoral.Application.Interfaces;

namespace SistemaEleitoral.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Realiza o login do usuário
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();
                
                var response = await _authService.LoginAsync(request, ipAddress, userAgent);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Realiza o logout do usuário
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var usuarioId = int.Parse(User.FindFirst("NameIdentifier")?.Value ?? "0");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();
            
            await _authService.LogoutAsync(usuarioId, ipAddress, userAgent);
            return Ok(new { message = "Logout realizado com sucesso" });
        }

        /// <summary>
        /// Solicita recuperação de senha
        /// </summary>
        [HttpPost("recuperar-senha")]
        [AllowAnonymous]
        public async Task<IActionResult> RecuperarSenha([FromBody] RecuperarSenhaDto request)
        {
            await _authService.RecuperarSenhaAsync(request.Email);
            return Ok(new { message = "Se o email existir em nossa base, você receberá as instruções de recuperação" });
        }

        /// <summary>
        /// Redefine a senha usando token de recuperação
        /// </summary>
        [HttpPost("redefinir-senha")]
        [AllowAnonymous]
        public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDto request)
        {
            try
            {
                await _authService.RedefinirSenhaAsync(request);
                return Ok(new { message = "Senha redefinida com sucesso" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Altera a senha do usuário autenticado
        /// </summary>
        [HttpPost("alterar-senha")]
        [Authorize]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaDto request)
        {
            try
            {
                var usuarioId = int.Parse(User.FindFirst("NameIdentifier")?.Value ?? "0");
                await _authService.AlterarSenhaAsync(usuarioId, request);
                return Ok(new { message = "Senha alterada com sucesso" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Valida se o token JWT ainda é válido
        /// </summary>
        [HttpGet("validar-token")]
        [Authorize]
        public IActionResult ValidarToken()
        {
            return Ok(new 
            { 
                valid = true,
                usuario = new
                {
                    id = User.FindFirst("NameIdentifier")?.Value,
                    nome = User.FindFirst("Name")?.Value,
                    email = User.FindFirst("Email")?.Value,
                    tipoUsuario = User.FindFirst("TipoUsuario")?.Value
                }
            });
        }
    }
    
    public class RecuperarSenhaDto
    {
        public string Email { get; set; }
    }
    
    public class RedefinirSenhaDto
    {
        public string Token { get; set; }
        public string NovaSenha { get; set; }
    }
    
    public class AlterarSenhaDto
    {
        public string SenhaAtual { get; set; }
        public string NovaSenha { get; set; }
    }
}