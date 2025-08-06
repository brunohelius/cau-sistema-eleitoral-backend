using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    // BATCH 1: CORE JUDICIAL SYSTEM REPOSITORIES
    // Auto-generated with established patterns for rapid migration
    
    #region JulgamentoImpugnacaoRepository
    public interface IJulgamentoImpugnacaoRepository : IRepository<JulgamentoImpugnacao>
    {
        Task<IEnumerable<JulgamentoImpugnacao>> GetPorImpugnacaoAsync(int impugnacaoId);
        Task<JulgamentoImpugnacao?> GetJulgamentoPrimeiraInstanciaAsync(int impugnacaoId);
        Task<JulgamentoImpugnacao?> GetJulgamentoSegundaInstanciaAsync(int impugnacaoId);
        Task<JulgamentoImpugnacao?> GetCompletoAsync(int id);
        Task<bool> ImpugnacaoJaFoiJulgadaAsync(int impugnacaoId, TipoInstancia instancia);
        Task<ResultadoJulgamento> CalcularResultadoAsync(int julgamentoId);
        Task<IEnumerable<JulgamentoImpugnacao>> GetPendentesVotacaoAsync(int comissaoId);
        Task<bool> ValidarQuorumMinimoAsync(int julgamentoId);
    }
    
    public class JulgamentoImpugnacaoRepository : BaseRepository<JulgamentoImpugnacao>, IJulgamentoImpugnacaoRepository
    {
        public JulgamentoImpugnacaoRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<JulgamentoImpugnacaoRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<JulgamentoImpugnacao>> GetPorImpugnacaoAsync(int impugnacaoId)
        {
            var cacheKey = GetCacheKey("por_impugnacao", impugnacaoId);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Include(j => j.PedidoImpugnacao).Include(j => j.ComissaoEleitoral)
                    .Where(j => j.PedidoImpugnacaoId == impugnacaoId).AsNoTracking().ToListAsync(),
                TimeSpan.FromMinutes(15));
        }
        
        public async Task<JulgamentoImpugnacao?> GetJulgamentoPrimeiraInstanciaAsync(int impugnacaoId)
        {
            return await _dbSet.Include(j => j.VotacoesJulgamento)
                .FirstOrDefaultAsync(j => j.PedidoImpugnacaoId == impugnacaoId && j.Instancia == TipoInstancia.Primeira);
        }
        
        public async Task<JulgamentoImpugnacao?> GetJulgamentoSegundaInstanciaAsync(int impugnacaoId)
        {
            return await _dbSet.Include(j => j.VotacoesJulgamento)
                .FirstOrDefaultAsync(j => j.PedidoImpugnacaoId == impugnacaoId && j.Instancia == TipoInstancia.Segunda);
        }
        
        public async Task<JulgamentoImpugnacao?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Include(j => j.PedidoImpugnacao).Include(j => j.ComissaoEleitoral)
                    .Include(j => j.VotacoesJulgamento).ThenInclude(v => v.MembroComissao).ThenInclude(m => m.Profissional)
                    .AsNoTracking().FirstOrDefaultAsync(j => j.Id == id),
                TimeSpan.FromMinutes(10));
        }
        
        public async Task<bool> ImpugnacaoJaFoiJulgadaAsync(int impugnacaoId, TipoInstancia instancia)
        {
            return await _dbSet.AnyAsync(j => j.PedidoImpugnacaoId == impugnacaoId && j.Instancia == instancia && j.Status == StatusJulgamento.Julgado);
        }
        
        public async Task<ResultadoJulgamento> CalcularResultadoAsync(int julgamentoId)
        {
            var votacoes = await _context.VotacoesJulgamento.Where(v => v.JulgamentoImpugnacaoId == julgamentoId).ToListAsync();
            var totalVotos = votacoes.Count;
            if (totalVotos == 0) return new ResultadoJulgamento();
            
            var votosProcedente = votacoes.Count(v => v.Voto == TipoVoto.Procedente);
            var votosImprocedente = votacoes.Count(v => v.Voto == TipoVoto.Improcedente);
            
            return new ResultadoJulgamento
            {
                TotalVotos = totalVotos,
                VotosProcedente = votosProcedente,
                VotosImprocedente = votosImprocedente,
                ResultadoFinal = votosProcedente > votosImprocedente ? ResultadoFinal.Procedente : 
                                votosImprocedente > votosProcedente ? ResultadoFinal.Improcedente : ResultadoFinal.Empate
            };
        }
        
        public async Task<IEnumerable<JulgamentoImpugnacao>> GetPendentesVotacaoAsync(int comissaoId)
        {
            return await _dbSet.Include(j => j.PedidoImpugnacao)
                .Where(j => j.ComissaoEleitoralId == comissaoId && j.Status == StatusJulgamento.EmVotacao)
                .AsNoTracking().ToListAsync();
        }
        
        public async Task<bool> ValidarQuorumMinimoAsync(int julgamentoId)
        {
            var julgamento = await _dbSet.Include(j => j.ComissaoEleitoral).ThenInclude(c => c.Membros)
                .Include(j => j.VotacoesJulgamento).FirstOrDefaultAsync(j => j.Id == julgamentoId);
            if (julgamento == null) return false;
            
            var totalMembros = julgamento.ComissaoEleitoral.Membros.Count(m => m.Ativo);
            var totalVotos = julgamento.VotacoesJulgamento.Count();
            return totalVotos >= (int)Math.Ceiling(totalMembros * 0.6);
        }
    }
    #endregion
    
    #region RecursoDenunciaRepository
    public interface IRecursoDenunciaRepository : IRepository<RecursoDenuncia>
    {
        Task<IEnumerable<RecursoDenuncia>> GetPorDenunciaAsync(int denunciaId);
        Task<RecursoDenuncia?> GetCompletoAsync(int id);
        Task<IEnumerable<RecursoDenuncia>> GetPorRequerente(int requerenteId);
        Task<bool> PodeInterporRecursoAsync(int denunciaId);
        Task<string> GerarNumeroRecursoAsync(int ano);
        Task<IEnumerable<RecursoDenuncia>> GetRecursosPendentesAsync(int comissaoId);
        Task<bool> RecursoEstaVencidoAsync(int recursoId);
    }
    
    public class RecursoDenunciaRepository : BaseRepository<RecursoDenuncia>, IRecursoDenunciaRepository
    {
        public RecursoDenunciaRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<RecursoDenunciaRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<RecursoDenuncia>> GetPorDenunciaAsync(int denunciaId)
        {
            return await _dbSet.Include(r => r.Denuncia).Include(r => r.Requerente)
                .Where(r => r.DenunciaId == denunciaId && !r.Excluido)
                .OrderBy(r => r.DataInterposicao).AsNoTracking().ToListAsync();
        }
        
        public async Task<RecursoDenuncia?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Include(r => r.Denuncia).Include(r => r.Requerente)
                    .Include(r => r.ArquivosRecurso).Include(r => r.JulgamentoRecurso)
                    .AsNoTracking().FirstOrDefaultAsync(r => r.Id == id),
                TimeSpan.FromMinutes(10));
        }
        
        public async Task<IEnumerable<RecursoDenuncia>> GetPorRequerente(int requerenteId)
        {
            return await _dbSet.Include(r => r.Denuncia)
                .Where(r => r.RequerenteId == requerenteId && !r.Excluido)
                .OrderByDescending(r => r.DataInterposicao).AsNoTracking().ToListAsync();
        }
        
        public async Task<bool> PodeInterporRecursoAsync(int denunciaId)
        {
            var denuncia = await _context.Denuncias.Include(d => d.JulgamentosDenuncia)
                .FirstOrDefaultAsync(d => d.Id == denunciaId);
            if (denuncia == null) return false;
            
            var julgamentoPrimeiraInstancia = denuncia.JulgamentosDenuncia
                .FirstOrDefault(j => j.Instancia == TipoInstancia.Primeira && j.Status == StatusJulgamento.Julgado);
            if (julgamentoPrimeiraInstancia == null) return false;
            
            var prazoRecurso = julgamentoPrimeiraInstancia.DataJulgamento.AddDays(15); // 15 dias para recurso
            return DateTime.Now <= prazoRecurso;
        }
        
        public async Task<string> GerarNumeroRecursoAsync(int ano)
        {
            var ultimoNumero = await _dbSet.Where(r => r.DataInterposicao.Year == ano)
                .OrderByDescending(r => r.NumeroRecurso).Select(r => r.NumeroRecurso)
                .FirstOrDefaultAsync();
            
            if (string.IsNullOrEmpty(ultimoNumero)) return $"REC-DEN-{ano:0000}-0001";
            
            var partes = ultimoNumero.Split('-');
            if (partes.Length == 4 && int.TryParse(partes[3], out int sequencia))
                return $"REC-DEN-{ano:0000}-{(sequencia + 1):0000}";
            
            return $"REC-DEN-{ano:0000}-0001";
        }
        
        public async Task<IEnumerable<RecursoDenuncia>> GetRecursosPendentesAsync(int comissaoId)
        {
            return await _dbSet.Include(r => r.Denuncia).Include(r => r.Requerente)
                .Where(r => r.ComissaoEleitoralId == comissaoId && r.Status == StatusRecurso.EmAnalise)
                .OrderBy(r => r.DataInterposicao).AsNoTracking().ToListAsync();
        }
        
        public async Task<bool> RecursoEstaVencidoAsync(int recursoId)
        {
            var recurso = await _dbSet.FirstOrDefaultAsync(r => r.Id == recursoId);
            return recurso?.PrazoJulgamento < DateTime.Now;
        }
    }
    #endregion
    
    #region DefesaDenunciaRepository  
    public interface IDefesaDenunciaRepository : IRepository<DefesaDenuncia>
    {
        Task<IEnumerable<DefesaDenuncia>> GetPorDenunciaAsync(int denunciaId);
        Task<DefesaDenuncia?> GetCompletoAsync(int id);
        Task<bool> PodeApresentarDefesaAsync(int denunciaId);
        Task<bool> DefesaJaApresentadaAsync(int denunciaId);
        Task<IEnumerable<DefesaDenuncia>> GetDefesasVencidasAsync();
    }
    
    public class DefesaDenunciaRepository : BaseRepository<DefesaDenuncia>, IDefesaDenunciaRepository
    {
        public DefesaDenunciaRepository(ApplicationDbContext context, IMemoryCache cache, ILogger<DefesaDenunciaRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<DefesaDenuncia>> GetPorDenunciaAsync(int denunciaId)
        {
            return await _dbSet.Include(d => d.Denuncia).Include(d => d.Defensor)
                .Include(d => d.Arquivos).Where(d => d.DenunciaId == denunciaId)
                .OrderBy(d => d.DataApresentacao).AsNoTracking().ToListAsync();
        }
        
        public async Task<DefesaDenuncia?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            return await GetFromCacheOrExecuteAsync(cacheKey,
                async () => await _dbSet.Include(d => d.Denuncia).Include(d => d.Defensor)
                    .Include(d => d.Arquivos).AsNoTracking().FirstOrDefaultAsync(d => d.Id == id),
                TimeSpan.FromMinutes(10));
        }
        
        public async Task<bool> PodeApresentarDefesaAsync(int denunciaId)
        {
            var denuncia = await _context.Denuncias.FirstOrDefaultAsync(d => d.Id == denunciaId);
            return denuncia?.Status == StatusDenuncia.EmDefesa && denuncia.PrazoDefesa > DateTime.Now;
        }
        
        public async Task<bool> DefesaJaApresentadaAsync(int denunciaId)
        {
            return await _dbSet.AnyAsync(d => d.DenunciaId == denunciaId);
        }
        
        public async Task<IEnumerable<DefesaDenuncia>> GetDefesasVencidasAsync()
        {
            var agora = DateTime.Now;
            return await _dbSet.Include(d => d.Denuncia).Include(d => d.Defensor)
                .Where(d => d.Denuncia.PrazoDefesa < agora && d.Denuncia.Status == StatusDenuncia.EmDefesa)
                .AsNoTracking().ToListAsync();
        }
    }
    #endregion
    
    // Supporting entities for batch repositories
    public class JulgamentoImpugnacao : BaseEntity
    {
        public int PedidoImpugnacaoId { get; set; }
        public int ComissaoEleitoralId { get; set; }
        public int RelatorId { get; set; }
        public DateTime DataJulgamento { get; set; }
        public TipoInstancia Instancia { get; set; }
        public StatusJulgamento Status { get; set; }
        
        public virtual PedidoImpugnacao PedidoImpugnacao { get; set; } = null!;
        public virtual ComissaoEleitoral ComissaoEleitoral { get; set; } = null!;
        public virtual Profissional Relator { get; set; } = null!;
        public virtual ICollection<VotacaoJulgamento> VotacoesJulgamento { get; set; } = new List<VotacaoJulgamento>();
    }
    
    public class RecursoDenuncia : BaseEntity
    {
        public int DenunciaId { get; set; }
        public int RequerenteId { get; set; }
        public int ComissaoEleitoralId { get; set; }
        public string NumeroRecurso { get; set; } = string.Empty;
        public DateTime DataInterposicao { get; set; }
        public DateTime? PrazoJulgamento { get; set; }
        public StatusRecurso Status { get; set; }
        public string Fundamentacao { get; set; } = string.Empty;
        public bool Excluido { get; set; }
        
        public virtual Denuncia Denuncia { get; set; } = null!;
        public virtual Profissional Requerente { get; set; } = null!;
        public virtual ComissaoEleitoral ComissaoEleitoral { get; set; } = null!;
        public virtual ICollection<ArquivoRecurso> ArquivosRecurso { get; set; } = new List<ArquivoRecurso>();
        public virtual JulgamentoRecurso? JulgamentoRecurso { get; set; }
    }
    
    public class DefesaDenuncia : BaseEntity
    {
        public int DenunciaId { get; set; }
        public int DefensorId { get; set; }
        public DateTime DataApresentacao { get; set; }
        public string Conteudo { get; set; } = string.Empty;
        
        public virtual Denuncia Denuncia { get; set; } = null!;
        public virtual Profissional Defensor { get; set; } = null!;
        public virtual ICollection<ArquivoDefesa> Arquivos { get; set; } = new List<ArquivoDefesa>();
    }
    
    public enum StatusRecurso { EmAnalise = 1, Julgado = 2, Arquivado = 3 }
    public class ArquivoRecurso { public int Id { get; set; } }
    public class JulgamentoRecurso { public int Id { get; set; } }
    public class ArquivoDefesa { public int Id { get; set; } }
}