using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    public interface IJulgamentoDenunciaRepository : IRepository<JulgamentoDenuncia>
    {
        Task<IEnumerable<JulgamentoDenuncia>> GetPorDenunciaAsync(int denunciaId);
        Task<JulgamentoDenuncia?> GetJulgamentoPrimeiraInstanciaAsync(int denunciaId);
        Task<JulgamentoDenuncia?> GetJulgamentoSegundaInstanciaAsync(int denunciaId);
        Task<JulgamentoDenuncia?> GetCompletoAsync(int id);
        Task<IEnumerable<JulgamentoDenuncia>> GetPorRelatorAsync(int relatorId);
        Task<IEnumerable<JulgamentoDenuncia>> GetPorComissaoAsync(int comissaoId);
        Task<IEnumerable<JulgamentoDenuncia>> GetAgendadosAsync(DateTime? data = null);
        Task<IEnumerable<JulgamentoDenuncia>> GetRealizadosAsync(DateTime dataInicio, DateTime dataFim);
        Task<bool> DenunciaJaFoiJulgadaAsync(int denunciaId, TipoInstancia instancia);
        Task<bool> PodeIniciarJulgamentoAsync(int julgamentoId);
        Task<bool> TodosVotaramAsync(int julgamentoId);
        Task<ResultadoJulgamento> CalcularResultadoAsync(int julgamentoId);
        Task<IEnumerable<VotacaoJulgamento>> GetVotacoesAsync(int julgamentoId);
        Task<bool> MembroJaVotouAsync(int julgamentoId, int membroId);
        Task<IEnumerable<JulgamentoDenuncia>> GetPendentesVotacaoAsync(int comissaoId);
        Task<bool> ValidarQuorumMinimoAsync(int julgamentoId);
        Task<decimal> CalcularPercentualVotosAsync(int julgamentoId, TipoVoto tipoVoto);
        Task<bool> JulgamentoEstaFinalizadoAsync(int julgamentoId);
        Task<DateTime?> GetPrazoJulgamentoAsync(int julgamentoId);
        Task<IEnumerable<JulgamentoDenuncia>> GetJulgamentosVencidosAsync();
    }

    public class JulgamentoDenunciaRepository : BaseRepository<JulgamentoDenuncia>, IJulgamentoDenunciaRepository
    {
        public JulgamentoDenunciaRepository(
            ApplicationDbContext context, 
            IMemoryCache cache, 
            ILogger<JulgamentoDenunciaRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<JulgamentoDenuncia>> GetPorDenunciaAsync(int denunciaId)
        {
            var cacheKey = GetCacheKey("por_denuncia", denunciaId);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(j => j.Denuncia)
                    .Include(j => j.ComissaoEleitoral)
                    .Include(j => j.Relator)
                    .Include(j => j.StatusJulgamento)
                    .Include(j => j.VotacoesJulgamento)
                    .ThenInclude(v => v.MembroComissao)
                    .ThenInclude(m => m.Profissional)
                    .Where(j => j.DenunciaId == denunciaId)
                    .OrderBy(j => j.Instancia)
                    .ThenBy(j => j.DataJulgamento)
                    .AsNoTracking()
                    .ToListAsync(),
                TimeSpan.FromMinutes(15)
            );
        }
        
        public async Task<JulgamentoDenuncia?> GetJulgamentoPrimeiraInstanciaAsync(int denunciaId)
        {
            return await _dbSet
                .Include(j => j.ComissaoEleitoral)
                .Include(j => j.Relator)
                .Include(j => j.VotacoesJulgamento)
                .ThenInclude(v => v.MembroComissao)
                .FirstOrDefaultAsync(j => j.DenunciaId == denunciaId && 
                                         j.Instancia == TipoInstancia.Primeira);
        }
        
        public async Task<JulgamentoDenuncia?> GetJulgamentoSegundaInstanciaAsync(int denunciaId)
        {
            return await _dbSet
                .Include(j => j.ComissaoEleitoral)
                .Include(j => j.Relator)
                .Include(j => j.VotacoesJulgamento)
                .ThenInclude(v => v.MembroComissao)
                .FirstOrDefaultAsync(j => j.DenunciaId == denunciaId && 
                                         j.Instancia == TipoInstancia.Segunda);
        }
        
        public async Task<JulgamentoDenuncia?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(j => j.Denuncia)
                    .ThenInclude(d => d.Denunciante)
                    .Include(j => j.Denuncia)
                    .ThenInclude(d => d.Denunciado)
                    .Include(j => j.ComissaoEleitoral)
                    .ThenInclude(c => c.Membros.Where(m => m.Ativo))
                    .ThenInclude(m => m.Profissional)
                    .Include(j => j.Relator)
                    .Include(j => j.StatusJulgamento)
                    .Include(j => j.VotacoesJulgamento)
                    .ThenInclude(v => v.MembroComissao)
                    .ThenInclude(m => m.Profissional)
                    .Include(j => j.ArquivosJulgamento)
                    .Include(j => j.HistoricoJulgamento)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(j => j.Id == id),
                TimeSpan.FromMinutes(10)
            );
        }
        
        public async Task<IEnumerable<JulgamentoDenuncia>> GetPorRelatorAsync(int relatorId)
        {
            return await _dbSet
                .Include(j => j.Denuncia)
                .ThenInclude(d => d.Denunciante)
                .Include(j => j.Denuncia)
                .ThenInclude(d => d.Denunciado)
                .Include(j => j.ComissaoEleitoral)
                .Include(j => j.StatusJulgamento)
                .Where(j => j.RelatorId == relatorId)
                .OrderByDescending(j => j.DataJulgamento)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<IEnumerable<JulgamentoDenuncia>> GetPorComissaoAsync(int comissaoId)
        {
            return await _dbSet
                .Include(j => j.Denuncia)
                .ThenInclude(d => d.Denunciante)
                .Include(j => j.Denuncia)
                .ThenInclude(d => d.Denunciado)
                .Include(j => j.Relator)
                .Include(j => j.StatusJulgamento)
                .Where(j => j.ComissaoEleitoralId == comissaoId)
                .OrderBy(j => j.DataJulgamento)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<IEnumerable<JulgamentoDenuncia>> GetAgendadosAsync(DateTime? data = null)
        {
            var dataConsulta = data ?? DateTime.Today;
            
            return await _dbSet
                .Include(j => j.Denuncia)
                .Include(j => j.ComissaoEleitoral)
                .Include(j => j.Relator)
                .Where(j => j.DataJulgamento.Date == dataConsulta.Date &&
                           j.Status == StatusJulgamento.Agendado)
                .OrderBy(j => j.DataJulgamento)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<IEnumerable<JulgamentoDenuncia>> GetRealizadosAsync(DateTime dataInicio, DateTime dataFim)
        {
            return await _dbSet
                .Include(j => j.Denuncia)
                .Include(j => j.ComissaoEleitoral)
                .Include(j => j.Relator)
                .Where(j => j.DataJulgamento >= dataInicio &&
                           j.DataJulgamento <= dataFim &&
                           j.Status == StatusJulgamento.Julgado)
                .OrderBy(j => j.DataJulgamento)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> DenunciaJaFoiJulgadaAsync(int denunciaId, TipoInstancia instancia)
        {
            return await _dbSet
                .AnyAsync(j => j.DenunciaId == denunciaId &&
                              j.Instancia == instancia &&
                              j.Status == StatusJulgamento.Julgado);
        }
        
        public async Task<bool> PodeIniciarJulgamentoAsync(int julgamentoId)
        {
            var julgamento = await _dbSet
                .Include(j => j.Denuncia)
                .Include(j => j.ComissaoEleitoral)
                .ThenInclude(c => c.Membros.Where(m => m.Ativo))
                .FirstOrDefaultAsync(j => j.Id == julgamentoId);
                
            if (julgamento == null) return false;
            
            // Verificar se está agendado
            if (julgamento.Status != StatusJulgamento.Agendado) return false;
            
            // Verificar se está no prazo
            if (julgamento.DataJulgamento > DateTime.Now) return false;
            
            // Verificar quórum mínimo
            var membrosAtivos = julgamento.ComissaoEleitoral.Membros.Count(m => m.Ativo);
            var quorumMinimo = (int)Math.Ceiling(membrosAtivos * 0.6); // 60% dos membros
            
            return membrosAtivos >= quorumMinimo;
        }
        
        public async Task<bool> TodosVotaramAsync(int julgamentoId)
        {
            var julgamento = await _dbSet
                .Include(j => j.ComissaoEleitoral)
                .ThenInclude(c => c.Membros.Where(m => m.Ativo))
                .Include(j => j.VotacoesJulgamento)
                .FirstOrDefaultAsync(j => j.Id == julgamentoId);
                
            if (julgamento == null) return false;
            
            var totalMembros = julgamento.ComissaoEleitoral.Membros.Count(m => m.Ativo);
            var totalVotos = julgamento.VotacoesJulgamento.Count();
            
            return totalVotos >= totalMembros;
        }
        
        public async Task<ResultadoJulgamento> CalcularResultadoAsync(int julgamentoId)
        {
            var votacoes = await _context.VotacoesJulgamento
                .Where(v => v.JulgamentoDenunciaId == julgamentoId)
                .ToListAsync();
                
            var totalVotos = votacoes.Count;
            if (totalVotos == 0) return new ResultadoJulgamento();
            
            var votosProcedente = votacoes.Count(v => v.Voto == TipoVoto.Procedente);
            var votosImprocedente = votacoes.Count(v => v.Voto == TipoVoto.Improcedente);
            var votosAbstencao = votacoes.Count(v => v.Voto == TipoVoto.Abstencao);
            
            var resultado = new ResultadoJulgamento
            {
                TotalVotos = totalVotos,
                VotosProcedente = votosProcedente,
                VotosImprocedente = votosImprocedente,
                VotosAbstencao = votosAbstencao,
                PercentualProcedente = (votosProcedente * 100.0m) / totalVotos,
                PercentualImprocedente = (votosImprocedente * 100.0m) / totalVotos
            };
            
            // Determinar resultado final (maioria simples)
            if (votosProcedente > votosImprocedente)
                resultado.ResultadoFinal = ResultadoFinal.Procedente;
            else if (votosImprocedente > votosProcedente)
                resultado.ResultadoFinal = ResultadoFinal.Improcedente;
            else
                resultado.ResultadoFinal = ResultadoFinal.Empate;
                
            return resultado;
        }
        
        public async Task<IEnumerable<VotacaoJulgamento>> GetVotacoesAsync(int julgamentoId)
        {
            return await _context.VotacoesJulgamento
                .Include(v => v.MembroComissao)
                .ThenInclude(m => m.Profissional)
                .Where(v => v.JulgamentoDenunciaId == julgamentoId)
                .OrderBy(v => v.DataVoto)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> MembroJaVotouAsync(int julgamentoId, int membroId)
        {
            return await _context.VotacoesJulgamento
                .AnyAsync(v => v.JulgamentoDenunciaId == julgamentoId &&
                              v.MembroComissaoId == membroId);
        }
        
        public async Task<IEnumerable<JulgamentoDenuncia>> GetPendentesVotacaoAsync(int comissaoId)
        {
            return await _dbSet
                .Include(j => j.Denuncia)
                .Include(j => j.Relator)
                .Where(j => j.ComissaoEleitoralId == comissaoId &&
                           j.Status == StatusJulgamento.EmVotacao)
                .OrderBy(j => j.DataJulgamento)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> ValidarQuorumMinimoAsync(int julgamentoId)
        {
            var julgamento = await _dbSet
                .Include(j => j.ComissaoEleitoral)
                .ThenInclude(c => c.Membros.Where(m => m.Ativo))
                .Include(j => j.VotacoesJulgamento)
                .FirstOrDefaultAsync(j => j.Id == julgamentoId);
                
            if (julgamento == null) return false;
            
            var totalMembros = julgamento.ComissaoEleitoral.Membros.Count(m => m.Ativo);
            var totalVotos = julgamento.VotacoesJulgamento.Count();
            var quorumMinimo = (int)Math.Ceiling(totalMembros * 0.6); // 60%
            
            return totalVotos >= quorumMinimo;
        }
        
        public async Task<decimal> CalcularPercentualVotosAsync(int julgamentoId, TipoVoto tipoVoto)
        {
            var votacoes = await _context.VotacoesJulgamento
                .Where(v => v.JulgamentoDenunciaId == julgamentoId)
                .ToListAsync();
                
            if (!votacoes.Any()) return 0;
            
            var votosDoTipo = votacoes.Count(v => v.Voto == tipoVoto);
            return (votosDoTipo * 100.0m) / votacoes.Count;
        }
        
        public async Task<bool> JulgamentoEstaFinalizadoAsync(int julgamentoId)
        {
            var julgamento = await _dbSet
                .FirstOrDefaultAsync(j => j.Id == julgamentoId);
                
            return julgamento?.Status == StatusJulgamento.Julgado;
        }
        
        public async Task<DateTime?> GetPrazoJulgamentoAsync(int julgamentoId)
        {
            var julgamento = await _dbSet
                .Include(j => j.Denuncia)
                .FirstOrDefaultAsync(j => j.Id == julgamentoId);
                
            return julgamento?.Denuncia?.PrazoJulgamento;
        }
        
        public async Task<IEnumerable<JulgamentoDenuncia>> GetJulgamentosVencidosAsync()
        {
            var agora = DateTime.Now;
            
            return await _dbSet
                .Include(j => j.Denuncia)
                .Include(j => j.ComissaoEleitoral)
                .Include(j => j.Relator)
                .Where(j => j.Status == StatusJulgamento.Agendado &&
                           j.DataJulgamento < agora)
                .OrderBy(j => j.DataJulgamento)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public override async Task<JulgamentoDenuncia> UpdateAsync(JulgamentoDenuncia entity)
        {
            // Invalidate related caches
            InvalidateCacheForEntity(entity.Id);
            _cache.Remove(GetCacheKey("por_denuncia", entity.DenunciaId));
            
            return await base.UpdateAsync(entity);
        }
    }

    public class ResultadoJulgamento
    {
        public int TotalVotos { get; set; }
        public int VotosProcedente { get; set; }
        public int VotosImprocedente { get; set; }
        public int VotosAbstencao { get; set; }
        public decimal PercentualProcedente { get; set; }
        public decimal PercentualImprocedente { get; set; }
        public ResultadoFinal ResultadoFinal { get; set; }
    }

    public enum ResultadoFinal
    {
        Procedente = 1,
        Improcedente = 2,
        Empate = 3
    }

    public enum TipoVoto
    {
        Procedente = 1,
        Improcedente = 2,
        Abstencao = 3
    }

    public enum StatusJulgamento
    {
        Agendado = 1,
        EmVotacao = 2,
        Julgado = 3,
        Adiado = 4,
        Cancelado = 5
    }

    public enum TipoInstancia
    {
        Primeira = 1,
        Segunda = 2
    }

    // Supporting entities that would be defined elsewhere
    public class JulgamentoDenuncia : BaseEntity
    {
        public int DenunciaId { get; set; }
        public int ComissaoEleitoralId { get; set; }
        public int RelatorId { get; set; }
        public DateTime DataJulgamento { get; set; }
        public TipoInstancia Instancia { get; set; }
        public StatusJulgamento Status { get; set; }
        public string? Parecer { get; set; }
        public string? Observacoes { get; set; }
        
        public virtual Denuncia Denuncia { get; set; } = null!;
        public virtual ComissaoEleitoral ComissaoEleitoral { get; set; } = null!;
        public virtual Profissional Relator { get; set; } = null!;
        public virtual StatusJulgamento StatusJulgamento { get; set; } = null!;
        public virtual ICollection<VotacaoJulgamento> VotacoesJulgamento { get; set; } = new List<VotacaoJulgamento>();
        public virtual ICollection<ArquivoJulgamento> ArquivosJulgamento { get; set; } = new List<ArquivoJulgamento>();
        public virtual ICollection<HistoricoJulgamento> HistoricoJulgamento { get; set; } = new List<HistoricoJulgamento>();
    }

    public class VotacaoJulgamento
    {
        public int Id { get; set; }
        public int JulgamentoDenunciaId { get; set; }
        public int MembroComissaoId { get; set; }
        public TipoVoto Voto { get; set; }
        public string? Justificativa { get; set; }
        public DateTime DataVoto { get; set; }
        
        public virtual JulgamentoDenuncia JulgamentoDenuncia { get; set; } = null!;
        public virtual MembroComissao MembroComissao { get; set; } = null!;
    }

    public class ArquivoJulgamento
    {
        public int Id { get; set; }
        public int JulgamentoDenunciaId { get; set; }
        public string NomeArquivo { get; set; } = string.Empty;
        public string CaminhoArquivo { get; set; } = string.Empty;
        public string TipoArquivo { get; set; } = string.Empty;
        public DateTime DataUpload { get; set; }
        
        public virtual JulgamentoDenuncia JulgamentoDenuncia { get; set; } = null!;
    }

    public class HistoricoJulgamento
    {
        public int Id { get; set; }
        public int JulgamentoDenunciaId { get; set; }
        public StatusJulgamento StatusAnterior { get; set; }
        public StatusJulgamento StatusAtual { get; set; }
        public DateTime DataAlteracao { get; set; }
        public string? Observacao { get; set; }
        public int UsuarioId { get; set; }
        
        public virtual JulgamentoDenuncia JulgamentoDenuncia { get; set; } = null!;
        public virtual Usuario Usuario { get; set; } = null!;
    }
}