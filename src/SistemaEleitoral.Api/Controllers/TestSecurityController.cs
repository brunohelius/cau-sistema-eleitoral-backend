using Microsoft.AspNetCore.Mvc;
using SistemaEleitoral.Api.Attributes;

namespace SistemaEleitoral.Api.Controllers;

/// <summary>
/// Controller para demonstrar e testar os recursos de segurança implementados
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestSecurityController : ControllerBase
{
    /// <summary>
    /// Endpoint público para testar rate limiting
    /// </summary>
    [HttpGet("public")]
    [EnableRateLimiting("AuthPolicy")]
    public IActionResult PublicEndpoint()
    {
        return Ok(new { 
            message = "Endpoint público acessível",
            timestamp = DateTime.UtcNow,
            success = true
        });
    }

    /// <summary>
    /// Endpoint que requer apenas autenticação
    /// </summary>
    [HttpGet("authenticated")]
    [RequireRole(ElectoralRoles.PROFISSIONAL)]
    public IActionResult AuthenticatedEndpoint()
    {
        var userId = User.FindFirst("user_id")?.Value;
        var email = User.FindFirst("email")?.Value;
        
        return Ok(new { 
            message = "Endpoint autenticado",
            userId = userId,
            email = email,
            timestamp = DateTime.UtcNow,
            success = true
        });
    }

    /// <summary>
    /// Endpoint que requer permissão específica
    /// </summary>
    [HttpGet("chapa/create")]
    [RequirePermission(ElectoralPermissions.CRIAR_CHAPA)]
    public IActionResult CreateChapaTest()
    {
        return Ok(new { 
            message = "Usuário autorizado a criar chapas",
            permission = ElectoralPermissions.CRIAR_CHAPA,
            success = true
        });
    }

    /// <summary>
    /// Endpoint que requer múltiplas permissões (ALL)
    /// </summary>
    [HttpGet("admin/manage")]
    [RequirePermission(new[] { ElectoralPermissions.ADMIN_SISTEMA, ElectoralPermissions.GERENCIAR_COMISSAO }, requireAll: true)]
    public IActionResult AdminManageTest()
    {
        return Ok(new { 
            message = "Usuário com permissões administrativas completas",
            requiredPermissions = new[] { ElectoralPermissions.ADMIN_SISTEMA, ElectoralPermissions.GERENCIAR_COMISSAO },
            success = true
        });
    }

    /// <summary>
    /// Endpoint que requer múltiplas permissões (ANY)
    /// </summary>
    [HttpGet("comissao/access")]
    [RequirePermission(new[] { ElectoralPermissions.GERENCIAR_COMISSAO, ElectoralPermissions.JULGAR_PROCESSOS }, requireAll: false)]
    public IActionResult ComissaoAccessTest()
    {
        return Ok(new { 
            message = "Usuário com acesso à comissão eleitoral",
            anyOfPermissions = new[] { ElectoralPermissions.GERENCIAR_COMISSAO, ElectoralPermissions.JULGAR_PROCESSOS },
            success = true
        });
    }

    /// <summary>
    /// Endpoint que valida contexto UF
    /// </summary>
    [HttpGet("uf/{uf}/operations")]
    [ValidateElectoralContext("ESTADUAL")]
    [RequireRole(ElectoralRoles.MEMBRO_COMISSAO)]
    public IActionResult UfOperationsTest(string uf)
    {
        var userUf = User.FindFirst("uf_origem")?.Value;
        var nivelAcesso = User.FindFirst("nivel_acesso")?.Value;
        
        return Ok(new { 
            message = $"Acesso autorizado para operações em {uf.ToUpper()}",
            requestedUf = uf.ToUpper(),
            userUf = userUf,
            nivelAcesso = nivelAcesso,
            success = true
        });
    }

    /// <summary>
    /// Endpoint nacional - apenas usuários nacionais
    /// </summary>
    [HttpGet("nacional/reports")]
    [ValidateElectoralContext("NACIONAL")]
    [RequirePermission(ElectoralPermissions.RELATORIOS_GERENCIAIS)]
    public IActionResult NacionalReportsTest()
    {
        return Ok(new { 
            message = "Relatórios nacionais acessíveis",
            context = "NACIONAL",
            success = true
        });
    }

    /// <summary>
    /// Endpoint que combina múltiplas validações
    /// </summary>
    [HttpPost("denuncia/julgar")]
    [ElectoralOperation("JULGAR_DENUNCIA", "ESTADUAL", ElectoralRoles.RELATOR, ElectoralPermissions.JULGAR_DENUNCIA)]
    public IActionResult JulgarDenunciaTest()
    {
        var userRoles = User.FindAll("role").Select(c => c.Value).ToList();
        var nivelAcesso = User.FindFirst("nivel_acesso")?.Value;
        
        return Ok(new { 
            message = "Autorizado para julgar denúncias",
            operation = "JULGAR_DENUNCIA",
            userRoles = userRoles,
            nivelAcesso = nivelAcesso,
            success = true
        });
    }

    /// <summary>
    /// Endpoint para testar período eleitoral (demonstração)
    /// </summary>
    [HttpPost("chapa/register")]
    [RequireElectoralPeriod("CADASTRO_CHAPAS", allowOutsidePeriod: true)]
    [RequirePermission(ElectoralPermissions.CRIAR_CHAPA)]
    public IActionResult RegisterChapaTest()
    {
        return Ok(new { 
            message = "Registro de chapa autorizado",
            period = "CADASTRO_CHAPAS",
            note = "Período eleitoral não implementado ainda - sempre permite",
            success = true
        });
    }

    /// <summary>
    /// Endpoint de teste de segurança geral
    /// </summary>
    [HttpGet("security-test")]
    public IActionResult SecurityTest()
    {
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        
        return Ok(new { 
            message = "Teste de segurança",
            isAuthenticated = isAuthenticated,
            claims = isAuthenticated ? claims : new Dictionary<string, string>(),
            headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            ip = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent = Request.Headers.UserAgent.FirstOrDefault(),
            success = true
        });
    }
}