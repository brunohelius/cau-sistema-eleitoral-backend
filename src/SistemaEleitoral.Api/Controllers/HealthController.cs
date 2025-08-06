using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContextMinimal _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ApplicationDbContextMinimal context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var result = new
            {
                Status = "OK",
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                Version = "1.0.0",
                Database = new
                {
                    CanConnect = await CanConnectToDatabase(),
                    TotalUsuarios = await GetTotalUsuarios()
                }
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(500, new { Status = "Error", Message = ex.Message });
        }
    }

    [HttpGet("database")]
    public async Task<IActionResult> DatabaseCheck()
    {
        try
        {
            var canConnect = await CanConnectToDatabase();
            if (!canConnect)
            {
                return StatusCode(500, new { Status = "Error", Message = "Cannot connect to database" });
            }

            var tables = await _context.Database.SqlQueryRaw<string>(
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' ORDER BY table_name"
            ).ToListAsync();

            return Ok(new
            {
                Status = "OK",
                DatabaseConnection = true,
                TotalTables = tables.Count,
                Tables = tables
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database check failed");
            return StatusCode(500, new { Status = "Error", Message = ex.Message });
        }
    }

    private async Task<bool> CanConnectToDatabase()
    {
        try
        {
            return await _context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    private async Task<int> GetTotalUsuarios()
    {
        try
        {
            return await _context.Usuarios.CountAsync();
        }
        catch
        {
            return 0;
        }
    }
}