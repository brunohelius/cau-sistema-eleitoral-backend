using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    public interface IProfissionalRepository : IRepository<Profissional>
    {
        Task<Profissional?> GetPorNumeroCauAsync(string numeroCau);
        Task<Profissional?> GetPorCpfAsync(string cpf);
        Task<Profissional?> GetPorEmailAsync(string email);
        Task<Profissional?> GetCompletoAsync(int id);
        Task<IEnumerable<Profissional>> GetPorFiltroAsync(ProfissionalFiltroDto filtro);
        Task<bool> ValidarElegibilidadeAsync(int profissionalId, int eleicaoId);
        Task<IEnumerable<Profissional>> GetProfissionaisAptosVotacaoAsync(int eleicaoId);
        Task<bool> ProfissionalAptoVotarAsync(int profissionalId, int eleicaoId);
        Task<bool> ProfissionalAptoSerMembroAsync(int profissionalId, int eleicaoId);
        Task<IEnumerable<Profissional>> GetPorUfAsync(int ufId);
        Task<IEnumerable<Profissional>> GetComPendenciasEleitoraisAsync();
        Task<bool> TemPendenciaFinanceiraAsync(int profissionalId);
        Task<bool> TemPendenciaEticaAsync(int profissionalId);
        Task<bool> TemPendenciaDocumentalAsync(int profissionalId);
        Task<IEnumerable<Profissional>> GetMembrosComissaoAsync(int comissaoId);
        Task<IEnumerable<Profissional>> GetCandidatosComissaoAsync(int eleicaoId, int ufId);
        Task<StatusProfissional> GetStatusAtualAsync(int profissionalId);
        Task<IEnumerable<HistoricoProfissional>> GetHistoricoAsync(int profissionalId);
        Task<bool> PodeParticiparEleicaoAsync(int profissionalId, int eleicaoId);
    }

    public class ProfissionalRepository : BaseRepository<Profissional>, IProfissionalRepository
    {
        public ProfissionalRepository(
            ApplicationDbContext context, 
            IMemoryCache cache, 
            ILogger<ProfissionalRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<Profissional?> GetPorNumeroCauAsync(string numeroCau)
        {
            var cacheKey = GetCacheKey("por_numero_cau", numeroCau);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(p => p.Uf)
                    .Include(p => p.StatusProfissional)
                    .Include(p => p.TipoInscricao)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.NumeroCau == numeroCau),
                TimeSpan.FromMinutes(30) // Cache longer for stable data
            );
        }
        
        public async Task<Profissional?> GetPorCpfAsync(string cpf)
        {
            var cacheKey = GetCacheKey("por_cpf", cpf);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(p => p.Uf)
                    .Include(p => p.StatusProfissional)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Cpf == cpf),
                TimeSpan.FromMinutes(20)
            );
        }
        
        public async Task<Profissional?> GetPorEmailAsync(string email)
        {
            return await _dbSet
                .Include(p => p.StatusProfissional)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Email.ToLower() == email.ToLower());
        }
        
        public async Task<Profissional?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(p => p.Uf)
                    .Include(p => p.StatusProfissional)
                    .Include(p => p.TipoInscricao)
                    .Include(p => p.AtividadesPrincipais)
                    .Include(p => p.AtividadesComplementares)
                    .Include(p => p.PendenciasFinanceiras)
                    .Include(p => p.PendenciasEticas)
                    .Include(p => p.HistoricoProfissional)
                    .Include(p => p.ChapasMembro)
                    .Include(p => p.ChapasResponsavel)
                    .Include(p => p.MembroComissoes)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id),
                TimeSpan.FromMinutes(15)
            );
        }
        
        public async Task<IEnumerable<Profissional>> GetPorFiltroAsync(ProfissionalFiltroDto filtro)
        {
            var query = _dbSet.AsNoTracking()
                .Include(p => p.Uf)
                .Include(p => p.StatusProfissional)
                .Include(p => p.TipoInscricao)
                .Where(p => p.Ativo);
                
            if (!string.IsNullOrEmpty(filtro.Nome))
                query = query.Where(p => p.Nome.Contains(filtro.Nome));
                
            if (!string.IsNullOrEmpty(filtro.NumeroCau))
                query = query.Where(p => p.NumeroCau.Contains(filtro.NumeroCau));
                
            if (!string.IsNullOrEmpty(filtro.Cpf))
                query = query.Where(p => p.Cpf == filtro.Cpf);
                
            if (filtro.UfId.HasValue)
                query = query.Where(p => p.UfId == filtro.UfId);
                
            if (filtro.StatusId.HasValue)
                query = query.Where(p => p.StatusProfissionalId == filtro.StatusId);
                
            if (filtro.TipoInscricaoId.HasValue)
                query = query.Where(p => p.TipoInscricaoId == filtro.TipoInscricaoId);
                
            if (filtro.ApenasAtivos.HasValue && filtro.ApenasAtivos.Value)
                query = query.Where(p => p.StatusProfissional.PermiteParticiparEleicao);
                
            if (filtro.ApenasSemPendencias.HasValue && filtro.ApenasSemPendencias.Value)
                query = query.Where(p => !p.PendenciasFinanceiras.Any(pf => pf.Ativa) &&
                                        !p.PendenciasEticas.Any(pe => pe.Ativa));
                
            return await query
                .OrderBy(p => p.Nome)
                .Take(filtro.Limite ?? 100)
                .ToListAsync();
        }
        
        public async Task<bool> ValidarElegibilidadeAsync(int profissionalId, int eleicaoId)
        {
            var profissional = await _dbSet
                .Include(p => p.StatusProfissional)
                .Include(p => p.PendenciasFinanceiras)
                .Include(p => p.PendenciasEticas)
                .FirstOrDefaultAsync(p => p.Id == profissionalId);
                
            if (profissional == null) return false;
            
            // Deve estar ativo
            if (!profissional.Ativo) return false;
            
            // Status deve permitir participação
            if (!profissional.StatusProfissional.PermiteParticicarEleicao) return false;
            
            // Não pode ter pendências financeiras ativas
            if (profissional.PendenciasFinanceiras.Any(pf => pf.Ativa)) return false;
            
            // Não pode ter pendências éticas graves
            if (profissional.PendenciasEticas.Any(pe => pe.Ativa && pe.Gravidade == GravidadePendencia.Alta)) 
                return false;
            
            // Validações específicas por tipo de eleição
            var eleicao = await _context.Eleicoes
                .FirstOrDefaultAsync(e => e.Id == eleicaoId);
                
            if (eleicao?.TipoEleicao == TipoEleicao.Federal)
            {
                // Para eleição federal, deve ter no mínimo X anos de inscrição
                var anosInscricao = DateTime.Now.Year - profissional.DataInscricao.Year;
                return anosInscricao >= 3;
            }
            
            return true;
        }
        
        public async Task<IEnumerable<Profissional>> GetProfissionaisAptosVotacaoAsync(int eleicaoId)
        {
            var cacheKey = GetCacheKey("aptos_votacao", eleicaoId);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(p => p.StatusProfissional)
                    .Where(p => p.Ativo &&
                               p.StatusProfissional.PermiteVotar &&
                               !p.PendenciasFinanceiras.Any(pf => pf.Ativa && pf.BloqueiaVotacao))
                    .AsNoTracking()
                    .ToListAsync(),
                TimeSpan.FromHours(1)
            );
        }
        
        public async Task<bool> ProfissionalAptoVotarAsync(int profissionalId, int eleicaoId)
        {
            var profissional = await _dbSet
                .Include(p => p.StatusProfissional)
                .Include(p => p.PendenciasFinanceiras)
                .FirstOrDefaultAsync(p => p.Id == profissionalId);
                
            if (profissional == null) return false;
            
            return profissional.Ativo &&
                   profissional.StatusProfissional.PermiteVotar &&
                   !profissional.PendenciasFinanceiras.Any(pf => pf.Ativa && pf.BloqueiaVotacao);
        }
        
        public async Task<bool> ProfissionalAptoSerMembroAsync(int profissionalId, int eleicaoId)
        {
            return await ValidarElegibilidadeAsync(profissionalId, eleicaoId);
        }
        
        public async Task<IEnumerable<Profissional>> GetPorUfAsync(int ufId)
        {
            var cacheKey = GetCacheKey("por_uf", ufId);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(p => p.StatusProfissional)
                    .Where(p => p.UfId == ufId && p.Ativo)
                    .OrderBy(p => p.Nome)
                    .AsNoTracking()
                    .ToListAsync(),
                TimeSpan.FromMinutes(30)
            );
        }
        
        public async Task<IEnumerable<Profissional>> GetComPendenciasEleitoraisAsync()
        {
            return await _dbSet
                .Include(p => p.PendenciasFinanceiras)
                .Include(p => p.PendenciasEticas)
                .Include(p => p.Uf)
                .Where(p => p.PendenciasFinanceiras.Any(pf => pf.Ativa) ||
                           p.PendenciasEticas.Any(pe => pe.Ativa))
                .OrderBy(p => p.Nome)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> TemPendenciaFinanceiraAsync(int profissionalId)
        {
            return await _context.PendenciasFinanceiras
                .AnyAsync(pf => pf.ProfissionalId == profissionalId && pf.Ativa);
        }
        
        public async Task<bool> TemPendenciaEticaAsync(int profissionalId)
        {
            return await _context.PendenciasEticas
                .AnyAsync(pe => pe.ProfissionalId == profissionalId && pe.Ativa);
        }
        
        public async Task<bool> TemPendenciaDocumentalAsync(int profissionalId)
        {
            return await _context.PendenciasDocumentais
                .AnyAsync(pd => pd.ProfissionalId == profissionalId && pd.Ativa);
        }
        
        public async Task<IEnumerable<Profissional>> GetMembrosComissaoAsync(int comissaoId)
        {
            return await _dbSet
                .Include(p => p.MembroComissoes)
                .Where(p => p.MembroComissoes.Any(mc => mc.ComissaoEleitoralId == comissaoId && mc.Ativo))
                .OrderBy(p => p.Nome)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Profissional>> GetCandidatosComissaoAsync(int eleicaoId, int ufId)
        {
            var profissionaisElegiveis = await _dbSet
                .Include(p => p.StatusProfissional)
                .Include(p => p.PendenciasFinanceiras)
                .Include(p => p.PendenciasEticas)
                .Where(p => p.UfId == ufId &&
                           p.Ativo &&
                           p.StatusProfissional.PermiteParticiparEleicao &&
                           !p.PendenciasFinanceiras.Any(pf => pf.Ativa) &&
                           !p.PendenciasEticas.Any(pe => pe.Ativa))
                .AsNoTracking()
                .ToListAsync();
                
            // Filtrar apenas aqueles com experiência mínima (pode ser configurável)
            return profissionaisElegiveis
                .Where(p => DateTime.Now.Year - p.DataInscricao.Year >= 2)
                .OrderBy(p => p.Nome)
                .ToList();
        }
        
        public async Task<StatusProfissional> GetStatusAtualAsync(int profissionalId)
        {
            var profissional = await _dbSet
                .Include(p => p.StatusProfissional)
                .FirstOrDefaultAsync(p => p.Id == profissionalId);
                
            return profissional?.StatusProfissional ?? throw new InvalidOperationException("Profissional não encontrado");
        }
        
        public async Task<IEnumerable<HistoricoProfissional>> GetHistoricoAsync(int profissionalId)
        {
            return await _context.HistoricosProfissional
                .Include(h => h.StatusAnterior)
                .Include(h => h.StatusAtual)
                .Include(h => h.UsuarioAlteracao)
                .Where(h => h.ProfissionalId == profissionalId)
                .OrderByDescending(h => h.DataAlteracao)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> PodeParticiparEleicaoAsync(int profissionalId, int eleicaoId)
        {
            var profissional = await GetCompletoAsync(profissionalId);
            if (profissional == null) return false;
            
            // Já está em uma chapa da mesma eleição?
            var jaEstaEmChapa = profissional.ChapasMembro
                .Any(cm => cm.ChapaEleicao.EleicaoId == eleicaoId && !cm.Excluido) ||
                profissional.ChapasResponsavel
                .Any(cr => cr.EleicaoId == eleicaoId && !cr.Excluida);
                
            if (jaEstaEmChapa) return false;
            
            return await ValidarElegibilidadeAsync(profissionalId, eleicaoId);
        }
        
        public override async Task<Profissional> UpdateAsync(Profissional entity)
        {
            // Invalidate related caches
            InvalidateCacheForEntity(entity.Id);
            _cache.Remove(GetCacheKey("por_numero_cau", entity.NumeroCau));
            _cache.Remove(GetCacheKey("por_cpf", entity.Cpf));
            _cache.Remove(GetCacheKey("por_uf", entity.UfId));
            
            return await base.UpdateAsync(entity);
        }
    }

    public class ProfissionalFiltroDto
    {
        public string? Nome { get; set; }
        public string? NumeroCau { get; set; }
        public string? Cpf { get; set; }
        public int? UfId { get; set; }
        public int? StatusId { get; set; }
        public int? TipoInscricaoId { get; set; }
        public bool? ApenasAtivos { get; set; }
        public bool? ApenasSemPendencias { get; set; }
        public int? Limite { get; set; } = 100;
    }

    // Supporting entities that would be defined elsewhere
    public class StatusProfissional
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool PermiteParticiparEleicao { get; set; }
        public bool PermiteVotar { get; set; }
    }

    public class PendenciaFinanceira
    {
        public int Id { get; set; }
        public int ProfissionalId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public DateTime DataVencimento { get; set; }
        public bool Ativa { get; set; }
        public bool BloqueiaVotacao { get; set; }
    }

    public class PendenciaEtica
    {
        public int Id { get; set; }
        public int ProfissionalId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public GravidadePendencia Gravidade { get; set; }
        public bool Ativa { get; set; }
    }

    public class HistoricoProfissional
    {
        public int Id { get; set; }
        public int ProfissionalId { get; set; }
        public int StatusAnteriorId { get; set; }
        public int StatusAtualId { get; set; }
        public DateTime DataAlteracao { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public int UsuarioAlteracaoId { get; set; }
        
        public virtual StatusProfissional StatusAnterior { get; set; } = null!;
        public virtual StatusProfissional StatusAtual { get; set; } = null!;
        public virtual Usuario UsuarioAlteracao { get; set; } = null!;
    }

    public enum GravidadePendencia
    {
        Baixa = 1,
        Media = 2,
        Alta = 3
    }
}