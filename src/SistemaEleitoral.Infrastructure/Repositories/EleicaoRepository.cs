using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    public interface IEleicaoRepository : IRepository<Eleicao>
    {
        Task<IEnumerable<Eleicao>> GetEleicoesPorAnoAsync(int ano);
        Task<Eleicao?> GetEleicaoAtivaAsync();
        Task<IEnumerable<Eleicao>> GetEleicoesPorTipoAsync(TipoEleicao tipo);
        Task<Eleicao?> GetCompletoAsync(int id);
        Task<bool> ExisteEleicaoNoAnoAsync(int ano, TipoEleicao tipo, int? excludeId = null);
        Task<IEnumerable<Eleicao>> GetEleicoesPorPeriodoAsync(DateTime dataInicio, DateTime dataFim);
        Task<Eleicao?> GetEleicaoPorCalendarioAsync(int calendarioId);
        Task<IEnumerable<Eleicao>> GetEleicoesComChapasAsync();
        Task<IEnumerable<Eleicao>> GetEleicoesFinalizadasAsync();
        Task<IEnumerable<Eleicao>> GetEleicoesEmAndamentoAsync();
        Task<ResultadoEleicao?> GetResultadoEleicaoAsync(int eleicaoId);
        Task<int> CountChapasHabilitadasAsync(int eleicaoId);
        Task<int> CountVotosApuradosAsync(int eleicaoId);
        Task<bool> PodeIniciarVotacaoAsync(int eleicaoId);
        Task<bool> PodeFinalizarEleicaoAsync(int eleicaoId);
        Task<IEnumerable<EstatisticaEleicao>> GetEstatisticasEleicaoAsync(int eleicaoId);
    }

    public class EleicaoRepository : BaseRepository<Eleicao>, IEleicaoRepository
    {
        public EleicaoRepository(
            ApplicationDbContext context, 
            IMemoryCache cache, 
            ILogger<EleicaoRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<Eleicao>> GetEleicoesPorAnoAsync(int ano)
        {
            var cacheKey = GetCacheKey("por_ano", ano);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(e => e.Calendario)
                    .Include(e => e.TipoEleicao)
                    .Include(e => e.SituacaoEleicao)
                    .Where(e => e.Ano == ano && !e.Excluida)
                    .OrderBy(e => e.DataInicio)
                    .AsNoTracking()
                    .ToListAsync(),
                TimeSpan.FromMinutes(30)
            );
        }
        
        public async Task<Eleicao?> GetEleicaoAtivaAsync()
        {
            var cacheKey = GetCacheKey("ativa");
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(e => e.Calendario)
                    .ThenInclude(c => c.PrazosCalendario)
                    .Include(e => e.ChapasEleicao.Where(c => !c.Excluida))
                    .Include(e => e.ComissaoEleitoral)
                    .FirstOrDefaultAsync(e => e.Status == StatusEleicao.Ativa && !e.Excluida),
                TimeSpan.FromMinutes(10)
            );
        }
        
        public async Task<IEnumerable<Eleicao>> GetEleicoesPorTipoAsync(TipoEleicao tipo)
        {
            var cacheKey = GetCacheKey("por_tipo", tipo.ToString());
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(e => e.Calendario)
                    .Include(e => e.SituacaoEleicao)
                    .Where(e => e.TipoEleicao == tipo && !e.Excluida)
                    .OrderByDescending(e => e.Ano)
                    .AsNoTracking()
                    .ToListAsync(),
                TimeSpan.FromMinutes(20)
            );
        }
        
        public async Task<Eleicao?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(e => e.Calendario)
                    .ThenInclude(c => c.PrazosCalendario)
                    .Include(e => e.ChapasEleicao.Where(c => !c.Excluida))
                    .ThenInclude(c => c.Membros.Where(m => !m.Excluido))
                    .ThenInclude(m => m.Profissional)
                    .Include(e => e.ComissaoEleitoral)
                    .ThenInclude(c => c.Membros)
                    .Include(e => e.ResultadosEleicao)
                    .Include(e => e.DocumentosEleicao)
                    .Include(e => e.TipoEleicao)
                    .Include(e => e.SituacaoEleicao)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id),
                TimeSpan.FromMinutes(15)
            );
        }
        
        public async Task<bool> ExisteEleicaoNoAnoAsync(int ano, TipoEleicao tipo, int? excludeId = null)
        {
            var query = _dbSet.Where(e => e.Ano == ano && 
                                         e.TipoEleicao == tipo && 
                                         !e.Excluida);
                                         
            if (excludeId.HasValue)
                query = query.Where(e => e.Id != excludeId);
                
            return await query.AnyAsync();
        }
        
        public async Task<IEnumerable<Eleicao>> GetEleicoesPorPeriodoAsync(DateTime dataInicio, DateTime dataFim)
        {
            return await _dbSet
                .Include(e => e.Calendario)
                .Include(e => e.TipoEleicao)
                .Where(e => e.DataInicio <= dataFim && 
                           e.DataFim >= dataInicio && 
                           !e.Excluida)
                .OrderBy(e => e.DataInicio)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<Eleicao?> GetEleicaoPorCalendarioAsync(int calendarioId)
        {
            return await _dbSet
                .Include(e => e.Calendario)
                .Include(e => e.ChapasEleicao)
                .FirstOrDefaultAsync(e => e.CalendarioId == calendarioId && !e.Excluida);
        }
        
        public async Task<IEnumerable<Eleicao>> GetEleicoesComChapasAsync()
        {
            return await _dbSet
                .Include(e => e.ChapasEleicao.Where(c => !c.Excluida))
                .Include(e => e.Calendario)
                .Where(e => e.ChapasEleicao.Any(c => !c.Excluida) && !e.Excluida)
                .OrderByDescending(e => e.DataInicio)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Eleicao>> GetEleicoesFinalizadasAsync()
        {
            return await _dbSet
                .Include(e => e.ResultadosEleicao)
                .Include(e => e.Calendario)
                .Where(e => e.Status == StatusEleicao.Finalizada && !e.Excluida)
                .OrderByDescending(e => e.DataFinalizacao)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Eleicao>> GetEleicoesEmAndamentoAsync()
        {
            return await _dbSet
                .Include(e => e.Calendario)
                .Include(e => e.ChapasEleicao)
                .Where(e => (e.Status == StatusEleicao.Ativa || 
                            e.Status == StatusEleicao.Votacao || 
                            e.Status == StatusEleicao.Apuracao) && 
                           !e.Excluida)
                .OrderBy(e => e.DataInicio)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<ResultadoEleicao?> GetResultadoEleicaoAsync(int eleicaoId)
        {
            return await _context.ResultadosEleicao
                .Include(r => r.ChapaVencedora)
                .Include(r => r.EstatisticasVotacao)
                .FirstOrDefaultAsync(r => r.EleicaoId == eleicaoId);
        }
        
        public async Task<int> CountChapasHabilitadasAsync(int eleicaoId)
        {
            return await _context.ChapasEleicao
                .CountAsync(c => c.EleicaoId == eleicaoId && 
                                c.Status == StatusChapa.Habilitada && 
                                !c.Excluida);
        }
        
        public async Task<int> CountVotosApuradosAsync(int eleicaoId)
        {
            return await _context.VotosEleicao
                .Where(v => v.EleicaoId == eleicaoId && v.Validado)
                .SumAsync(v => v.Quantidade);
        }
        
        public async Task<bool> PodeIniciarVotacaoAsync(int eleicaoId)
        {
            var eleicao = await _dbSet
                .Include(e => e.Calendario)
                .ThenInclude(c => c.PrazosCalendario)
                .Include(e => e.ChapasEleicao)
                .FirstOrDefaultAsync(e => e.Id == eleicaoId);
                
            if (eleicao == null) return false;
            
            var agora = DateTime.Now;
            var prazoVotacao = eleicao.Calendario?.PrazosCalendario?
                .FirstOrDefault(p => p.TipoAtividade == "VOTACAO");
                
            return prazoVotacao != null && 
                   agora >= prazoVotacao.DataInicio && 
                   agora <= prazoVotacao.DataFim &&
                   eleicao.ChapasEleicao.Count(c => c.Status == StatusChapa.Habilitada) >= 1;
        }
        
        public async Task<bool> PodeFinalizarEleicaoAsync(int eleicaoId)
        {
            var eleicao = await _dbSet
                .Include(e => e.Calendario)
                .ThenInclude(c => c.PrazosCalendario)
                .FirstOrDefaultAsync(e => e.Id == eleicaoId);
                
            if (eleicao == null) return false;
            
            var agora = DateTime.Now;
            var prazoVotacao = eleicao.Calendario?.PrazosCalendario?
                .FirstOrDefault(p => p.TipoAtividade == "VOTACAO");
                
            return prazoVotacao != null && agora > prazoVotacao.DataFim;
        }
        
        public async Task<IEnumerable<EstatisticaEleicao>> GetEstatisticasEleicaoAsync(int eleicaoId)
        {
            var stats = new List<EstatisticaEleicao>();
            
            var totalChapas = await CountAsync(e => e.Id == eleicaoId);
            var chapasHabilitadas = await CountChapasHabilitadasAsync(eleicaoId);
            var votosApurados = await CountVotosApuradosAsync(eleicaoId);
            
            var totalProfissionaisAptos = await _context.ProfissionaisEleicao
                .CountAsync(pe => pe.EleicaoId == eleicaoId && pe.AptoVotar);
                
            stats.Add(new EstatisticaEleicao 
            { 
                Tipo = "Total de Chapas", 
                Valor = totalChapas 
            });
            
            stats.Add(new EstatisticaEleicao 
            { 
                Tipo = "Chapas Habilitadas", 
                Valor = chapasHabilitadas 
            });
            
            stats.Add(new EstatisticaEleicao 
            { 
                Tipo = "Votos Apurados", 
                Valor = votosApurados 
            });
            
            stats.Add(new EstatisticaEleicao 
            { 
                Tipo = "Profissionais Aptos", 
                Valor = totalProfissionaisAptos 
            });
            
            if (totalProfissionaisAptos > 0)
            {
                var percentualParticipacao = (votosApurados * 100.0) / totalProfissionaisAptos;
                stats.Add(new EstatisticaEleicao 
                { 
                    Tipo = "Percentual de Participação", 
                    Valor = (int)Math.Round(percentualParticipacao) 
                });
            }
            
            return stats;
        }
        
        public override async Task<Eleicao> UpdateAsync(Eleicao entity)
        {
            // Invalidate related caches
            InvalidateCacheForEntity(entity.Id);
            _cache.Remove(GetCacheKey("ativa"));
            _cache.Remove(GetCacheKey("por_ano", entity.Ano));
            _cache.Remove(GetCacheKey("por_tipo", entity.TipoEleicao.ToString()));
            
            return await base.UpdateAsync(entity);
        }
    }

    public class EstatisticaEleicao
    {
        public string Tipo { get; set; } = string.Empty;
        public int Valor { get; set; }
    }

    public class ResultadoEleicao
    {
        public int Id { get; set; }
        public int EleicaoId { get; set; }
        public int? ChapaVencedoraId { get; set; }
        public DateTime DataApuracao { get; set; }
        public int TotalVotos { get; set; }
        public int VotosValidos { get; set; }
        public int VotosNulos { get; set; }
        public int VotosBrancos { get; set; }
        public decimal PercentualParticipacao { get; set; }
        
        public virtual Eleicao Eleicao { get; set; } = null!;
        public virtual ChapaEleicao? ChapaVencedora { get; set; }
        public virtual ICollection<EstatisticaVotacao> EstatisticasVotacao { get; set; } = new List<EstatisticaVotacao>();
    }

    public class EstatisticaVotacao
    {
        public int Id { get; set; }
        public int ResultadoEleicaoId { get; set; }
        public int ChapaId { get; set; }
        public int QuantidadeVotos { get; set; }
        public decimal PercentualVotos { get; set; }
        
        public virtual ResultadoEleicao ResultadoEleicao { get; set; } = null!;
        public virtual ChapaEleicao Chapa { get; set; } = null!;
    }
}