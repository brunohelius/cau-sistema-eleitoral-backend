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
    /// Implementação do repositório para BoletimUrna
    /// </summary>
    public class BoletimUrnaRepository : BaseRepository<BoletimUrna>, IBoletimUrnaRepository
    {
        public BoletimUrnaRepository(ApplicationDbContext context) : base(context)
        {
        }
        
        public async Task<BoletimUrna> ObterPorNumeroUrnaAsync(int numeroUrna, int resultadoApuracaoId)
        {
            return await _dbSet
                .Include(b => b.VotosChapas)
                .FirstOrDefaultAsync(b => b.NumeroUrna == numeroUrna && 
                                         b.ResultadoApuracaoId == resultadoApuracaoId);
        }
        
        public async Task<BoletimUrna> ObterPorCodigoIdentificacaoAsync(string codigoIdentificacao)
        {
            return await _dbSet
                .Include(b => b.VotosChapas)
                .Include(b => b.ResultadoApuracao)
                .FirstOrDefaultAsync(b => b.CodigoIdentificacao == codigoIdentificacao);
        }
        
        public async Task<IEnumerable<BoletimUrna>> ObterPorStatusAsync(StatusBoletim status)
        {
            return await _dbSet
                .Include(b => b.ResultadoApuracao)
                .Where(b => b.Status == status)
                .OrderBy(b => b.NumeroUrna)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<BoletimUrna>> ObterPendentesAsync()
        {
            return await _dbSet
                .Include(b => b.ResultadoApuracao)
                .Where(b => b.Status == StatusBoletim.Pendente)
                .OrderBy(b => b.DataHoraProcessamento)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<BoletimUrna>> ObterProcessadosAsync(int resultadoApuracaoId)
        {
            return await _dbSet
                .Include(b => b.VotosChapas)
                .Where(b => b.ResultadoApuracaoId == resultadoApuracaoId && 
                          (b.Status == StatusBoletim.Processado || b.Status == StatusBoletim.Conferido))
                .OrderBy(b => b.NumeroUrna)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<BoletimUrna>> ObterPorLocalVotacaoAsync(string localVotacao)
        {
            return await _dbSet
                .Include(b => b.ResultadoApuracao)
                .Where(b => b.LocalVotacao.Contains(localVotacao))
                .OrderBy(b => b.NumeroUrna)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<BoletimUrna>> ObterPorZonaAsync(string zona)
        {
            return await _dbSet
                .Include(b => b.ResultadoApuracao)
                .Where(b => b.Zona == zona)
                .OrderBy(b => b.NumeroUrna)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<BoletimUrna>> ObterPorSecaoAsync(string secao)
        {
            return await _dbSet
                .Include(b => b.ResultadoApuracao)
                .Where(b => b.Secao == secao)
                .OrderBy(b => b.NumeroUrna)
                .ToListAsync();
        }
        
        public async Task<bool> BoletimJaProcessadoAsync(int numeroUrna, int resultadoApuracaoId)
        {
            return await _dbSet
                .AnyAsync(b => b.NumeroUrna == numeroUrna && 
                             b.ResultadoApuracaoId == resultadoApuracaoId &&
                             b.Status != StatusBoletim.Pendente);
        }
        
        public async Task<IEnumerable<BoletimUrna>> ObterNaoConferidosAsync(int resultadoApuracaoId)
        {
            return await _dbSet
                .Include(b => b.VotosChapas)
                .Where(b => b.ResultadoApuracaoId == resultadoApuracaoId && 
                          b.Status == StatusBoletim.Processado && 
                          !b.Conferido)
                .OrderBy(b => b.NumeroUrna)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<BoletimUrna>> ObterRejeitadosAsync(int resultadoApuracaoId)
        {
            return await _dbSet
                .Where(b => b.ResultadoApuracaoId == resultadoApuracaoId && 
                          b.Status == StatusBoletim.Rejeitado)
                .OrderBy(b => b.NumeroUrna)
                .ToListAsync();
        }
        
        public async Task<int> ObterTotalProcessadasAsync(int resultadoApuracaoId)
        {
            return await _dbSet
                .CountAsync(b => b.ResultadoApuracaoId == resultadoApuracaoId && 
                               (b.Status == StatusBoletim.Processado || 
                                b.Status == StatusBoletim.Conferido));
        }
        
        public async Task<IEnumerable<VotoChapa>> ObterVotosChapaAsync(int boletimUrnaId)
        {
            var boletim = await _dbSet
                .Include(b => b.VotosChapas)
                .FirstOrDefaultAsync(b => b.Id == boletimUrnaId);
                
            return boletim?.VotosChapas ?? new List<VotoChapa>();
        }
        
        public async Task<BoletimUrna> SalvarComVotosAsync(BoletimUrna boletimUrna)
        {
            if (boletimUrna.Id == 0)
            {
                await _dbSet.AddAsync(boletimUrna);
            }
            else
            {
                _dbSet.Update(boletimUrna);
            }
            
            await _context.SaveChangesAsync();
            return boletimUrna;
        }
        
        public async Task<(int Total, int Processados, int Pendentes, int Rejeitados)> ObterEstatisticasAsync(int resultadoApuracaoId)
        {
            var boletins = await _dbSet
                .Where(b => b.ResultadoApuracaoId == resultadoApuracaoId)
                .Select(b => b.Status)
                .ToListAsync();
                
            var total = boletins.Count;
            var processados = boletins.Count(s => s == StatusBoletim.Processado || s == StatusBoletim.Conferido);
            var pendentes = boletins.Count(s => s == StatusBoletim.Pendente);
            var rejeitados = boletins.Count(s => s == StatusBoletim.Rejeitado);
            
            return (total, processados, pendentes, rejeitados);
        }
    }
}