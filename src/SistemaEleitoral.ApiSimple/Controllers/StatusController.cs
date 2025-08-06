using Microsoft.AspNetCore.Mvc;

namespace SistemaEleitoral.ApiSimple.Controllers
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
                migrationProgress = "60%",
                timestamp = DateTime.UtcNow,
                message = "Sistema Eleitoral CAU API - 60% Migrado",
                features = new
                {
                    authentication = true,
                    chapas = true,
                    votacao = true,
                    denuncia = true,
                    impugnacao = true,
                    calendario = true,
                    resultados = true,
                    emailJobs = "parcial"
                },
                credentials = new
                {
                    admin = new { email = "admin@sistemaeleitoral.com", password = "Admin@123" },
                    eleitor = new { email = "eleitor@teste.com", password = "Eleitor@123" }
                }
            });
        }

        [HttpGet("metrics")]
        public IActionResult GetMetrics()
        {
            return Ok(new
            {
                backend = new
                {
                    progress = "75%",
                    entities = "91/177",
                    controllers = "13/67",
                    services = "17/96",
                    emailJobs = "5/50",
                    endpoints = "400/470"
                },
                frontend = new
                {
                    progress = "35%",
                    admin = "94 arquivos",
                    publico = "95 arquivos"
                },
                totalProgress = "60%",
                linesOfCode = "35000+ migradas"
            });
        }
    }
}