using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Eleitoral.Domain.Entities.Apuracao;
using Eleitoral.Domain.Interfaces.Repositories;
using Eleitoral.Infrastructure.Data;

namespace Eleitoral.Infrastructure.Repositories
{
    /// <summary>
    /// Implementação do repositório para ResultadoApuracao
    /// </summary>
    public class ResultadoApuracaoRepository : BaseRepository<ResultadoApuracao>, IResultadoApuracaoRepository
    {
        public ResultadoApuracaoRepository(ApplicationDbContext context) : base(context)
        {
        }
        
        public async Task<ResultadoApuracao> ObterPorEleicaoAsync(int eleicaoId)
        {
            return await _dbSet
                .Include(r => r.Eleicao)
                .Include(r => r.ResultadosChapas)
                    .ThenInclude(rc => rc.Chapa)
                .FirstOrDefaultAsync(r => r.EleicaoId == eleicaoId);
        }
        
        public async Task<ResultadoApuracao> ObterCompletoAsync(int id)
        {
            return await _dbSet
                .Include(r => r.Eleicao)
                .Include(r => r.ResultadosChapas)
                    .ThenInclude(rc => rc.Chapa)
                        .ThenInclude(c => c.MembrosChapa)
                .Include(r => r.BoletinsUrna)
                    .ThenInclude(b => b.VotosChapas)
                .Include(r => r.LogsApuracao)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        
        public async Task<IEnumerable<ResultadoApuracao>> ObterPorStatusAsync(StatusApuracao status)
        {
            return await _dbSet
                .Include(r => r.Eleicao)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.InicioApuracao)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<ResultadoApuracao>> ObterEmAndamentoAsync()
        {
            return await _dbSet
                .Include(r => r.Eleicao)
                .Include(r => r.ResultadosChapas)
                .Where(r => r.Status == StatusApuracao.EmAndamento)
                .OrderByDescending(r => r.InicioApuracao)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<ResultadoApuracao>> ObterFinalizadosAsync()
        {
            return await _dbSet
                .Include(r => r.Eleicao)
                .Include(r => r.ResultadosChapas)
                    .ThenInclude(rc => rc.Chapa)
                .Where(r => r.Status == StatusApuracao.Finalizada)
                .OrderByDescending(r => r.FimApuracao)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<ResultadoApuracao>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim)
        {
            return await _dbSet
                .Include(r => r.Eleicao)
                .Where(r => r.InicioApuracao >= inicio && r.InicioApuracao <= fim)
                .OrderByDescending(r => r.InicioApuracao)
                .ToListAsync();
        }
        
        public async Task<bool> ExisteApuracaoEmAndamentoAsync(int eleicaoId)
        {
            return await _dbSet
                .AnyAsync(r => r.EleicaoId == eleicaoId && 
                         (r.Status == StatusApuracao.EmAndamento || r.Status == StatusApuracao.Pausada));
        }
        
        public async Task<ResultadoApuracao> ObterUltimoResultadoAsync(int eleicaoId)
        {
            return await _dbSet
                .Include(r => r.Eleicao)
                .Include(r => r.ResultadosChapas)
                    .ThenInclude(rc => rc.Chapa)
                .Where(r => r.EleicaoId == eleicaoId)
                .OrderByDescending(r => r.InicioApuracao)
                .FirstOrDefaultAsync();
        }
        
        public async Task<IEnumerable<ResultadoChapa>> ObterResultadosChapasAsync(int resultadoApuracaoId)
        {
            var resultado = await _dbSet
                .Include(r => r.ResultadosChapas)
                    .ThenInclude(rc => rc.Chapa)
                        .ThenInclude(c => c.MembrosChapa)
                .FirstOrDefaultAsync(r => r.Id == resultadoApuracaoId);
                
            return resultado?.ResultadosChapas ?? new List<ResultadoChapa>();
        }
        
        public async Task<IEnumerable<BoletimUrna>> ObterBoletinsUrnaAsync(int resultadoApuracaoId)
        {
            var resultado = await _dbSet
                .Include(r => r.BoletinsUrna)
                    .ThenInclude(b => b.VotosChapas)
                .FirstOrDefaultAsync(r => r.Id == resultadoApuracaoId);
                
            return resultado?.BoletinsUrna ?? new List<BoletimUrna>();
        }
        
        public async Task<IEnumerable<LogApuracao>> ObterLogsApuracaoAsync(int resultadoApuracaoId)
        {
            var resultado = await _dbSet
                .Include(r => r.LogsApuracao)
                .FirstOrDefaultAsync(r => r.Id == resultadoApuracaoId);
                
            return resultado?.LogsApuracao.OrderByDescending(l => l.DataHora) ?? new List<LogApuracao>();
        }
        
        public async Task<EstatisticasApuracao> ObterEstatisticasAsync(int resultadoApuracaoId)
        {
            return await _context.Set<EstatisticasApuracao>()
                .Include(e => e.EstatisticasRegionais)
                .FirstOrDefaultAsync(e => e.ResultadoApuracaoId == resultadoApuracaoId);
        }
        
        public async Task<ResultadoApuracao> SalvarCompletoAsync(ResultadoApuracao resultadoApuracao)
        {
            if (resultadoApuracao.Id == 0)
            {
                await _dbSet.AddAsync(resultadoApuracao);
            }
            else
            {
                _dbSet.Update(resultadoApuracao);
            }
            
            await _context.SaveChangesAsync();
            return resultadoApuracao;
        }
        
        public async Task AtualizarTotaisAsync(int id, int totalVotantes, int votosValidos, int votosBrancos, int votosNulos)
        {
            var resultado = await _dbSet.FindAsync(id);
            if (resultado != null)
            {
                // Usar reflection ou método interno para atualizar propriedades privadas
                // Ou adicionar métodos públicos na entidade para atualização
                var propriedadeTotalVotantes = typeof(ResultadoApuracao).GetProperty("TotalVotantes");
                propriedadeTotalVotantes?.SetValue(resultado, totalVotantes);
                
                var propriedadeVotosValidos = typeof(ResultadoApuracao).GetProperty("VotosValidos");
                propriedadeVotosValidos?.SetValue(resultado, votosValidos);
                
                var propriedadeVotosBrancos = typeof(ResultadoApuracao).GetProperty("VotosBrancos");
                propriedadeVotosBrancos?.SetValue(resultado, votosBrancos);
                
                var propriedadeVotosNulos = typeof(ResultadoApuracao).GetProperty("VotosNulos");
                propriedadeVotosNulos?.SetValue(resultado, votosNulos);
                
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task<IEnumerable<ResultadoApuracao>> ObterPendentesAuditoriaAsync()
        {
            return await _dbSet
                .Include(r => r.Eleicao)
                .Include(r => r.ResultadosChapas)
                .Where(r => r.Status == StatusApuracao.Finalizada && !r.Auditado)
                .OrderBy(r => r.FimApuracao)
                .ToListAsync();
        }
    }
}