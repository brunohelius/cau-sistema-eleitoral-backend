using Microsoft.AspNetCore.Mvc;

namespace SistemaEleitoral.ApiSimple.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            // Validação simplificada para teste
            if (dto.Email == "admin@sistemaeleitoral.com" && dto.Password == "Admin@123")
            {
                return Ok(new
                {
                    token = $"jwt-token-admin-{Guid.NewGuid()}",
                    refreshToken = Guid.NewGuid().ToString(),
                    expiration = DateTime.UtcNow.AddHours(2),
                    user = new
                    {
                        id = "admin-001",
                        email = dto.Email,
                        nome = "Administrador do Sistema",
                        role = "Admin"
                    }
                });
            }
            else if (dto.Email == "eleitor@teste.com" && dto.Password == "Eleitor@123")
            {
                return Ok(new
                {
                    token = $"jwt-token-eleitor-{Guid.NewGuid()}",
                    refreshToken = Guid.NewGuid().ToString(),
                    expiration = DateTime.UtcNow.AddHours(2),
                    user = new
                    {
                        id = "eleitor-001",
                        email = dto.Email,
                        nome = "Eleitor Teste",
                        role = "Eleitor"
                    }
                });
            }

            return Unauthorized(new { message = "Email ou senha inválidos" });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            return Ok(new { message = "Logout realizado com sucesso" });
        }

        [HttpGet("validate")]
        public IActionResult ValidateToken([FromHeader] string authorization)
        {
            if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
            {
                return Unauthorized();
            }

            var token = authorization.Replace("Bearer ", "");
            if (token.StartsWith("jwt-token-"))
            {
                return Ok(new { valid = true, role = token.Contains("admin") ? "Admin" : "Eleitor" });
            }

            return Unauthorized();
        }
    }

    public class LoginDto
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}