using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Infrastructure.Data;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Infrastructure.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<List<RefreshToken>> GetActiveTokensByUserAsync(int userId);
    Task RevokeAllUserTokensAsync(int userId);
    Task<List<RefreshToken>> GetExpiredTokensAsync();
    Task CleanupExpiredTokensAsync();
}

public interface ISessaoLoginRepository : IRepository<SessaoLogin>
{
    Task<SessaoLogin?> GetByJwtIdAsync(string jwtId);
    Task<List<SessaoLogin>> GetActiveSessionsByUserAsync(int userId);
    Task EndAllUserSessionsAsync(int userId);
    Task<List<SessaoLogin>> GetExpiredSessionsAsync();
    Task CleanupExpiredSessionsAsync();
}

public interface ILogUsuarioRepository : IRepository<LogUsuario>
{
    Task<List<LogUsuario>> GetLogsByUserAsync(int userId, int pageSize = 50, int pageNumber = 1);
    Task<List<LogUsuario>> GetLogsByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<LogUsuario>> GetSecurityLogsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<Dictionary<string, int>> GetLoginAttemptsStatsAsync(DateTime? startDate = null);
}

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContextMinimal context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.Usuario)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<List<RefreshToken>> GetActiveTokensByUserAsync(int userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UsuarioId == userId && 
                        !rt.Revogado && 
                        rt.DataExpiracao > DateTime.UtcNow)
            .OrderByDescending(rt => rt.DataCriacao)
            .ToListAsync();
    }

    public async Task RevokeAllUserTokensAsync(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UsuarioId == userId && !rt.Revogado)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Revogado = true;
            token.DataRevogacao = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<RefreshToken>> GetExpiredTokensAsync()
    {
        return await _context.RefreshTokens
            .Where(rt => rt.DataExpiracao < DateTime.UtcNow || rt.Revogado)
            .ToListAsync();
    }

    public async Task CleanupExpiredTokensAsync()
    {
        // Remove tokens expirados há mais de 30 dias
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.DataExpiracao < cutoffDate || 
                        (rt.Revogado && rt.DataRevogacao < cutoffDate))
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync();
    }
}

public class SessaoLoginRepository : BaseRepository<SessaoLogin>, ISessaoLoginRepository
{
    public SessaoLoginRepository(ApplicationDbContextMinimal context) : base(context)
    {
    }

    public async Task<SessaoLogin?> GetByJwtIdAsync(string jwtId)
    {
        return await _context.SessoesLogin
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.JwtId == jwtId);
    }

    public async Task<List<SessaoLogin>> GetActiveSessionsByUserAsync(int userId)
    {
        return await _context.SessoesLogin
            .Where(s => s.UsuarioId == userId && s.SessaoAtiva)
            .OrderByDescending(s => s.DataInicio)
            .ToListAsync();
    }

    public async Task EndAllUserSessionsAsync(int userId)
    {
        var sessions = await _context.SessoesLogin
            .Where(s => s.UsuarioId == userId && s.SessaoAtiva)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.SessaoAtiva = false;
            session.DataFim = DateTime.UtcNow;
            session.TipoLogout = "ADMIN";
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<SessaoLogin>> GetExpiredSessionsAsync()
    {
        // Sessões consideradas expiradas após 24 horas de inatividade
        var cutoffDate = DateTime.UtcNow.AddHours(-24);
        
        return await _context.SessoesLogin
            .Where(s => s.SessaoAtiva && s.DataInicio < cutoffDate)
            .ToListAsync();
    }

    public async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await GetExpiredSessionsAsync();
        
        foreach (var session in expiredSessions)
        {
            session.SessaoAtiva = false;
            session.DataFim = DateTime.UtcNow;
            session.TipoLogout = "EXPIRACAO";
        }

        await _context.SaveChangesAsync();
    }
}

public class LogUsuarioRepository : BaseRepository<LogUsuario>, ILogUsuarioRepository
{
    public LogUsuarioRepository(ApplicationDbContextMinimal context) : base(context)
    {
    }

    public async Task<List<LogUsuario>> GetLogsByUserAsync(int userId, int pageSize = 50, int pageNumber = 1)
    {
        return await _context.LogsUsuario
            .Where(l => l.UsuarioId == userId)
            .OrderByDescending(l => l.DataCriacao)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<LogUsuario>> GetLogsByActionAsync(string action, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.LogsUsuario
            .Where(l => l.Acao == action);

        if (startDate.HasValue)
            query = query.Where(l => l.DataCriacao >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.DataCriacao <= endDate.Value);

        return await query
            .OrderByDescending(l => l.DataCriacao)
            .ToListAsync();
    }

    public async Task<List<LogUsuario>> GetSecurityLogsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var securityActions = new[] 
        { 
            "LOGIN_SUCESSO", 
            "LOGIN_FALHA", 
            "LOGOUT", 
            "ALTERACAO_SENHA",
            "TENTATIVA_ACESSO_NEGADO",
            "TOKEN_REVOGADO"
        };

        var query = _context.LogsUsuario
            .Where(l => securityActions.Contains(l.Acao));

        if (startDate.HasValue)
            query = query.Where(l => l.DataCriacao >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.DataCriacao <= endDate.Value);

        return await query
            .Include(l => l.Usuario)
            .OrderByDescending(l => l.DataCriacao)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetLoginAttemptsStatsAsync(DateTime? startDate = null)
    {
        startDate ??= DateTime.UtcNow.AddDays(-30);

        var stats = await _context.LogsUsuario
            .Where(l => l.DataCriacao >= startDate.Value && 
                       (l.Acao == "LOGIN_SUCESSO" || l.Acao == "LOGIN_FALHA"))
            .GroupBy(l => l.Acao)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Action, x => x.Count);

        return stats;
    }
}

// Service para limpeza automática de dados sensíveis
public interface ISecurityCleanupService
{
    Task CleanupExpiredDataAsync();
    Task GenerateSecurityReportAsync();
    Task ArchiveOldLogsAsync();
}

public class SecurityCleanupService : ISecurityCleanupService
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISessaoLoginRepository _sessaoLoginRepository;
    private readonly ILogUsuarioRepository _logUsuarioRepository;
    private readonly ILogger<SecurityCleanupService> _logger;

    public SecurityCleanupService(
        IRefreshTokenRepository refreshTokenRepository,
        ISessaoLoginRepository sessaoLoginRepository,
        ILogUsuarioRepository logUsuarioRepository,
        ILogger<SecurityCleanupService> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _sessaoLoginRepository = sessaoLoginRepository;
        _logUsuarioRepository = logUsuarioRepository;
        _logger = logger;
    }

    public async Task CleanupExpiredDataAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando limpeza de dados de segurança expirados");

            // Limpar tokens expirados
            await _refreshTokenRepository.CleanupExpiredTokensAsync();
            _logger.LogInformation("Tokens expirados removidos");

            // Limpar sessões expiradas
            await _sessaoLoginRepository.CleanupExpiredSessionsAsync();
            _logger.LogInformation("Sessões expiradas finalizadas");

            _logger.LogInformation("Limpeza de dados de segurança concluída");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante limpeza de dados de segurança");
            throw;
        }
    }

    public async Task GenerateSecurityReportAsync()
    {
        try
        {
            _logger.LogInformation("Gerando relatório de segurança");

            var last30Days = DateTime.UtcNow.AddDays(-30);
            
            // Estatísticas de login
            var loginStats = await _logUsuarioRepository.GetLoginAttemptsStatsAsync(last30Days);
            
            // Logs de segurança recentes
            var securityLogs = await _logUsuarioRepository.GetSecurityLogsAsync(last30Days);

            _logger.LogInformation("Relatório de segurança - Últimos 30 dias:");
            _logger.LogInformation("- Logins bem-sucedidos: {SuccessCount}", 
                loginStats.GetValueOrDefault("LOGIN_SUCESSO", 0));
            _logger.LogInformation("- Tentativas de login falhas: {FailureCount}", 
                loginStats.GetValueOrDefault("LOGIN_FALHA", 0));
            _logger.LogInformation("- Total de eventos de segurança: {TotalEvents}", 
                securityLogs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante geração do relatório de segurança");
        }
    }

    public async Task ArchiveOldLogsAsync()
    {
        try
        {
            // Arquivar logs com mais de 1 ano (implementação futura)
            _logger.LogInformation("Arquivamento de logs antigos não implementado ainda");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante arquivamento de logs");
        }
    }
}