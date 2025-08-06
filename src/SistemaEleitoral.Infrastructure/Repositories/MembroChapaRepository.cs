using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    public interface IMembroChapaRepository : IRepository<MembroChapa>
    {
        Task<IEnumerable<MembroChapa>> GetMembrosPorChapaAsync(int chapaId);
        Task<MembroChapa?> GetMembroPorProfissionalEChapaAsync(int profissionalId, int chapaId);
        Task<bool> ProfissionalJaEMembroAsync(int profissionalId, int eleicaoId);
        Task<IEnumerable<MembroChapa>> GetMembrosPorCargoAsync(int chapaId, string cargo);
        Task<IEnumerable<MembroChapa>> GetMembrosComPendenciasAsync(int chapaId);
        Task<bool> ChapaTemMembrosMinimosAsync(int chapaId);
        Task<bool> ChapaTemDiversidadeMinimaMAsync(int chapaId);
        Task<int> ContarMembrosPorTipoAsync(int chapaId, TipoMembro tipo);
        Task<IEnumerable<MembroChapa>> GetTitularesAsync(int chapaId);
        Task<IEnumerable<MembroChapa>> GetSuplentesAsync(int chapaId);
        Task<bool> ValidarComposicaoChapaAsync(int chapaId);
        Task<IEnumerable<MembroChapa>> GetMembrosAprovadosAsync(int chapaId);
        Task<IEnumerable<MembroChapa>> GetMembrosComImpugnacoesAsync(int chapaId);
        Task<bool> MembroTemPendenciaAsync(int membroId);
        Task<EstatisticaDiversidade> GetEstatisticaDiversidadeAsync(int chapaId);
        Task<bool> PodeRemoverMembroAsync(int membroId);
        Task<bool> PodeAlterarOrdemAsync(int chapaId);
        Task<IEnumerable<MembroChapa>> GetMembrosParaValidacaoAsync();
    }

    public class MembroChapaRepository : BaseRepository<MembroChapa>, IMembroChapaRepository
    {
        public MembroChapaRepository(
            ApplicationDbContext context, 
            IMemoryCache cache, 
            ILogger<MembroChapaRepository> logger) 
            : base(context, cache, logger) { }
        
        public async Task<IEnumerable<MembroChapa>> GetMembrosPorChapaAsync(int chapaId)
        {
            var cacheKey = GetCacheKey("membros_chapa", chapaId);
            
            return await GetFromCacheOrExecuteAsync(
                cacheKey,
                async () => await _dbSet
                    .Include(m => m.Profissional)
                    .ThenInclude(p => p.Uf)
                    .Include(m => m.Profissional)
                    .ThenInclude(p => p.StatusProfissional)
                    .Include(m => m.ChapaEleicao)
                    .Include(m => m.CargoMembro)
                    .Include(m => m.StatusMembro)
                    .Where(m => m.ChapaEleicaoId == chapaId && !m.Excluido)
                    .OrderBy(m => m.Ordem)
                    .ThenBy(m => m.TipoMembro)
                    .ThenBy(m => m.Profissional.Nome)
                    .AsNoTracking()
                    .ToListAsync(),
                TimeSpan.FromMinutes(15)
            );
        }
        
        public async Task<MembroChapa?> GetMembroPorProfissionalEChapaAsync(int profissionalId, int chapaId)
        {
            return await _dbSet
                .Include(m => m.Profissional)
                .Include(m => m.ChapaEleicao)
                .FirstOrDefaultAsync(m => m.ProfissionalId == profissionalId && 
                                         m.ChapaEleicaoId == chapaId && 
                                         !m.Excluido);
        }
        
        public async Task<bool> ProfissionalJaEMembroAsync(int profissionalId, int eleicaoId)
        {
            return await _dbSet
                .Include(m => m.ChapaEleicao)
                .AnyAsync(m => m.ProfissionalId == profissionalId &&
                              m.ChapaEleicao.EleicaoId == eleicaoId &&
                              !m.Excluido &&
                              !m.ChapaEleicao.Excluida);
        }
        
        public async Task<IEnumerable<MembroChapa>> GetMembrosPorCargoAsync(int chapaId, string cargo)
        {
            return await _dbSet
                .Include(m => m.Profissional)
                .Include(m => m.CargoMembro)
                .Where(m => m.ChapaEleicaoId == chapaId && 
                           m.CargoMembro.Nome == cargo && 
                           !m.Excluido)
                .OrderBy(m => m.TipoMembro)
                .ThenBy(m => m.Ordem)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<IEnumerable<MembroChapa>> GetMembrosComPendenciasAsync(int chapaId)
        {
            return await _dbSet
                .Include(m => m.Profissional)
                .ThenInclude(p => p.PendenciasFinanceiras)
                .Include(m => m.Profissional)
                .ThenInclude(p => p.PendenciasEticas)
                .Include(m => m.Profissional)
                .ThenInclude(p => p.StatusProfissional)
                .Where(m => m.ChapaEleicaoId == chapaId && 
                           !m.Excluido &&
                           (m.Profissional.PendenciasFinanceiras.Any(pf => pf.Ativa) ||
                            m.Profissional.PendenciasEticas.Any(pe => pe.Ativa) ||
                            !m.Profissional.StatusProfissional.PermiteParticiparEleicao))
                .OrderBy(m => m.Ordem)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> ChapaTemMembrosMinimosAsync(int chapaId)
        {
            var chapa = await _context.ChapasEleicao
                .Include(c => c.Eleicao)
                .FirstOrDefaultAsync(c => c.Id == chapaId);
                
            if (chapa == null) return false;
            
            var membrosAtivos = await _dbSet
                .CountAsync(m => m.ChapaEleicaoId == chapaId && !m.Excluido);
                
            // Regras específicas por tipo de eleição
            return chapa.Eleicao.TipoEleicao switch
            {
                TipoEleicao.Federal => membrosAtivos >= 10, // 5 titulares + 5 suplentes
                TipoEleicao.Estadual => membrosAtivos >= 6, // 3 titulares + 3 suplentes
                TipoEleicao.IES => membrosAtivos >= 4,      // 2 titulares + 2 suplentes
                _ => membrosAtivos >= 2
            };
        }
        
        public async Task<bool> ChapaTemDiversidadeMinimaMAsync(int chapaId)
        {
            var estatisticas = await GetEstatisticaDiversidadeAsync(chapaId);
            
            // Pelo menos 30% de representação feminina
            if (estatisticas.PercentualFeminino < 30) return false;
            
            // Pelo menos uma pessoa de etnia não branca (se tiver mais de 5 membros)
            if (estatisticas.TotalMembros > 5 && estatisticas.DiversidadeEtnica == 0) return false;
            
            return true;
        }
        
        public async Task<int> ContarMembrosPorTipoAsync(int chapaId, TipoMembro tipo)
        {
            return await _dbSet
                .CountAsync(m => m.ChapaEleicaoId == chapaId && 
                                m.TipoMembro == tipo && 
                                !m.Excluido);
        }
        
        public async Task<IEnumerable<MembroChapa>> GetTitularesAsync(int chapaId)
        {
            return await _dbSet
                .Include(m => m.Profissional)
                .Include(m => m.CargoMembro)
                .Where(m => m.ChapaEleicaoId == chapaId && 
                           m.TipoMembro == TipoMembro.Titular && 
                           !m.Excluido)
                .OrderBy(m => m.Ordem)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<IEnumerable<MembroChapa>> GetSuplentesAsync(int chapaId)
        {
            return await _dbSet
                .Include(m => m.Profissional)
                .Include(m => m.CargoMembro)
                .Where(m => m.ChapaEleicaoId == chapaId && 
                           m.TipoMembro == TipoMembro.Suplente && 
                           !m.Excluido)
                .OrderBy(m => m.Ordem)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> ValidarComposicaoChapaAsync(int chapaId)
        {
            var chapa = await _context.ChapasEleicao
                .Include(c => c.Eleicao)
                .FirstOrDefaultAsync(c => c.Id == chapaId);
                
            if (chapa == null) return false;
            
            var membros = await GetMembrosPorChapaAsync(chapaId);
            var titulares = membros.Where(m => m.TipoMembro == TipoMembro.Titular);
            var suplentes = membros.Where(m => m.TipoMembro == TipoMembro.Suplente);
            
            // Verificar se tem o número correto de titulares e suplentes
            var (requiredTitulares, requiredSuplentes) = chapa.Eleicao.TipoEleicao switch
            {
                TipoEleicao.Federal => (5, 5),
                TipoEleicao.Estadual => (3, 3),
                TipoEleicao.IES => (2, 2),
                _ => (1, 1)
            };
            
            if (titulares.Count() != requiredTitulares || suplentes.Count() != requiredSuplentes)
                return false;
            
            // Verificar se todos os membros são elegíveis
            var membrosComProblemas = await GetMembrosComPendenciasAsync(chapaId);
            if (membrosComProblemas.Any()) return false;
            
            // Verificar diversidade mínima
            if (!await ChapaTemDiversidadeMinimaMAsync(chapaId)) return false;
            
            return true;
        }
        
        public async Task<IEnumerable<MembroChapa>> GetMembrosAprovadosAsync(int chapaId)
        {
            return await _dbSet
                .Include(m => m.Profissional)
                .Include(m => m.StatusMembro)
                .Where(m => m.ChapaEleicaoId == chapaId && 
                           m.StatusMembro.Nome == "Aprovado" && 
                           !m.Excluido)
                .OrderBy(m => m.Ordem)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<IEnumerable<MembroChapa>> GetMembrosComImpugnacoesAsync(int chapaId)
        {
            return await _dbSet
                .Include(m => m.Profissional)
                .Include(m => m.PedidosImpugnacao)
                .Where(m => m.ChapaEleicaoId == chapaId && 
                           m.PedidosImpugnacao.Any(pi => !pi.Excluido && 
                                                        pi.Status != StatusImpugnacao.Julgada) &&
                           !m.Excluido)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public async Task<bool> MembroTemPendenciaAsync(int membroId)
        {
            var membro = await _dbSet
                .Include(m => m.Profissional)
                .ThenInclude(p => p.PendenciasFinanceiras)
                .Include(m => m.Profissional)
                .ThenInclude(p => p.PendenciasEticas)
                .FirstOrDefaultAsync(m => m.Id == membroId);
                
            if (membro == null) return true; // Se não encontrar, considera como pendência
            
            return membro.Profissional.PendenciasFinanceiras.Any(pf => pf.Ativa) ||
                   membro.Profissional.PendenciasEticas.Any(pe => pe.Ativa);
        }
        
        public async Task<EstatisticaDiversidade> GetEstatisticaDiversidadeAsync(int chapaId)
        {
            var membros = await _dbSet
                .Include(m => m.Profissional)
                .Where(m => m.ChapaEleicaoId == chapaId && !m.Excluido)
                .AsNoTracking()
                .ToListAsync();
                
            var total = membros.Count;
            if (total == 0) return new EstatisticaDiversidade();
            
            var mulheres = membros.Count(m => m.Profissional.Genero == "F");
            var naosBrancos = membros.Count(m => m.Profissional.Etnia != "Branca");
            var lgbtqi = membros.Count(m => m.Profissional.IdentidadeLGBTQI == true);
            var deficientes = membros.Count(m => m.Profissional.PessoaComDeficiencia == true);
            
            return new EstatisticaDiversidade
            {
                TotalMembros = total,
                TotalMulheres = mulheres,
                PercentualFeminino = (mulheres * 100.0m) / total,
                DiversidadeEtnica = naosBrancos,
                PercentualNaoBranco = (naosBrancos * 100.0m) / total,
                RepresentacaoLGBTQI = lgbtqi,
                PessoasComDeficiencia = deficientes
            };
        }
        
        public async Task<bool> PodeRemoverMembroAsync(int membroId)
        {
            var membro = await _dbSet
                .Include(m => m.ChapaEleicao)
                .ThenInclude(c => c.Eleicao)
                .ThenInclude(e => e.Calendario)
                .FirstOrDefaultAsync(m => m.Id == membroId);
                
            if (membro == null) return false;
            
            // Verificar se ainda está no prazo de edição
            var calendario = membro.ChapaEleicao.Eleicao.Calendario;
            var agora = DateTime.Now;
            var prazoEdicao = calendario?.PrazosCalendario?
                .FirstOrDefault(p => p.TipoAtividade == "CADASTRO_CHAPAS");
                
            if (prazoEdicao == null) return false;
            
            return agora >= prazoEdicao.DataInicio && agora <= prazoEdicao.DataFim;
        }
        
        public async Task<bool> PodeAlterarOrdemAsync(int chapaId)
        {
            var chapa = await _context.ChapasEleicao
                .Include(c => c.Eleicao)
                .ThenInclude(e => e.Calendario)
                .ThenInclude(c => c.PrazosCalendario)
                .FirstOrDefaultAsync(c => c.Id == chapaId);
                
            if (chapa == null) return false;
            
            // Só pode alterar ordem durante o período de cadastro
            var agora = DateTime.Now;
            var prazoEdicao = chapa.Eleicao.Calendario.PrazosCalendario
                .FirstOrDefault(p => p.TipoAtividade == "CADASTRO_CHAPAS");
                
            return prazoEdicao != null && 
                   agora >= prazoEdicao.DataInicio && 
                   agora <= prazoEdicao.DataFim &&
                   chapa.Status != StatusChapa.Aprovada;
        }
        
        public async Task<IEnumerable<MembroChapa>> GetMembrosParaValidacaoAsync()
        {
            return await _dbSet
                .Include(m => m.Profissional)
                .Include(m => m.ChapaEleicao)
                .ThenInclude(c => c.Eleicao)
                .Include(m => m.StatusMembro)
                .Where(m => m.StatusMembro.Nome == "Pendente Validacao" && !m.Excluido)
                .OrderBy(m => m.DataCadastro)
                .AsNoTracking()
                .ToListAsync();
        }
        
        public override async Task<MembroChapa> UpdateAsync(MembroChapa entity)
        {
            // Invalidate related caches
            InvalidateCacheForEntity(entity.Id);
            _cache.Remove(GetCacheKey("membros_chapa", entity.ChapaEleicaoId));
            
            return await base.UpdateAsync(entity);
        }
    }

    public class EstatisticaDiversidade
    {
        public int TotalMembros { get; set; }
        public int TotalMulheres { get; set; }
        public decimal PercentualFeminino { get; set; }
        public int DiversidadeEtnica { get; set; }
        public decimal PercentualNaoBranco { get; set; }
        public int RepresentacaoLGBTQI { get; set; }
        public int PessoasComDeficiencia { get; set; }
    }

    // Supporting enums that would be defined elsewhere
    public enum TipoMembro
    {
        Titular = 1,
        Suplente = 2
    }
}