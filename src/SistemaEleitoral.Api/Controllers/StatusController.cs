using Microsoft.AspNetCore.Mvc;

namespace SistemaEleitoral.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                status = "online",
                version = "1.0.0",
                timestamp = DateTime.UtcNow,
                message = "Sistema Eleitoral CAU API está funcionando!"
            });
        }

        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new { status = "healthy" });
        }
    }
}