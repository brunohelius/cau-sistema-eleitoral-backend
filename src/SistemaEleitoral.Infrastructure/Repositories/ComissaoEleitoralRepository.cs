using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    public interface IComissaoEleitoralRepository : IRepository<ComissaoEleitoral>
    {
        Task<IEnumerable<ComissaoEleitoral>> GetPorEleicaoAsync(int eleicaoId);
        Task<ComissaoEleitoral?> GetComissaoNacionalAsync(int eleicaoId);
        Task<IEnumerable<ComissaoEleitoral>> GetComissoesEstaduaisAsync(int eleicaoId);
        Task<ComissaoEleitoral?> GetPorEleicaoEUfAsync(int eleicaoId, int? ufId);
        Task<ComissaoEleitoral?> GetCompletoAsync(int id);
        Task<bool> ValidarComposicaoComissaoAsync(int comissaoId);
        Task<IEnumerable<ComissaoEleitoral>> GetComissoesAtivasAsync();
        Task<bool> ComissaoTemMembrosMinimosAsync(int comissaoId);
        Task<int> CalcularQuantidadeMembrosRequeridaAsync(TipoComissao tipo, int? populacaoProfissionais = null);
        Task<IEnumerable<MembroComissao>> GetMembrosComissaoAsync(int comissaoId);
        Task<bool> ProfissionalJaEMembroComissaoAsync(int profissionalId, int eleicaoId);
        Task<IEnumerable<ComissaoEleitoral>> GetComissoesParaConstituicaoAsync();
        Task<bool> PodeConstituirComissaoAsync(int comissaoId);
        Task<IEnumerable<Profissional>> GetCandidatosElegiveisAsync(int comissaoId);
        Task<bool> ValidarDiversidadeComissaoAsync(int comissaoId);
        Task<EstatisticaComissao> GetEstatisticasComissaoAsync(int comissaoId);
        Task<bool> ComissaoEstaCompletaAsync(int comissaoId);
        Task<DateTime?> GetPrazoConstituicaoAsync(int comissaoId);
        Task<IEnumerable<ComissaoEleitoral>> GetComissoesVencendoMandatoAsync(int diasAlerta = 30);
    }

    public class ComissaoEleitoralRepository : BaseRepository<ComissaoEleitoral>, IComissaoEleitoralRepository
    {
        public ComissaoEleitoralRepository(
            ApplicationDbContext context, 
            IMemoryCache cache, 
            ILogger<ComissaoEleitoralRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<ComissaoEleitoral>> GetPorEleicaoAsync(int eleicaoId)
        {
            var cacheKey = GetCacheKey("por_eleicao", eleicaoId);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(c => c.Eleicao)
                    .Include(c => c.Uf)
                    .Include(c => c.Membros.Where(m => m.Ativo))
                    .ThenInclude(m => m.Profissional)
                    .Include(c => c.StatusComissao)
                    .Where(c => c.EleicaoId == eleicaoId && !c.Excluida)
                    .OrderBy(c => c.TipoComissao)
                    .ThenBy(c => c.Uf.Nome)
                    .AsNoTracking()
                    .ToListAsync(),
                TimeSpan.FromMinutes(20)
            );
        }
        
        public async Task<ComissaoEleitoral?> GetComissaoNacionalAsync(int eleicaoId)
        {
            var cacheKey = GetCacheKey("nacional", eleicaoId);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(c => c.Membros.Where(m => m.Ativo))
                    .ThenInclude(m => m.Profissional)
                    .ThenInclude(p => p.Uf)
                    .Include(c => c.Eleicao)
                    .Include(c => c.StatusComissao)
                    .FirstOrDefaultAsync(c => c.EleicaoId == eleicaoId && 
                                             c.TipoComissao == TipoComissao.Nacional && 
                                             !c.Excluida),
                TimeSpan.FromMinutes(30)
            );
        }
        
        public async Task<IEnumerable<ComissaoEleitoral>> GetComissoesEstaduaisAsync(int eleicaoId)
        {
            return await _dbSet
                .Include(c => c.Uf)
                .Include(c => c.Membros.Where(m => m.Ativo))
                .ThenInclude(m => m.Profissional)
                .Include(c => c.StatusComissao)
                .Where(c => c.EleicaoId == eleicaoId && 
                           c.TipoComissao == TipoComissao.Estadual && 
                           !c.Excluida)
                .OrderBy(c => c.Uf.Nome)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<ComissaoEleitoral?> GetPorEleicaoEUfAsync(int eleicaoId, int? ufId)
        {
            if (ufId.HasValue)
            {
                return await _dbSet
                    .Include(c => c.Membros.Where(m => m.Ativo))
                    .ThenInclude(m => m.Profissional)
                    .Include(c => c.Uf)
                    .FirstOrDefaultAsync(c => c.EleicaoId == eleicaoId && 
                                             c.UfId == ufId && 
                                             !c.Excluida);
            }
            else
            {
                return await GetComissaoNacionalAsync(eleicaoId);
            }
        }
        
        public async Task<ComissaoEleitoral?> GetCompletoAsync(int id)
        {
            var cacheKey = GetCacheKey("completo", id);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(c => c.Eleicao)
                    .ThenInclude(e => e.Calendario)
                    .Include(c => c.Uf)
                    .Include(c => c.Membros.Where(m => m.Ativo))
                    .ThenInclude(m => m.Profissional)
                    .ThenInclude(p => p.StatusProfissional)
                    .Include(c => c.Membros)
                    .ThenInclude(m => m.CargoComissao)
                    .Include(c => c.StatusComissao)
                    .Include(c => c.ProcessosJulgamento)
                    .Include(c => c.Reunioes)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id),
                TimeSpan.FromMinutes(15)
            );
        }
        
        public async Task<bool> ValidarComposicaoComissaoAsync(int comissaoId)
        {
            var comissao = await GetCompletoAsync(comissaoId);
            if (comissao == null) return false;
            
            var requiredMembers = await CalcularQuantidadeMembrosRequeridaAsync(
                comissao.TipoComissao, 
                comissao.PopulacaoProfissionais);
                
            var membrosAtivos = comissao.Membros.Count(m => m.Ativo);
            
            // Verificar quantidade mínima
            if (membrosAtivos < requiredMembers) return false;
            
            // Verificar se tem coordenador
            if (!comissao.Membros.Any(m => m.CargoComissao.Nome == "Coordenador" && m.Ativo)) 
                return false;
            
            // Verificar diversidade geográfica (para comissão nacional)
            if (comissao.TipoComissao == TipoComissao.Nacional)
            {
                var regioes = comissao.Membros.Where(m => m.Ativo)
                    .Select(m => m.Profissional.Uf.Regiao)
                    .Distinct()
                    .Count();
                    
                if (regioes < 3) return false; // Deve ter pelo menos 3 regiões representadas
            }
            
            // Verificar se todos os membros estão elegíveis
            foreach (var membro in comissao.Membros.Where(m => m.Ativo))
            {
                if (!membro.Profissional.StatusProfissional.PermiteParticiparEleicao)
                    return false;
            }
            
            return true;
        }
        
        public async Task<IEnumerable<ComissaoEleitoral>> GetComissoesAtivasAsync()
        {
            return await _dbSet
                .Include(c => c.Eleicao)
                .Include(c => c.Uf)
                .Include(c => c.StatusComissao)
                .Where(c => c.StatusComissao.Nome == "Ativa" && !c.Excluida)
                .OrderBy(c => c.TipoComissao)
                .ThenBy(c => c.Uf.Nome)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> ComissaoTemMembrosMinimosAsync(int comissaoId)
        {
            var comissao = await _dbSet
                .Include(c => c.Membros)
                .FirstOrDefaultAsync(c => c.Id == comissaoId);
                
            if (comissao == null) return false;
            
            var requiredMembers = await CalcularQuantidadeMembrosRequeridaAsync(
                comissao.TipoComissao, 
                comissao.PopulacaoProfissionais);
                
            var membrosAtivos = comissao.Membros.Count(m => m.Ativo);
            
            return membrosAtivos >= requiredMembers;
        }
        
        public async Task<int> CalcularQuantidadeMembrosRequeridaAsync(TipoComissao tipo, int? populacaoProfissionais = null)
        {
            return tipo switch
            {
                TipoComissao.Nacional => 5, // Sempre 5 membros para comissão nacional
                TipoComissao.Estadual => CalcularMembrosComissaoEstadual(populacaoProfissionais ?? 0),
                TipoComissao.Regional => 3, // 3 membros para regionais
                _ => 3
            };
        }
        
        private static int CalcularMembrosComissaoEstadual(int populacaoProfissionais)
        {
            // Cálculo proporcional baseado na população de profissionais
            return populacaoProfissionais switch
            {
                <= 1000 => 3,
                <= 5000 => 5,
                <= 10000 => 7,
                _ => 9 // Máximo de 9 membros
            };
        }
        
        public async Task<IEnumerable<MembroComissao>> GetMembrosComissaoAsync(int comissaoId)
        {
            return await _context.MembrosComissao
                .Include(m => m.Profissional)
                .ThenInclude(p => p.Uf)
                .Include(m => m.CargoComissao)
                .Include(m => m.StatusMembro)
                .Where(m => m.ComissaoEleitoralId == comissaoId && m.Ativo)
                .OrderBy(m => m.CargoComissao.Ordem)
                .ThenBy(m => m.Profissional.Nome)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> ProfissionalJaEMembroComissaoAsync(int profissionalId, int eleicaoId)
        {
            return await _dbSet
                .Include(c => c.Membros)
                .AnyAsync(c => c.EleicaoId == eleicaoId &&
                              c.Membros.Any(m => m.ProfissionalId == profissionalId && m.Ativo) &&
                              !c.Excluida);
        }
        
        public async Task<IEnumerable<ComissaoEleitoral>> GetComissoesParaConstituicaoAsync()
        {
            return await _dbSet
                .Include(c => c.Eleicao)
                .Include(c => c.Uf)
                .Include(c => c.StatusComissao)
                .Where(c => c.StatusComissao.Nome == "Pendente Constituicao" && 
                           !c.Excluida)
                .OrderBy(c => c.DataLimiteConstituicao)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> PodeConstituirComissaoAsync(int comissaoId)
        {
            var comissao = await _dbSet
                .Include(c => c.Eleicao)
                .ThenInclude(e => e.Calendario)
                .Include(c => c.Membros)
                .FirstOrDefaultAsync(c => c.Id == comissaoId);
                
            if (comissao == null) return false;
            
            // Verificar se está no prazo
            if (comissao.DataLimiteConstituicao.HasValue && 
                DateTime.Now > comissao.DataLimiteConstituicao.Value)
                return false;
                
            // Verificar se tem membros suficientes
            if (!await ComissaoTemMembrosMinimosAsync(comissaoId))
                return false;
                
            return true;
        }
        
        public async Task<IEnumerable<Profissional>> GetCandidatosElegiveisAsync(int comissaoId)
        {
            var comissao = await _dbSet
                .Include(c => c.Eleicao)
                .Include(c => c.Uf)
                .FirstOrDefaultAsync(c => c.Id == comissaoId);
                
            if (comissao == null) return new List<Profissional>();
            
            var query = _context.Profissionais
                .Include(p => p.StatusProfissional)
                .Include(p => p.PendenciasFinanceiras)
                .Include(p => p.PendenciasEticas)
                .Where(p => p.Ativo &&
                           p.StatusProfissional.PermiteParticiparEleicao &&
                           !p.PendenciasFinanceiras.Any(pf => pf.Ativa) &&
                           !p.PendenciasEticas.Any(pe => pe.Ativa));
            
            // Filtrar por UF se for comissão estadual
            if (comissao.TipoComissao == TipoComissao.Estadual && comissao.UfId.HasValue)
            {
                query = query.Where(p => p.UfId == comissao.UfId);
            }
            
            // Excluir quem já é membro de outra comissão na mesma eleição
            var profissionaisJaMembros = await _context.MembrosComissao
                .Include(m => m.ComissaoEleitoral)
                .Where(m => m.ComissaoEleitoral.EleicaoId == comissao.EleicaoId && m.Ativo)
                .Select(m => m.ProfissionalId)
                .ToListAsync();
                
            query = query.Where(p => !profissionaisJaMembros.Contains(p.Id));
            
            return await query
                .OrderBy(p => p.Nome)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> ValidarDiversidadeComissaoAsync(int comissaoId)
        {
            var estatisticas = await GetEstatisticasComissaoAsync(comissaoId);
            
            // Pelo menos 30% de representação feminina
            if (estatisticas.PercentualFeminino < 30) return false;
            
            // Para comissão nacional, deve ter diversidade regional
            var comissao = await _dbSet.FirstOrDefaultAsync(c => c.Id == comissaoId);
            if (comissao?.TipoComissao == TipoComissao.Nacional)
            {
                if (estatisticas.DiversidadeRegional < 3) return false;
            }
            
            return true;
        }
        
        public async Task<EstatisticaComissao> GetEstatisticasComissaoAsync(int comissaoId)
        {
            var membros = await _context.MembrosComissao
                .Include(m => m.Profissional)
                .ThenInclude(p => p.Uf)
                .Where(m => m.ComissaoEleitoralId == comissaoId && m.Ativo)
                .AsNoTracking()
                .ToListAsync();
                
            var total = membros.Count;
            if (total == 0) return new EstatisticaComissao();
            
            var mulheres = membros.Count(m => m.Profissional.Genero == "F");
            var regioes = membros.Select(m => m.Profissional.Uf.Regiao).Distinct().Count();
            
            return new EstatisticaComissao
            {
                TotalMembros = total,
                TotalMulheres = mulheres,
                PercentualFeminino = total > 0 ? (mulheres * 100.0m) / total : 0,
                DiversidadeRegional = regioes,
                IdadeMediaMembros = membros.Average(m => DateTime.Now.Year - m.Profissional.DataNascimento.Year)
            };
        }
        
        public async Task<bool> ComissaoEstaCompletaAsync(int comissaoId)
        {
            return await ComissaoTemMembrosMinimosAsync(comissaoId) &&
                   await ValidarComposicaoComissaoAsync(comissaoId);
        }
        
        public async Task<DateTime?> GetPrazoConstituicaoAsync(int comissaoId)
        {
            var comissao = await _dbSet
                .FirstOrDefaultAsync(c => c.Id == comissaoId);
                
            return comissao?.DataLimiteConstituicao;
        }
        
        public async Task<IEnumerable<ComissaoEleitoral>> GetComissoesVencendoMandatoAsync(int diasAlerta = 30)
        {
            var dataLimite = DateTime.Now.AddDays(diasAlerta);
            
            return await _dbSet
                .Include(c => c.Eleicao)
                .Include(c => c.Uf)
                .Where(c => c.DataFimMandato <= dataLimite && 
                           c.DataFimMandato > DateTime.Now &&
                           !c.Excluida)
                .OrderBy(c => c.DataFimMandato)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public override async Task<ComissaoEleitoral> UpdateAsync(ComissaoEleitoral entity)
        {
            // Invalidate related caches
            InvalidateCacheForEntity(entity.Id);
            _cache.Remove(GetCacheKey("por_eleicao", entity.EleicaoId));
            _cache.Remove(GetCacheKey("nacional", entity.EleicaoId));
            
            return await base.UpdateAsync(entity);
        }
    }

    public class EstatisticaComissao
    {
        public int TotalMembros { get; set; }
        public int TotalMulheres { get; set; }
        public decimal PercentualFeminino { get; set; }
        public int DiversidadeRegional { get; set; }
        public double IdadeMediaMembros { get; set; }
    }

    public enum TipoComissao
    {
        Nacional = 1,
        Estadual = 2,
        Regional = 3
    }

    // Supporting entities that would be defined elsewhere
    public class MembroComissao
    {
        public int Id { get; set; }
        public int ComissaoEleitoralId { get; set; }
        public int ProfissionalId { get; set; }
        public int CargoComissaoId { get; set; }
        public DateTime DataNomeacao { get; set; }
        public DateTime? DataPosse { get; set; }
        public bool Ativo { get; set; }
        
        public virtual ComissaoEleitoral ComissaoEleitoral { get; set; } = null!;
        public virtual Profissional Profissional { get; set; } = null!;
        public virtual CargoComissao CargoComissao { get; set; } = null!;
        public virtual StatusMembro StatusMembro { get; set; } = null!;
    }

    public class CargoComissao
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Ordem { get; set; }
        public string Descricao { get; set; } = string.Empty;
    }

    public class StatusMembro
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; }
    }
}