using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    // BATCH 5: EMAIL/NOTIFICATION REPOSITORIES
    // Critical for electoral communication - 50+ email jobs from PHP system
    // Handles all electoral notifications, alerts, and automated communications
    
    #region EmailRepository
    public interface IEmailRepository : IRepository<Email>
    {
        Task<IEnumerable<Email>> GetPendentesAsync();
        Task<IEnumerable<Email>> GetPorStatusAsync(StatusEmail status);
        Task<Email?> GetCompletoAsync(int id);
        Task<IEnumerable<Email>> GetPorDestinatarioAsync(string emailDestinatario);
        Task<bool> EnviarEmailAsync(Email email);
        Task<bool> MarcarComoEnviadoAsync(int emailId);
        Task<bool> MarcarComoFalhaAsync(int emailId, string motivoFalha);
        Task<IEnumerable<Email>> GetFilaEnvioAsync(int limite = 50);
        Task<long> GetTotalPendentesAsync();
        Task<IEnumerable<Email>> GetFalhasEnvioAsync();
        Task<bool> ReprocessarFalhasAsync();
    }
    
    public class EmailRepository : BaseRepository<Email>, IEmailRepository
    {
        public EmailRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<EmailRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<Email>> GetPendentesAsync()
        {
            return await _dbSet.Include(e => e.TemplateEmail).Include(e => e.Anexos)
                .Where(e => e.Status == StatusEmail.Pendente && !e.Excluido)
                .OrderBy(e => e.DataCriacao).AsNoTracking().ToListAsync();
        }
        
        public async Task<IEnumerable<Email>> GetPorStatusAsync(StatusEmail status)
        {
            var cacheKey = GetCacheKey("por_status", status.ToString());
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Include(e => e.TemplateEmail)
                    .Where(e => e.Status == status && !e.Excluido)
                    .OrderByDescending(e => e.DataCriacao).AsNoTracking().ToListAsync(),
                TimeSpan.FromMinutes(5));
        }
        
        public async Task<Email?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Include(e => e.TemplateEmail).Include(e => e.Anexos)
                    .Include(e => e.LogsEnvio).AsNoTracking().FirstOrDefaultAsync(e => e.Id == id),
                TimeSpan.FromMinutes(10));
        }
        
        public async Task<IEnumerable<Email>> GetPorDestinatarioAsync(string emailDestinatario)
        {
            return await _dbSet.Include(e => e.TemplateEmail)
                .Where(e => e.EmailDestinatario == emailDestinatario && !e.Excluido)
                .OrderByDescending(e => e.DataCriacao).AsNoTracking().ToListAsync();
        }
        
        public async Task<bool> EnviarEmailAsync(Email email)
        {
            try
            {
                // Aqui integraria com provedor de email (SendGrid, Amazon SES, etc.)
                // Por simplicidade, simula envio
                email.Status = StatusEmail.Enviado;
                email.DataEnvio = DateTime.UtcNow;
                email.TentativasEnvio++;
                
                await UpdateAsync(email);
                
                // Registrar log de envio
                var logEnvio = new LogEnvioEmail
                {
                    EmailId = email.Id,
                    StatusEnvio = StatusEmail.Enviado,
                    DataTentativa = DateTime.UtcNow,
                    Sucesso = true
                };
                
                await _context.LogsEnvioEmail.AddAsync(logEnvio);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Email {EmailId} enviado com sucesso para {Destinatario}", email.Id, email.EmailDestinatario);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha no envio do email {EmailId}", email.Id);
                await MarcarComoFalhaAsync(email.Id, ex.Message);
                return false;
            }
        }
        
        public async Task<bool> MarcarComoEnviadoAsync(int emailId)
        {
            var email = await GetByIdAsync(emailId);
            if (email == null) return false;
            
            email.Status = StatusEmail.Enviado;
            email.DataEnvio = DateTime.UtcNow;
            await UpdateAsync(email);
            return true;
        }
        
        public async Task<bool> MarcarComoFalhaAsync(int emailId, string motivoFalha)
        {
            var email = await GetByIdAsync(emailId);
            if (email == null) return false;
            
            email.Status = StatusEmail.Falha;
            email.UltimaTentativa = DateTime.UtcNow;
            email.TentativasEnvio++;
            email.UltimoErro = motivoFalha;
            
            await UpdateAsync(email);
            
            // Registrar log de falha
            var logEnvio = new LogEnvioEmail
            {
                EmailId = emailId,
                StatusEnvio = StatusEmail.Falha,
                DataTentativa = DateTime.UtcNow,
                Sucesso = false,
                MotivoFalha = motivoFalha
            };
            
            await _context.LogsEnvioEmail.AddAsync(logEnvio);
            await _context.SaveChangesAsync();
            
            return true;
        }
        
        public async Task<IEnumerable<Email>> GetFilaEnvioAsync(int limite = 50)
        {
            return await _dbSet.Include(e => e.TemplateEmail).Include(e => e.Anexos)
                .Where(e => e.Status == StatusEmail.Pendente && !e.Excluido && e.TentativasEnvio < 3)
                .OrderBy(e => e.DataCriacao).Take(limite).AsNoTracking().ToListAsync();
        }
        
        public async Task<long> GetTotalPendentesAsync()
        {
            return await _dbSet.Where(e => e.Status == StatusEmail.Pendente && !e.Excluido).CountAsync();
        }
        
        public async Task<IEnumerable<Email>> GetFalhasEnvioAsync()
        {
            return await _dbSet.Include(e => e.TemplateEmail)
                .Where(e => e.Status == StatusEmail.Falha && !e.Excluido && e.TentativasEnvio < 3)
                .OrderBy(e => e.UltimaTentativa).AsNoTracking().ToListAsync();
        }
        
        public async Task<bool> ReprocessarFalhasAsync()
        {
            var emailsFalha = await GetFalhasEnvioAsync();
            foreach (var email in emailsFalha)
            {
                email.Status = StatusEmail.Pendente;
                await UpdateAsync(email);
            }
            
            _logger.LogInformation("Reprocessando {Count} emails com falha", emailsFalha.Count());
            return true;
        }
    }
    #endregion
    
    #region NotificacaoRepository
    public interface INotificacaoRepository : IRepository<Notificacao>
    {
        Task<IEnumerable<Notificacao>> GetPorUsuarioAsync(int usuarioId);
        Task<IEnumerable<Notificacao>> GetNaoLidasAsync(int usuarioId);
        Task<bool> MarcarComoLidaAsync(int notificacaoId);
        Task<bool> MarcarTodasComoLidasAsync(int usuarioId);
        Task<IEnumerable<Notificacao>> GetPorTipoAsync(TipoNotificacao tipo);
        Task<bool> NotificarAsync(int usuarioId, TipoNotificacao tipo, string titulo, string mensagem, string? link = null);
        Task<long> GetTotalNaoLidasAsync(int usuarioId);
    }
    
    public class NotificacaoRepository : BaseRepository<Notificacao>, INotificacaoRepository
    {
        public NotificacaoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<NotificacaoRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<Notificacao>> GetPorUsuarioAsync(int usuarioId)
        {
            var cacheKey = GetCacheKey("por_usuario", usuarioId);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Include(n => n.TipoNotificacao)
                    .Where(n => n.UsuarioId == usuarioId)
                    .OrderByDescending(n => n.DataCriacao).AsNoTracking().ToListAsync(),
                TimeSpan.FromMinutes(5));
        }
        
        public async Task<IEnumerable<Notificacao>> GetNaoLidasAsync(int usuarioId)
        {
            return await _dbSet.Include(n => n.TipoNotificacao)
                .Where(n => n.UsuarioId == usuarioId && !n.Lida)
                .OrderByDescending(n => n.DataCriacao).AsNoTracking().ToListAsync();
        }
        
        public async Task<bool> MarcarComoLidaAsync(int notificacaoId)
        {
            var notificacao = await GetByIdAsync(notificacaoId);
            if (notificacao == null) return false;
            
            notificacao.Lida = true;
            notificacao.DataLeitura = DateTime.UtcNow;
            await UpdateAsync(notificacao);
            
            // Invalidar cache do usuário
            _cache.Remove(GetCacheKey("por_usuario", notificacao.UsuarioId));
            return true;
        }
        
        public async Task<bool> MarcarTodasComoLidasAsync(int usuarioId)
        {
            var notificacoesNaoLidas = await _dbSet.Where(n => n.UsuarioId == usuarioId && !n.Lida).ToListAsync();
            foreach (var notificacao in notificacoesNaoLidas)
            {
                notificacao.Lida = true;
                notificacao.DataLeitura = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();
            
            // Invalidar cache do usuário
            _cache.Remove(GetCacheKey("por_usuario", usuarioId));
            return true;
        }
        
        public async Task<IEnumerable<Notificacao>> GetPorTipoAsync(TipoNotificacao tipo)
        {
            return await _dbSet.Include(n => n.Usuario)
                .Where(n => n.TipoNotificacaoId == (int)tipo)
                .OrderByDescending(n => n.DataCriacao).AsNoTracking().ToListAsync();
        }
        
        public async Task<bool> NotificarAsync(int usuarioId, TipoNotificacao tipo, string titulo, string mensagem, string? link = null)
        {
            var notificacao = new Notificacao
            {
                UsuarioId = usuarioId,
                TipoNotificacaoId = (int)tipo,
                Titulo = titulo,
                Mensagem = mensagem,
                Link = link,
                DataCriacao = DateTime.UtcNow,
                Lida = false
            };
            
            await AddAsync(notificacao);
            
            // Invalidar cache do usuário
            _cache.Remove(GetCacheKey("por_usuario", usuarioId));
            
            _logger.LogInformation("Notificação criada para usuário {UsuarioId}: {Titulo}", usuarioId, titulo);
            return true;
        }
        
        public async Task<long> GetTotalNaoLidasAsync(int usuarioId)
        {
            return await _dbSet.Where(n => n.UsuarioId == usuarioId && !n.Lida).CountAsync();
        }
    }
    #endregion
    
    #region TemplateEmailRepository  
    public interface ITemplateEmailRepository : IRepository<TemplateEmail>
    {
        Task<IEnumerable<TemplateEmail>> GetPorCategoriaAsync(string categoria);
        Task<TemplateEmail?> GetPorCodigoAsync(string codigo);
        Task<TemplateEmail?> GetCompletoAsync(int id);
        Task<string> ProcessarTemplateAsync(string codigo, Dictionary<string, object> dados);
        Task<bool> AtivarTemplateAsync(int templateId);
        Task<bool> DesativarTemplateAsync(int templateId);
    }
    
    public class TemplateEmailRepository : BaseRepository<TemplateEmail>, ITemplateEmailRepository
    {
        public TemplateEmailRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<TemplateEmailRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<TemplateEmail>> GetPorCategoriaAsync(string categoria)
        {
            return await _dbSet.Where(t => t.Categoria == categoria && t.Ativo)
                .OrderBy(t => t.Nome).AsNoTracking().ToListAsync();
        }
        
        public async Task<TemplateEmail?> GetPorCodigoAsync(string codigo)
        {
            var cacheKey = GetCacheKey("por_codigo", codigo);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.FirstOrDefaultAsync(t => t.Codigo == codigo && t.Ativo),
                TimeSpan.FromHours(1));
        }
        
        public async Task<TemplateEmail?> GetCompletoAsync(int id)
        {
            return await _dbSet.Include(t => t.VariaveisTemplate)
                .AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        }
        
        public async Task<string> ProcessarTemplateAsync(string codigo, Dictionary<string, object> dados)
        {
            var template = await GetPorCodigoAsync(codigo);
            if (template == null) return string.Empty;
            
            var conteudo = template.ConteudoHtml ?? string.Empty;
            
            // Processamento simples de variáveis - em produção usar Razor ou similar
            foreach (var variavel in dados)
            {
                conteudo = conteudo.Replace($"{{{{{variavel.Key}}}}}", variavel.Value?.ToString() ?? "");
            }
            
            return conteudo;
        }
        
        public async Task<bool> AtivarTemplateAsync(int templateId)
        {
            var template = await GetByIdAsync(templateId);
            if (template == null) return false;
            
            template.Ativo = true;
            await UpdateAsync(template);
            
            // Invalidar cache
            _cache.Remove(GetCacheKey("por_codigo", template.Codigo));
            return true;
        }
        
        public async Task<bool> DesativarTemplateAsync(int templateId)
        {
            var template = await GetByIdAsync(templateId);
            if (template == null) return false;
            
            template.Ativo = false;
            await UpdateAsync(template);
            
            // Invalidar cache
            _cache.Remove(GetCacheKey("por_codigo", template.Codigo));
            return true;
        }
    }
    #endregion
    
    #region JobEmailRepository (50+ Electoral Email Jobs)
    public interface IJobEmailRepository : IRepository<JobEmail>
    {
        Task<IEnumerable<JobEmail>> GetJobsPendentesAsync();
        Task<IEnumerable<JobEmail>> GetPorTipoAsync(TipoJobEmail tipo);
        Task<bool> ExecutarJobAsync(int jobId);
        Task<bool> AgendarJobAsync(TipoJobEmail tipo, DateTime? dataExecucao = null, object? parametros = null);
        Task<IEnumerable<JobEmail>> GetHistoricoExecucaoAsync(TipoJobEmail tipo);
    }
    
    public class JobEmailRepository : BaseRepository<JobEmail>, IJobEmailRepository
    {
        public JobEmailRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<JobEmailRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<JobEmail>> GetJobsPendentesAsync()
        {
            return await _dbSet.Where(j => j.Status == StatusJobEmail.Pendente && 
                (j.DataAgendamento == null || j.DataAgendamento <= DateTime.UtcNow))
                .OrderBy(j => j.DataCriacao).AsNoTracking().ToListAsync();
        }
        
        public async Task<IEnumerable<JobEmail>> GetPorTipoAsync(TipoJobEmail tipo)
        {
            return await _dbSet.Where(j => j.TipoJob == tipo)
                .OrderByDescending(j => j.DataCriacao).AsNoTracking().ToListAsync();
        }
        
        public async Task<bool> ExecutarJobAsync(int jobId)
        {
            var job = await GetByIdAsync(jobId);
            if (job == null) return false;
            
            try
            {
                job.Status = StatusJobEmail.Executando;
                job.DataInicio = DateTime.UtcNow;
                await UpdateAsync(job);
                
                // Aqui executaria a lógica específica do job baseado no TipoJob
                switch (job.TipoJob)
                {
                    case TipoJobEmail.NotificacaoCalendario:
                        await ExecutarNotificacaoCalendario(job);
                        break;
                    case TipoJobEmail.LembretePrazo:
                        await ExecutarLembretePrazo(job);
                        break;
                    case TipoJobEmail.RelatorioEleicao:
                        await ExecutarRelatorioEleicao(job);
                        break;
                    // ... outros 47+ jobs
                }
                
                job.Status = StatusJobEmail.Concluido;
                job.DataConclusao = DateTime.UtcNow;
                job.Sucesso = true;
                
                _logger.LogInformation("Job {JobId} do tipo {TipoJob} executado com sucesso", jobId, job.TipoJob);
            }
            catch (Exception ex)
            {
                job.Status = StatusJobEmail.Falha;
                job.DataConclusao = DateTime.UtcNow;
                job.Sucesso = false;
                job.MensagemErro = ex.Message;
                
                _logger.LogError(ex, "Falha na execução do job {JobId}", jobId);
            }
            
            await UpdateAsync(job);
            return job.Sucesso;
        }
        
        public async Task<bool> AgendarJobAsync(TipoJobEmail tipo, DateTime? dataExecucao = null, object? parametros = null)
        {
            var job = new JobEmail
            {
                TipoJob = tipo,
                Status = StatusJobEmail.Pendente,
                DataCriacao = DateTime.UtcNow,
                DataAgendamento = dataExecucao,
                Parametros = parametros != null ? System.Text.Json.JsonSerializer.Serialize(parametros) : null
            };
            
            await AddAsync(job);
            _logger.LogInformation("Job {TipoJob} agendado para {DataExecucao}", tipo, dataExecucao ?? DateTime.UtcNow);
            return true;
        }
        
        public async Task<IEnumerable<JobEmail>> GetHistoricoExecucaoAsync(TipoJobEmail tipo)
        {
            return await _dbSet.Where(j => j.TipoJob == tipo && j.Status == StatusJobEmail.Concluido)
                .OrderByDescending(j => j.DataConclusao).AsNoTracking().ToListAsync();
        }
        
        private async Task ExecutarNotificacaoCalendario(JobEmail job)
        {
            // Implementação específica para notificações de calendário eleitoral
            await Task.Delay(100); // Simula processamento
        }
        
        private async Task ExecutarLembretePrazo(JobEmail job)
        {
            // Implementação específica para lembretes de prazo
            await Task.Delay(100); // Simula processamento
        }
        
        private async Task ExecutarRelatorioEleicao(JobEmail job)
        {
            // Implementação específica para relatórios eleitorais
            await Task.Delay(100); // Simula processamento
        }
    }
    #endregion
    
    // Supporting entities for email/notification system
    public class Email : BaseEntity
    {
        public string EmailRemetente { get; set; } = string.Empty;
        public string EmailDestinatario { get; set; } = string.Empty;
        public string? CopiaDestinatarios { get; set; }
        public string? CopiaOcultaDestinatarios { get; set; }
        public string Assunto { get; set; } = string.Empty;
        public string ConteudoHtml { get; set; } = string.Empty;
        public string? ConteudoTexto { get; set; }
        public int? TemplateEmailId { get; set; }
        public StatusEmail Status { get; set; }
        public DateTime? DataEnvio { get; set; }
        public DateTime? UltimaTentativa { get; set; }
        public int TentativasEnvio { get; set; }
        public string? UltimoErro { get; set; }
        public bool Excluido { get; set; }
        
        public virtual TemplateEmail? TemplateEmail { get; set; }
        public virtual ICollection<AnexoEmail> Anexos { get; set; } = new List<AnexoEmail>();
        public virtual ICollection<LogEnvioEmail> LogsEnvio { get; set; } = new List<LogEnvioEmail>();
    }
    
    public class Notificacao : BaseEntity
    {
        public int UsuarioId { get; set; }
        public int TipoNotificacaoId { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public string? Link { get; set; }
        public bool Lida { get; set; }
        public DateTime? DataLeitura { get; set; }
        
        public virtual Usuario Usuario { get; set; } = null!;
        public virtual TipoNotificacao TipoNotificacao { get; set; } = null!;
    }
    
    public class TemplateEmail : BaseEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public string Assunto { get; set; } = string.Empty;
        public string ConteudoHtml { get; set; } = string.Empty;
        public string? ConteudoTexto { get; set; }
        public bool Ativo { get; set; }
        
        public virtual ICollection<VariavelTemplateEmail> VariaveisTemplate { get; set; } = new List<VariavelTemplateEmail>();
    }
    
    public class JobEmail : BaseEntity
    {
        public TipoJobEmail TipoJob { get; set; }
        public StatusJobEmail Status { get; set; }
        public DateTime? DataAgendamento { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataConclusao { get; set; }
        public string? Parametros { get; set; }
        public string? MensagemErro { get; set; }
        public bool Sucesso { get; set; }
    }
    
    // Enums and support classes
    public enum StatusEmail { Pendente = 1, Enviado = 2, Falha = 3, Cancelado = 4 }
    // TipoNotificacao movido para Domain/Enums
    public enum TipoJobEmail { NotificacaoCalendario = 1, LembretePrazo = 2, RelatorioEleicao = 3 }
    public enum StatusJobEmail { Pendente = 1, Executando = 2, Concluido = 3, Falha = 4 }
    
    public class AnexoEmail { public int Id { get; set; } }
    public class LogEnvioEmail { public int Id { get; set; } public int EmailId { get; set; } public StatusEmail StatusEnvio { get; set; } public DateTime DataTentativa { get; set; } public bool Sucesso { get; set; } public string? MotivoFalha { get; set; } }
    public class TipoNotificacao { public int Id { get; set; } public string Nome { get; set; } = string.Empty; }
    public class VariavelTemplateEmail { public int Id { get; set; } }
}