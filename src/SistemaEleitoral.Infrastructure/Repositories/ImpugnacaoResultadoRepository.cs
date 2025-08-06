using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces.Repositories;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Infrastructure.Repositories
{
    /// <summary>
    /// Repositório para gerenciamento de impugnações de resultado
    /// </summary>
    public class ImpugnacaoResultadoRepository : BaseRepository<ImpugnacaoResultado>, IImpugnacaoResultadoRepository
    {
        private readonly ApplicationDbContext _context;

        public ImpugnacaoResultadoRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Busca impugnação de resultado por ID com relacionamentos
        /// </summary>
        public async Task<ImpugnacaoResultado?> GetByIdWithRelationsAsync(int id)
        {
            return await _context.ImpugnacoesResultado
                .Include(i => i.Profissional)
                .Include(i => i.Status)
                .Include(i => i.Calendario)
                .Include(i => i.Alegacoes)
                    .ThenInclude(a => a.Profissional)
                .Include(i => i.Recursos)
                    .ThenInclude(r => r.Profissional)
                .Include(i => i.Contrarrazoes)
                    .ThenInclude(c => c.Profissional)
                .Include(i => i.JulgamentoAlegacao)
                .Include(i => i.JulgamentoRecurso)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        /// <summary>
        /// Lista impugnações de resultado por calendário
        /// </summary>
        public async Task<IEnumerable<ImpugnacaoResultado>> GetByCalendarioAsync(int calendarioId)
        {
            return await _context.ImpugnacoesResultado
                .Include(i => i.Profissional)
                .Include(i => i.Status)
                .Where(i => i.CalendarioId == calendarioId)
                .OrderByDescending(i => i.DataCadastro)
                .ToListAsync();
        }

        /// <summary>
        /// Lista impugnações de resultado por profissional
        /// </summary>
        public async Task<IEnumerable<ImpugnacaoResultado>> GetByProfissionalAsync(int profissionalId)
        {
            return await _context.ImpugnacoesResultado
                .Include(i => i.Status)
                .Include(i => i.Calendario)
                .Where(i => i.ProfissionalId == profissionalId)
                .OrderByDescending(i => i.DataCadastro)
                .ToListAsync();
        }

        /// <summary>
        /// Lista impugnações pendentes de julgamento
        /// </summary>
        public async Task<IEnumerable<ImpugnacaoResultado>> GetPendentesJulgamentoAsync()
        {
            var statusPendentes = new[] { 
                (int)StatusImpugnacaoResultadoEnum.AguardandoJulgamento,
                (int)StatusImpugnacaoResultadoEnum.EmRecurso
            };

            return await _context.ImpugnacoesResultado
                .Include(i => i.Profissional)
                .Include(i => i.Calendario)
                .Include(i => i.Status)
                .Where(i => statusPendentes.Contains(i.StatusId))
                .OrderBy(i => i.DataCadastro)
                .ToListAsync();
        }

        /// <summary>
        /// Verifica se existe impugnação para o calendário
        /// </summary>
        public async Task<bool> ExisteImpugnacaoParaCalendarioAsync(int calendarioId, int profissionalId)
        {
            return await _context.ImpugnacoesResultado
                .AnyAsync(i => i.CalendarioId == calendarioId && 
                              i.ProfissionalId == profissionalId &&
                              i.StatusId != (int)StatusImpugnacaoResultadoEnum.Arquivada);
        }

        /// <summary>
        /// Obtém o próximo número de protocolo
        /// </summary>
        public async Task<int> GetProximoNumeroAsync(int calendarioId)
        {
            var ultimoNumero = await _context.ImpugnacoesResultado
                .Where(i => i.CalendarioId == calendarioId)
                .MaxAsync(i => (int?)i.Numero) ?? 0;

            return ultimoNumero + 1;
        }

        /// <summary>
        /// Lista alegações de uma impugnação
        /// </summary>
        public async Task<IEnumerable<AlegacaoImpugnacaoResultado>> GetAlegacoesAsync(int impugnacaoId)
        {
            return await _context.AlegacoesImpugnacaoResultado
                .Include(a => a.Profissional)
                .Include(a => a.ChapaEleicao)
                .Where(a => a.ImpugnacaoResultadoId == impugnacaoId)
                .OrderBy(a => a.DataCadastro)
                .ToListAsync();
        }

        /// <summary>
        /// Lista recursos de uma impugnação
        /// </summary>
        public async Task<IEnumerable<RecursoImpugnacaoResultado>> GetRecursosAsync(int impugnacaoId)
        {
            return await _context.RecursosImpugnacaoResultado
                .Include(r => r.Profissional)
                .Include(r => r.TipoRecurso)
                .Include(r => r.Contrarrazao)
                .Where(r => r.ImpugnacaoResultadoId == impugnacaoId)
                .OrderBy(r => r.DataCadastro)
                .ToListAsync();
        }

        /// <summary>
        /// Adiciona alegação à impugnação
        /// </summary>
        public async Task<AlegacaoImpugnacaoResultado> AddAlegacaoAsync(AlegacaoImpugnacaoResultado alegacao)
        {
            _context.AlegacoesImpugnacaoResultado.Add(alegacao);
            await _context.SaveChangesAsync();
            return alegacao;
        }

        /// <summary>
        /// Adiciona recurso à impugnação
        /// </summary>
        public async Task<RecursoImpugnacaoResultado> AddRecursoAsync(RecursoImpugnacaoResultado recurso)
        {
            _context.RecursosImpugnacaoResultado.Add(recurso);
            await _context.SaveChangesAsync();
            return recurso;
        }

        /// <summary>
        /// Adiciona contrarrazão ao recurso
        /// </summary>
        public async Task<ContrarrazaoImpugnacaoResultado> AddContrarrazaoAsync(ContrarrazaoImpugnacaoResultado contrarrazao)
        {
            _context.ContrarrazoesImpugnacaoResultado.Add(contrarrazao);
            await _context.SaveChangesAsync();
            return contrarrazao;
        }

        /// <summary>
        /// Registra julgamento de alegação
        /// </summary>
        public async Task<JulgamentoAlegacaoImpugResultado> AddJulgamentoAlegacaoAsync(JulgamentoAlegacaoImpugResultado julgamento)
        {
            _context.JulgamentosAlegacaoImpugResultado.Add(julgamento);
            await _context.SaveChangesAsync();
            return julgamento;
        }

        /// <summary>
        /// Registra julgamento de recurso
        /// </summary>
        public async Task<JulgamentoRecursoImpugResultado> AddJulgamentoRecursoAsync(JulgamentoRecursoImpugResultado julgamento)
        {
            _context.JulgamentosRecursoImpugResultado.Add(julgamento);
            await _context.SaveChangesAsync();
            return julgamento;
        }

        /// <summary>
        /// Atualiza status da impugnação
        /// </summary>
        public async Task AtualizarStatusAsync(int impugnacaoId, StatusImpugnacaoResultadoEnum novoStatus)
        {
            var impugnacao = await GetByIdAsync(impugnacaoId);
            if (impugnacao != null)
            {
                impugnacao.AtualizarStatus(novoStatus);
                await UpdateAsync(impugnacao);
            }
        }
    }
}