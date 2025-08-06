using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace SistemaEleitoral.Api.Attributes;

/// <summary>
/// Atributos específicos para permissões do sistema eleitoral
/// </summary>
public static class ElectoralPermissions
{
    // Permissões gerais
    public const string VISUALIZAR_CALENDARIO = "CALENDARIO_VISUALIZAR";
    public const string GERENCIAR_CALENDARIO = "CALENDARIO_GERENCIAR";
    
    // Permissões de chapas
    public const string CRIAR_CHAPA = "CHAPA_CRIAR";
    public const string EDITAR_CHAPA = "CHAPA_EDITAR";
    public const string VISUALIZAR_CHAPA = "CHAPA_VISUALIZAR";
    public const string CONFIRMAR_CHAPA = "CHAPA_CONFIRMAR";
    
    // Permissões de comissão eleitoral
    public const string GERENCIAR_COMISSAO = "COMISSAO_GERENCIAR";
    public const string JULGAR_PROCESSOS = "PROCESSOS_JULGAR";
    public const string CRIAR_IMPUGNACAO = "IMPUGNACAO_CRIAR";
    public const string JULGAR_IMPUGNACAO = "IMPUGNACAO_JULGAR";
    
    // Permissões de denúncias
    public const string CRIAR_DENUNCIA = "DENUNCIA_CRIAR";
    public const string JULGAR_DENUNCIA = "DENUNCIA_JULGAR";
    public const string RELATAR_DENUNCIA = "DENUNCIA_RELATAR";
    
    // Permissões administrativas
    public const string ADMIN_SISTEMA = "ADMIN_SISTEMA";
    public const string ADMIN_UF = "ADMIN_UF";
    public const string ADMIN_FILIAL = "ADMIN_FILIAL";
    
    // Permissões de relatórios
    public const string RELATORIOS_GERENCIAIS = "RELATORIOS_GERENCIAIS";
    public const string RELATORIOS_AUDITORIA = "RELATORIOS_AUDITORIA";
}

/// <summary>
/// Roles específicas do sistema eleitoral
/// </summary>
public static class ElectoralRoles
{
    public const string PROFISSIONAL = "PROFISSIONAL";
    public const string MEMBRO_COMISSAO = "MEMBRO_COMISSAO";
    public const string COORDENADOR_COMISSAO = "COORDENADOR_COMISSAO";
    public const string RELATOR = "RELATOR";
    public const string ADMINISTRADOR = "ADMINISTRADOR";
    public const string SUPER_ADMIN = "SUPER_ADMIN";
}

/// <summary>
/// Atributo para permissões específicas de chapa eleitoral
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireChapaPermissionAttribute : RequirePermissionAttribute
{
    public RequireChapaPermissionAttribute(string action) 
        : base($"CHAPA_{action.ToUpper()}")
    {
    }
}

/// <summary>
/// Atributo para permissões de comissão eleitoral
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireComissaoPermissionAttribute : RequirePermissionAttribute
{
    public RequireComissaoPermissionAttribute(string action, string? context = null) 
        : base($"COMISSAO_{action.ToUpper()}", context)
    {
    }
}

/// <summary>
/// Atributo para validar acesso baseado no contexto eleitoral (UF/Nacional)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ValidateElectoralContextAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _requiredContext;
    private readonly string? _specificUf;

    /// <summary>
    /// Valida contexto eleitoral
    /// </summary>
    /// <param name="requiredContext">NACIONAL, ESTADUAL, ou REGIONAL</param>
    /// <param name="specificUf">UF específica (opcional)</param>
    public ValidateElectoralContextAttribute(string requiredContext, string? specificUf = null)
    {
        _requiredContext = requiredContext.ToUpper();
        _specificUf = specificUf?.ToUpper();
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userLevel = context.HttpContext.User.FindFirst("nivel_acesso")?.Value;
        var userUf = context.HttpContext.User.FindFirst("uf_origem")?.Value;

        // Verificar nível de acesso
        bool hasAccess = _requiredContext switch
        {
            "NACIONAL" => userLevel == "NACIONAL",
            "ESTADUAL" => userLevel is "NACIONAL" or "ESTADUAL",
            "REGIONAL" => userLevel is "NACIONAL" or "ESTADUAL" or "REGIONAL",
            _ => false
        };

        // Verificar UF específica se necessária
        if (hasAccess && !string.IsNullOrEmpty(_specificUf))
        {
            if (userLevel != "NACIONAL" && userUf != _specificUf)
            {
                hasAccess = false;
            }
        }

        if (!hasAccess)
        {
            context.Result = new ObjectResult(new 
            { 
                success = false,
                message = "Acesso não autorizado para este contexto eleitoral",
                requiredContext = _requiredContext,
                requiredUf = _specificUf,
                userContext = userLevel,
                userUf = userUf
            })
            {
                StatusCode = 403
            };
        }
    }
}

/// <summary>
/// Atributo para validar acesso durante período eleitoral
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireElectoralPeriodAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _activityType;
    private readonly bool _allowOutsidePeriod;

    public RequireElectoralPeriodAttribute(string activityType, bool allowOutsidePeriod = false)
    {
        _activityType = activityType;
        _allowOutsidePeriod = allowOutsidePeriod;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // TODO: Implementar validação de período eleitoral
        // Por enquanto, apenas continua a execução
        
        var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireElectoralPeriodAttribute>>();
        logger?.LogInformation("Validando período eleitoral para atividade: {ActivityType}", _activityType);

        if (!_allowOutsidePeriod)
        {
            // TODO: Verificar se estamos no período adequado para a atividade
            // Exemplo: período de cadastro de chapas, período de impugnações, etc.
        }

        await next();
    }
}

/// <summary>
/// Atributo combinado para operações específicas do sistema eleitoral
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ElectoralOperationAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _operation;
    private readonly string _context;
    private readonly string? _requiredRole;
    private readonly string? _requiredPermission;

    public ElectoralOperationAttribute(
        string operation, 
        string context = "REGIONAL",
        string? requiredRole = null,
        string? requiredPermission = null)
    {
        _operation = operation;
        _context = context;
        _requiredRole = requiredRole;
        _requiredPermission = requiredPermission;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var validationResults = new List<string>();

        // Validar role se especificada
        if (!string.IsNullOrEmpty(_requiredRole))
        {
            var userRoles = context.HttpContext.User.FindAll("role").Select(c => c.Value).ToList();
            if (!userRoles.Contains(_requiredRole))
            {
                validationResults.Add($"Role '{_requiredRole}' necessária");
            }
        }

        // Validar permissão se especificada
        if (!string.IsNullOrEmpty(_requiredPermission))
        {
            var authService = context.HttpContext.RequestServices.GetService<IAuthService>();
            if (authService != null)
            {
                var userId = GetUserId(context.HttpContext.User);
                if (userId.HasValue)
                {
                    var hasPermission = await authService.ValidarPermissaoAsync(userId.Value, _requiredPermission);
                    if (!hasPermission)
                    {
                        validationResults.Add($"Permissão '{_requiredPermission}' necessária");
                    }
                }
            }
        }

        // Validar contexto
        var userLevel = context.HttpContext.User.FindFirst("nivel_acesso")?.Value;
        bool hasContextAccess = _context.ToUpper() switch
        {
            "NACIONAL" => userLevel == "NACIONAL",
            "ESTADUAL" => userLevel is "NACIONAL" or "ESTADUAL",
            "REGIONAL" => userLevel is "NACIONAL" or "ESTADUAL" or "REGIONAL",
            _ => true
        };

        if (!hasContextAccess)
        {
            validationResults.Add($"Nível de acesso '{_context}' necessário");
        }

        if (validationResults.Any())
        {
            context.Result = new ObjectResult(new 
            { 
                success = false,
                message = "Acesso negado para a operação solicitada",
                operation = _operation,
                validationErrors = validationResults
            })
            {
                StatusCode = 403
            };
        }
    }

    private int? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst("user_id")?.Value ?? 
                         user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
            
        return null;
    }
}