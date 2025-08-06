using System.Security.Claims;
using SistemaEleitoral.Application.DTOs.Authentication;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Application.Services;

public interface IJwtService
{
    /// <summary>
    /// Gera um token JWT para o usuário
    /// </summary>
    string GerarToken(ApplicationUser usuario, IEnumerable<string> roles, IEnumerable<string> permissoes);
    
    /// <summary>
    /// Gera um refresh token seguro
    /// </summary>
    string GerarRefreshToken();
    
    /// <summary>
    /// Valida um token JWT
    /// </summary>
    ClaimsPrincipal? ValidarToken(string token);
    
    /// <summary>
    /// Extrai claims do token
    /// </summary>
    Dictionary<string, string> ExtrairClaims(string token);
    
    /// <summary>
    /// Verifica se o token está próximo do vencimento
    /// </summary>
    bool TokenProximoVencimento(string token, int minutosAntes = 5);
    
    /// <summary>
    /// Obtém o JTI (JWT ID) do token
    /// </summary>
    string? ObterJwtId(string token);
    
    /// <summary>
    /// Obtém a data de expiração do token
    /// </summary>
    DateTime? ObterDataExpiracao(string token);
    
    /// <summary>
    /// Obtém o ID do usuário do token
    /// </summary>
    int? ObterApplicationUserId(string token);
}