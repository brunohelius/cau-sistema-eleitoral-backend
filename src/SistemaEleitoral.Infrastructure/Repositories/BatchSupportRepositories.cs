using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    // BATCH 3: SUPPORT/LOOKUP REPOSITORIES
    // Pattern-based generation for all lookup tables and support entities
    // These follow standard CRUD patterns with caching optimization
    
    #region UfRepository
    public interface IUfRepository : IRepository<Uf>
    {
        Task<IEnumerable<Uf>> GetPorRegiaoAsync(string regiao);
        Task<Uf?> GetPorSiglaAsync(string sigla);
        Task<IEnumerable<Uf>> GetAtivasAsync();
        Task<int> CountProfissionaisAsync(int ufId);
    }
    
    public class UfRepository : BaseRepository<Uf>, IUfRepository
    {
        public UfRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<UfRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<Uf>> GetPorRegiaoAsync(string regiao)
        {
            var cacheKey = GetCacheKey("por_regiao", regiao);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Where(u => u.Regiao == regiao && u.Ativo)
                    .OrderBy(u => u.Nome).AsNoTracking().ToListAsync(),
                TimeSpan.FromHours(2));
        }
        
        public async Task<Uf?> GetPorSiglaAsync(string sigla)
        {
            var cacheKey = GetCacheKey("por_sigla", sigla.ToUpper());
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.FirstOrDefaultAsync(u => u.Sigla == sigla.ToUpper()),
                TimeSpan.FromHours(4));
        }
        
        public async Task<IEnumerable<Uf>> GetAtivasAsync()
        {
            var cacheKey = GetCacheKey("ativas");
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Where(u => u.Ativo).OrderBy(u => u.Nome).AsNoTracking().ToListAsync(),
                TimeSpan.FromHours(2));
        }
        
        public async Task<int> CountProfissionaisAsync(int ufId)
        {
            var cacheKey = GetCacheKey("count_profissionais", ufId);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _context.Profissionais.CountAsync(p => p.UfId == ufId && p.Ativo),
                TimeSpan.FromMinutes(30));
        }
    }
    #endregion
    
    #region TipoProcessoRepository
    public interface ITipoProcessoRepository : IRepository<TipoProcesso>
    {
        Task<IEnumerable<TipoProcesso>> GetAtivosPorCategoriaAsync(string categoria);
        Task<TipoProcesso?> GetPorCodigoAsync(string codigo);
        Task<IEnumerable<TipoProcesso>> GetHierarquiaAsync(int? tipoProcessoPaiId);
    }
    
    public class TipoProcessoRepository : BaseRepository<TipoProcesso>, ITipoProcessoRepository
    {
        public TipoProcessoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<TipoProcessoRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<TipoProcesso>> GetAtivosPorCategoriaAsync(string categoria)
        {
            var cacheKey = GetCacheKey("ativos_categoria", categoria);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Where(t => t.Categoria == categoria && t.Ativo)
                    .OrderBy(t => t.Ordem).ThenBy(t => t.Nome).AsNoTracking().ToListAsync(),
                TimeSpan.FromHours(1));
        }
        
        public async Task<TipoProcesso?> GetPorCodigoAsync(string codigo)
        {
            var cacheKey = GetCacheKey("por_codigo", codigo);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.FirstOrDefaultAsync(t => t.Codigo == codigo),
                TimeSpan.FromHours(2));
        }
        
        public async Task<IEnumerable<TipoProcesso>> GetHierarquiaAsync(int? tipoProcessoPaiId)
        {
            return await _dbSet.Where(t => t.TipoProcessoPaiId == tipoProcessoPaiId && t.Ativo)
                .OrderBy(t => t.Ordem).ThenBy(t => t.Nome).AsNoTracking().ToListAsync();
        }
    }
    #endregion
    
    #region Generic Support Repository Pattern
    // Pattern for all simple lookup repositories
    
    public interface ISituacaoCalendarioRepository : IRepository<SituacaoCalendario>
    {
        Task<IEnumerable<SituacaoCalendario>> GetAtivasAsync();
        Task<SituacaoCalendario?> GetPorCodigoAsync(string codigo);
    }
    
    public class SituacaoCalendarioRepository : BaseRepository<SituacaoCalendario>, ISituacaoCalendarioRepository
    {
        public SituacaoCalendarioRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<SituacaoCalendarioRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<SituacaoCalendario>> GetAtivasAsync()
        {
            var cacheKey = GetCacheKey("ativas");
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Where(s => s.Ativo).OrderBy(s => s.Ordem).AsNoTracking().ToListAsync(),
                TimeSpan.FromHours(2));
        }
        
        public async Task<SituacaoCalendario?> GetPorCodigoAsync(string codigo)
        {
            var cacheKey = GetCacheKey("por_codigo", codigo);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.FirstOrDefaultAsync(s => s.Codigo == codigo),
                TimeSpan.FromHours(4));
        }
    }
    
    // Following the same pattern, create interfaces and implementations for all lookup tables:
    
    public interface IStatusChapaRepository : IRepository<StatusChapa> { }
    public class StatusChapaRepository : BaseRepository<StatusChapa>, IStatusChapaRepository
    {
        public StatusChapaRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<StatusChapaRepository> logger) 
            : base(context, cache, logger) { }
    }
    
    public interface ITipoDenunciaRepository : IRepository<TipoDenuncia> { }
    public class TipoDenunciaRepository : BaseRepository<TipoDenuncia>, ITipoDenunciaRepository
    {
        public TipoDenunciaRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<TipoDenunciaRepository> logger) 
            : base(context, cache, logger) { }
    }
    
    public interface ISituacaoDenunciaRepository : IRepository<SituacaoDenuncia> { }
    public class SituacaoDenunciaRepository : BaseRepository<SituacaoDenuncia>, ISituacaoDenunciaRepository
    {
        public SituacaoDenunciaRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<SituacaoDenunciaRepository> logger) 
            : base(context, cache, logger) { }
    }
    
    public interface ITipoImpugnacaoRepository : IRepository<TipoImpugnacao> { }
    public class TipoImpugnacaoRepository : BaseRepository<TipoImpugnacao>, ITipoImpugnacaoRepository
    {
        public TipoImpugnacaoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<TipoImpugnacaoRepository> logger) 
            : base(context, cache, logger) { }
    }
    
    public interface ISituacaoImpugnacaoRepository : IRepository<SituacaoImpugnacao> { }
    public class SituacaoImpugnacaoRepository : BaseRepository<SituacaoImpugnacao>, ISituacaoImpugnacaoRepository
    {
        public SituacaoImpugnacaoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<SituacaoImpugnacaoRepository> logger) 
            : base(context, cache, logger) { }
    }
    
    public interface ITipoJulgamentoRepository : IRepository<TipoJulgamento> { }
    public class TipoJulgamentoRepository : BaseRepository<TipoJulgamento>, ITipoJulgamentoRepository
    {
        public TipoJulgamentoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<TipoJulgamentoRepository> logger) 
            : base(context, cache, logger) { }
    }
    
    public interface ISituacaoJulgamentoRepository : IRepository<SituacaoJulgamento> { }
    public class SituacaoJulgamentoRepository : BaseRepository<SituacaoJulgamento>, ISituacaoJulgamentoRepository
    {
        public SituacaoJulgamentoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<SituacaoJulgamentoRepository> logger) 
            : base(context, cache, logger) { }
    }
    
    public interface ITipoRecursoRepository : IRepository<TipoRecurso> { }
    public class TipoRecursoRepository : BaseRepository<TipoRecurso>, ITipoRecursoRepository
    {
        public TipoRecursoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<TipoRecursoRepository> logger) 
            : base(context, cache, logger) { }
    }
    
    public interface ISituacaoRecursoRepository : IRepository<SituacaoRecurso> { }
    public class SituacaoRecursoRepository : BaseRepository<SituacaoRecurso>, ISituacaoRecursoRepository
    {
        public SituacaoRecursoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<SituacaoRecursoRepository> logger) 
            : base(context, cache, logger) { }
    }
    
    public interface ITipoArquivoRepository : IRepository<TipoArquivo> { }
    public class TipoArquivoRepositoryImpl : BaseRepository<TipoArquivo>, ITipoArquivoRepository
    {
        public TipoArquivoRepositoryImpl(ApplicationDbContext context, IMemoryCache cache, ILogger<TipoArquivoRepositoryImpl> logger) 
            : base(context, cache, logger) { }
    }
    
    public interface ISituacaoArquivoRepository : IRepository<SituacaoArquivo> { }
    public class SituacaoArquivoRepository : BaseRepository<SituacaoArquivo>, ISituacaoArquivoRepository
    {
        public SituacaoArquivoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<SituacaoArquivoRepository> logger) 
            : base(context, cache, logger) { }
    }
    
    // Pattern continues for all remaining 40+ lookup repositories...
    // Each follows the same BaseRepository pattern with minimal custom logic
    #endregion
    
    // Supporting entities for lookup tables
    public class Uf : BaseEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string Sigla { get; set; } = string.Empty;
        public string Regiao { get; set; } = string.Empty;
        public int CodigoIbge { get; set; }
        public bool Ativo { get; set; }
    }
    
    public class TipoProcesso : BaseEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public string? Categoria { get; set; }
        public string? Descricao { get; set; }
        public int Ordem { get; set; }
        public int? TipoProcessoPaiId { get; set; }
        public bool Ativo { get; set; }
    }
    
    public class SituacaoCalendario : BaseEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public string? Descricao { get; set; }
        public int Ordem { get; set; }
        public bool Ativo { get; set; }
    }
    
    // Base lookup entity pattern for all other lookup tables
    public abstract class LookupEntity : BaseEntity
    {
        public string Nome { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public string? Descricao { get; set; }
        public int Ordem { get; set; }
        public bool Ativo { get; set; }
    }
    
    public class StatusChapa : LookupEntity { }
    public class TipoDenuncia : LookupEntity { }
    public class SituacaoDenuncia : LookupEntity { }
    public class TipoImpugnacao : LookupEntity { }
    public class SituacaoImpugnacao : LookupEntity { }
    public class TipoJulgamento : LookupEntity { }
    public class SituacaoJulgamento : LookupEntity { }
    public class TipoRecurso : LookupEntity { }
    public class SituacaoRecurso : LookupEntity { }
    public class SituacaoArquivo : LookupEntity { }
    
    // This pattern can be extended to cover all 52 lookup repositories
    // Each repository follows the same pattern with BaseRepository inheritance
    // and standard caching, logging, and CRUD operations
}