using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
// Services removidos
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
        _issuer = _configuration["Jwt:Issuer"] ?? "SistemaEleitoralCAU";
        _audience = _configuration["Jwt:Audience"] ?? "SistemaEleitoralCAU";
        _expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
        
        // Validar tamanho da chave (mínimo 256 bits para HMAC-SHA256)
        if (Encoding.UTF8.GetBytes(_secretKey).Length < 32)
        {
            throw new InvalidOperationException("JWT SecretKey deve ter pelo menos 256 bits (32 caracteres)");
        }
        
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
    }

    public string GerarToken(ApplicationUser usuario, IEnumerable<string> roles, IEnumerable<string> permissoes)
    {
        var jwtId = Guid.NewGuid().ToString();
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_expirationMinutes);

        var claims = new List<Claim>
        {
            // Claims padrão
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.Name, usuario.Nome),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            
            // Claims específicas do sistema eleitoral
            new("user_id", usuario.Id.ToString()),
            new("email_verificado", usuario.EmailVerificado.ToString().ToLower()),
        };

        // Adicionar claims específicas do usuário
        if (!string.IsNullOrEmpty(usuario.NumeroRegistro))
            claims.Add(new Claim("numero_registro", usuario.NumeroRegistro));
            
        if (!string.IsNullOrEmpty(usuario.UfOrigem))
            claims.Add(new Claim("uf_origem", usuario.UfOrigem));
            
        if (!string.IsNullOrEmpty(usuario.NivelAcesso))
            claims.Add(new Claim("nivel_acesso", usuario.NivelAcesso));
            
        if (usuario.FilialId.HasValue)
            claims.Add(new Claim("filial_id", usuario.FilialId.Value.ToString()));

        // Adicionar roles
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role)); // Duplicate for easier access
        }

        // Adicionar permissões
        foreach (var permissao in permissoes)
        {
            claims.Add(new Claim("permission", permissao));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature),
            
            // Headers adicionais de segurança
            AdditionalHeaderClaims = new Dictionary<string, object>
            {
                { "typ", "JWT" },
                { "alg", "HS256" }
            }
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        return tokenHandler.WriteToken(token);
    }

    public string GerarRefreshToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[64];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? ValidarToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = ObterParametrosValidacao();
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            
            // Validações adicionais de segurança
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public Dictionary<string, string> ExtrairClaims(string token)
    {
        var claims = new Dictionary<string, string>();
        
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            
            foreach (var claim in jsonToken.Claims)
            {
                claims[claim.Type] = claim.Value;
            }
        }
        catch
        {
            // Token inválido - retorna dicionário vazio
        }
        
        return claims;
    }

    public bool TokenProximoVencimento(string token, int minutosAntes = 5)
    {
        var dataExpiracao = ObterDataExpiracao(token);
        
        if (!dataExpiracao.HasValue)
            return true; // Se não conseguir obter data, considera próximo do vencimento
            
        return DateTime.UtcNow.AddMinutes(minutosAntes) >= dataExpiracao.Value;
    }

    public string? ObterJwtId(string token)
    {
        var claims = ExtrairClaims(token);
        return claims.GetValueOrDefault(JwtRegisteredClaimNames.Jti);
    }

    public DateTime? ObterDataExpiracao(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            
            var expClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);
            if (expClaim != null && long.TryParse(expClaim.Value, out var exp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
            }
        }
        catch
        {
            // Token inválido
        }
        
        return null;
    }

    public int? ObterApplicationUserId(string token)
    {
        var claims = ExtrairClaims(token);
        var userIdClaim = claims.GetValueOrDefault("user_id") ?? claims.GetValueOrDefault(JwtRegisteredClaimNames.Sub);
        
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
            
        return null;
    }

    private TokenValidationParameters ObterParametrosValidacao()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateIssuer = true,
            ValidIssuer = _issuer,
            ValidateAudience = true,
            ValidAudience = _audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero, // Remove tolerância padrão de 5 minutos
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            
            // Validações de segurança adicionais
            RequireAudience = true,
            SaveSigninToken = false,
            
            // Algoritmos permitidos (apenas HMAC-SHA256)
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }
        };
    }
    
    // Métodos adicionais para compatibilidade com a interface
    public string GenerateJwtToken(ApplicationUser user)
    {
        return GerarToken(user, new[] { "User" }, new string[] { });
    }

    public string GenerateRefreshToken()
    {
        return GerarRefreshToken();
    }
    
    public bool ValidateToken(string token)
    {
        return ValidarToken(token) != null;
    }
}