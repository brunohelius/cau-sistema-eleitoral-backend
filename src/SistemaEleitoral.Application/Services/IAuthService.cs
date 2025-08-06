using SistemaEleitoral.Application.DTOs.Authentication;

namespace SistemaEleitoral.Application.Services;

public interface IAuthService
{
    /// <summary>
    /// Realiza login com email e senha
    /// </summary>
    Task<TokenResponseDto?> LoginAsync(LoginDto loginDto);
    
    /// <summary>
    /// Realiza login com token do sistema CAU
    /// </summary>
    Task<TokenResponseDto?> LoginCAUAsync(LoginCAUDto loginDto);
    
    /// <summary>
    /// Renova o token usando refresh token
    /// </summary>
    Task<TokenResponseDto?> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    
    /// <summary>
    /// Realiza logout do usuário
    /// </summary>
    Task<bool> LogoutAsync(int usuarioId, LogoutDto logoutDto);
    
    /// <summary>
    /// Altera senha do usuário
    /// </summary>
    Task<bool> AlterarSenhaAsync(int usuarioId, AlterarSenhaDto alterarSenhaDto);
    
    /// <summary>
    /// Inicia processo de recuperação de senha
    /// </summary>
    Task<bool> IniciarRecuperacaoSenhaAsync(RecuperarSenhaDto recuperarSenhaDto);
    
    /// <summary>
    /// Redefine senha usando token de recuperação
    /// </summary>
    Task<bool> RedefinirSenhaAsync(RedefinirSenhaDto redefinirSenhaDto);
    
    /// <summary>
    /// Verifica se uma sessão está ativa
    /// </summary>
    Task<bool> SessaoAtivaAsync(string jwtId);
    
    /// <summary>
    /// Revoga um refresh token específico
    /// </summary>
    Task<bool> RevogarRefreshTokenAsync(string refreshToken);
    
    /// <summary>
    /// Revoga todos os refresh tokens de um usuário
    /// </summary>
    Task<bool> RevogarTodosRefreshTokensAsync(int usuarioId);
    
    /// <summary>
    /// Valida se o usuário tem permissão para acessar determinado recurso
    /// </summary>
    Task<bool> ValidarPermissaoAsync(int usuarioId, string permissao, string? contexto = null);
    
    /// <summary>
    /// Obtém informações completas do usuário para o token
    /// </summary>
    Task<UsuarioTokenInfoDto?> ObterInformacoesUsuarioAsync(int usuarioId);
}