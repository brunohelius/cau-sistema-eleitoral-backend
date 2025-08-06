using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using SistemaEleitoral.Application.Services;

namespace SistemaEleitoral.Api.Middleware;

public class ElectoralAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ElectoralAuthorizationMiddleware> _logger;

    public ElectoralAuthorizationMiddleware(RequestDelegate next, ILogger<ElectoralAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService, IJwtService jwtService)
    {
        // Pular se não há autenticação necessária
        if (!RequireAuthorization(context))
        {
            await _next(context);
            return;
        }

        try
        {
            var token = ExtractToken(context.Request);
            
            if (string.IsNullOrEmpty(token))
            {
                await HandleUnauthorized(context, "Token não fornecido");
                return;
            }

            // Validar token JWT
            var principal = jwtService.ValidarToken(token);
            
            if (principal == null)
            {
                await HandleUnauthorized(context, "Token inválido");
                return;
            }

            // Verificar se sessão está ativa
            var jwtId = principal.FindFirst("jti")?.Value;
            if (!string.IsNullOrEmpty(jwtId))
            {
                var sessaoAtiva = await authService.SessaoAtivaAsync(jwtId);
                if (!sessaoAtiva)
                {
                    await HandleUnauthorized(context, "Sessão expirada");
                    return;
                }
            }

            // Verificar se token está próximo do vencimento
            if (jwtService.TokenProximoVencimento(token, 5))
            {
                context.Response.Headers.Append("X-Token-Refresh-Needed", "true");
            }

            // Adicionar claims personalizadas ao contexto
            context.User = principal;
            
            // Log da requisição autenticada
            var usuarioId = principal.FindFirst("user_id")?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            
            _logger.LogInformation("Usuário {UserId} ({Email}) acessando {Path}", 
                usuarioId, email, context.Request.Path);

            // Verificar contexto eleitoral se necessário
            await VerificarContextoEleitoral(context, principal);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no middleware de autorização");
            await HandleUnauthorized(context, "Erro interno de autorização");
        }
    }

    private bool RequireAuthorization(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        
        // Verificar se tem atributo [AllowAnonymous]
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            return false;
        }

        // Verificar se tem atributo [Authorize]
        if (endpoint?.Metadata?.GetMetadata<IAuthorizeData>() != null)
        {
            return true;
        }

        // Por padrão, rotas da API requerem autenticação
        return context.Request.Path.StartsWithSegments("/api");
    }

    private string? ExtractToken(HttpRequest request)
    {
        var authHeader = request.Headers.Authorization.FirstOrDefault();
        
        if (authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
        {
            return authHeader["Bearer ".Length..];
        }

        return null;
    }

    private async Task VerificarContextoEleitoral(HttpContext context, ClaimsPrincipal principal)
    {
        // Verificar se é uma rota que precisa de contexto eleitoral específico
        var path = context.Request.Path.Value?.ToLower();
        
        if (path == null) return;

        // Rotas que precisam de contexto UF
        if (path.Contains("/uf/") || path.Contains("/estadual/"))
        {
            var ufOrigem = principal.FindFirst("uf_origem")?.Value;
            var ufRequerida = ExtractUfFromPath(path);
            
            if (!string.IsNullOrEmpty(ufRequerida) && 
                !string.IsNullOrEmpty(ufOrigem) && 
                ufOrigem != ufRequerida)
            {
                // Verificar se usuário tem permissão nacional
                var nivelAcesso = principal.FindFirst("nivel_acesso")?.Value;
                if (nivelAcesso != "NACIONAL")
                {
                    await HandleForbidden(context, "Acesso não autorizado para esta UF");
                    return;
                }
            }
        }

        // Rotas que precisam de contexto de filial
        if (path.Contains("/filial/") || path.Contains("/regional/"))
        {
            var filialId = principal.FindFirst("filial_id")?.Value;
            var filialRequerida = ExtractFilialFromPath(path);
            
            if (!string.IsNullOrEmpty(filialRequerida) && 
                !string.IsNullOrEmpty(filialId) && 
                filialId != filialRequerida)
            {
                // Verificar se usuário tem permissão estadual ou nacional
                var nivelAcesso = principal.FindFirst("nivel_acesso")?.Value;
                if (nivelAcesso != "NACIONAL" && nivelAcesso != "ESTADUAL")
                {
                    await HandleForbidden(context, "Acesso não autorizado para esta filial");
                    return;
                }
            }
        }
    }

    private string? ExtractUfFromPath(string path)
    {
        // Extrair UF de caminhos como "/api/uf/SP/..." ou "/api/estadual/RJ/..."
        var segments = path.Split('/');
        
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i] == "uf" || segments[i] == "estadual")
            {
                return segments[i + 1].ToUpper();
            }
        }
        
        return null;
    }

    private string? ExtractFilialFromPath(string path)
    {
        // Extrair ID da filial de caminhos como "/api/filial/123/..." ou "/api/regional/456/..."
        var segments = path.Split('/');
        
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i] == "filial" || segments[i] == "regional")
            {
                return segments[i + 1];
            }
        }
        
        return null;
    }

    private async Task HandleUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        
        var response = new 
        { 
            success = false,
            message = message,
            timestamp = DateTime.UtcNow
        };
        
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }

    private async Task HandleForbidden(HttpContext context, string message)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        
        var response = new 
        { 
            success = false,
            message = message,
            timestamp = DateTime.UtcNow
        };
        
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}

public static class ElectoralAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseElectoralAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ElectoralAuthorizationMiddleware>();
    }
}