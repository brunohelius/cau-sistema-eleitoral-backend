using System.ComponentModel.DataAnnotations;

namespace SistemaEleitoral.Application.DTOs.Authentication;

public class LoginDto
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    [MaxLength(150, ErrorMessage = "Email não pode exceder 150 caracteres")]
    public string Email { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Senha é obrigatória")]
    [MaxLength(100, ErrorMessage = "Senha não pode exceder 100 caracteres")]
    public string Senha { get; set; } = string.Empty;
    
    public bool ManterConectado { get; set; } = false;
    
    [MaxLength(50)]
    public string? EnderecoIp { get; set; }
    
    [MaxLength(200)]
    public string? UserAgent { get; set; }
}

public class LoginCAUDto
{
    [Required(ErrorMessage = "Token CAU é obrigatório")]
    public string TokenCAU { get; set; } = string.Empty;
    
    public bool ManterConectado { get; set; } = false;
    
    [MaxLength(50)]
    public string? EnderecoIp { get; set; }
    
    [MaxLength(200)]
    public string? UserAgent { get; set; }
}

public class RefreshTokenDto
{
    [Required(ErrorMessage = "Refresh token é obrigatório")]
    public string RefreshToken { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string? EnderecoIp { get; set; }
    
    [MaxLength(200)]
    public string? UserAgent { get; set; }
}

public class TokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } // Segundos até expiração
    public UsuarioTokenInfoDto Usuario { get; set; } = new();
}

public class UsuarioTokenInfoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? NumeroRegistro { get; set; }
    public string? UfOrigem { get; set; }
    public string? NivelAcesso { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissoes { get; set; } = new();
    public DateTime? DataUltimoLogin { get; set; }
    public bool EmailVerificado { get; set; }
}

public class LogoutDto
{
    public bool RevogarTodosSessions { get; set; } = false;
    
    [MaxLength(50)]
    public string? EnderecoIp { get; set; }
    
    [MaxLength(200)]
    public string? UserAgent { get; set; }
}

public class AlterarSenhaDto
{
    [Required(ErrorMessage = "Senha atual é obrigatória")]
    public string SenhaAtual { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [MinLength(8, ErrorMessage = "Nova senha deve ter pelo menos 8 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Nova senha deve conter pelo menos: 1 minúscula, 1 maiúscula, 1 número e 1 caractere especial")]
    public string NovaSenha { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Confirmação da senha é obrigatória")]
    [Compare(nameof(NovaSenha), ErrorMessage = "Confirmação da senha não confere")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}

public class RecuperarSenhaDto
{
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Formato de email inválido")]
    public string Email { get; set; } = string.Empty;
}

public class RedefinirSenhaDto
{
    [Required(ErrorMessage = "Token é obrigatório")]
    public string Token { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Nova senha é obrigatória")]
    [MinLength(8, ErrorMessage = "Nova senha deve ter pelo menos 8 caracteres")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]",
        ErrorMessage = "Nova senha deve conter pelo menos: 1 minúscula, 1 maiúscula, 1 número e 1 caractere especial")]
    public string NovaSenha { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Confirmação da senha é obrigatória")]
    [Compare(nameof(NovaSenha), ErrorMessage = "Confirmação da senha não confere")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}