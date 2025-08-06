using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SistemaEleitoral.Application.Services;
using System.Security.Claims;

namespace SistemaEleitoral.Api.Attributes;

/// <summary>
/// Atributo para exigir permissões específicas em controllers e actions
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;
    private readonly string? _context;
    private readonly bool _requireAll;

    /// <summary>
    /// Exige uma permissão específica
    /// </summary>
    /// <param name="permission">Código da permissão necessária</param>
    /// <param name="context">Contexto da permissão (NACIONAL, ESTADUAL, REGIONAL)</param>
    public RequirePermissionAttribute(string permission, string? context = null)
    {
        _permission = permission;
        _context = context;
        _requireAll = false;
    }

    /// <summary>
    /// Exige múltiplas permissões
    /// </summary>
    /// <param name="permissions">Array de permissões necessárias</param>
    /// <param name="requireAll">Se true, exige todas as permissões. Se false, exige apenas uma</param>
    /// <param name="context">Contexto da permissão</param>
    public RequirePermissionAttribute(string[] permissions, bool requireAll = true, string? context = null)
    {
        _permission = string.Join("|", permissions);
        _context = context;
        _requireAll = requireAll;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Verificar se usuário está autenticado
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedObjectResult(new 
            { 
                success = false,
                message = "Usuário não autenticado",
                requiredPermission = _permission
            });
            return;
        }

        var authService = context.HttpContext.RequestServices.GetService<IAuthService>();
        if (authService == null)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        try
        {
            var userId = GetUserId(context.HttpContext.User);
            if (userId == null)
            {
                context.Result = new UnauthorizedObjectResult(new 
                { 
                    success = false,
                    message = "ID do usuário não encontrado no token"
                });
                return;
            }

            var permissions = _permission.Split('|');
            var hasPermission = false;

            if (_requireAll)
            {
                // Verificar se tem TODAS as permissões
                hasPermission = true;
                foreach (var perm in permissions)
                {
                    if (!await authService.ValidarPermissaoAsync(userId.Value, perm, _context))
                    {
                        hasPermission = false;
                        break;
                    }
                }
            }
            else
            {
                // Verificar se tem PELO MENOS UMA das permissões
                foreach (var perm in permissions)
                {
                    if (await authService.ValidarPermissaoAsync(userId.Value, perm, _context))
                    {
                        hasPermission = true;
                        break;
                    }
                }
            }

            // Verificar contexto específico se necessário
            if (hasPermission && !string.IsNullOrEmpty(_context))
            {
                hasPermission = await ValidateContext(context.HttpContext, userId.Value, _context);
            }

            if (!hasPermission)
            {
                context.Result = new ForbidResult();
                // Ou para retornar JSON customizado:
                context.Result = new ObjectResult(new 
                { 
                    success = false,
                    message = "Permissão insuficiente para esta operação",
                    requiredPermission = _permission,
                    requiredContext = _context
                })
                {
                    StatusCode = 403
                };
            }
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RequirePermissionAttribute>>();
            logger?.LogError(ex, "Erro durante validação de permissão");
            
            context.Result = new StatusCodeResult(500);
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

    private async Task<bool> ValidateContext(HttpContext httpContext, int userId, string requiredContext)
    {
        var userContext = httpContext.User.FindFirst("nivel_acesso")?.Value;
        
        return requiredContext.ToUpper() switch
        {
            "NACIONAL" => userContext == "NACIONAL",
            "ESTADUAL" => userContext is "NACIONAL" or "ESTADUAL",
            "REGIONAL" => userContext is "NACIONAL" or "ESTADUAL" or "REGIONAL",
            _ => true // Contexto desconhecido permite acesso
        };
    }
}

/// <summary>
/// Atributo para exigir roles específicas
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;
    private readonly bool _requireAll;

    /// <summary>
    /// Exige uma role específica
    /// </summary>
    public RequireRoleAttribute(string role)
    {
        _roles = new[] { role };
        _requireAll = false;
    }

    /// <summary>
    /// Exige múltiplas roles
    /// </summary>
    public RequireRoleAttribute(string[] roles, bool requireAll = false)
    {
        _roles = roles;
        _requireAll = requireAll;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.User.Identity?.IsAuthenticated == true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var userRoles = context.HttpContext.User.FindAll("role")
            .Select(c => c.Value)
            .ToList();

        bool hasRequiredRole;

        if (_requireAll)
        {
            hasRequiredRole = _roles.All(role => userRoles.Contains(role));
        }
        else
        {
            hasRequiredRole = _roles.Any(role => userRoles.Contains(role));
        }

        if (!hasRequiredRole)
        {
            context.Result = new ObjectResult(new 
            { 
                success = false,
                message = "Role insuficiente para esta operação",
                requiredRoles = _roles,
                userRoles = userRoles
            })
            {
                StatusCode = 403
            };
        }
    }
}