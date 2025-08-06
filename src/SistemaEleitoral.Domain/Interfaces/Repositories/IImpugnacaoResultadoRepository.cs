using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Interface do repositório de impugnações de resultado
    /// </summary>
    public interface IImpugnacaoResultadoRepository : IBaseRepository<ImpugnacaoResultado>
    {
        Task<ImpugnacaoResultado?> GetByIdWithRelationsAsync(int id);
        Task<IEnumerable<ImpugnacaoResultado>> GetByCalendarioAsync(int calendarioId);
        Task<IEnumerable<ImpugnacaoResultado>> GetByProfissionalAsync(int profissionalId);
        Task<IEnumerable<ImpugnacaoResultado>> GetPendentesJulgamentoAsync();
        Task<bool> ExisteImpugnacaoParaCalendarioAsync(int calendarioId, int profissionalId);
        Task<int> GetProximoNumeroAsync(int calendarioId);
        
        // Alegações
        Task<IEnumerable<AlegacaoImpugnacaoResultado>> GetAlegacoesAsync(int impugnacaoId);
        Task<AlegacaoImpugnacaoResultado> AddAlegacaoAsync(AlegacaoImpugnacaoResultado alegacao);
        
        // Recursos
        Task<IEnumerable<RecursoImpugnacaoResultado>> GetRecursosAsync(int impugnacaoId);
        Task<RecursoImpugnacaoResultado> AddRecursoAsync(RecursoImpugnacaoResultado recurso);
        
        // Contrarrazões
        Task<ContrarrazaoImpugnacaoResultado> AddContrarrazaoAsync(ContrarrazaoImpugnacaoResultado contrarrazao);
        
        // Julgamentos
        Task<JulgamentoAlegacaoImpugResultado> AddJulgamentoAlegacaoAsync(JulgamentoAlegacaoImpugResultado julgamento);
        Task<JulgamentoRecursoImpugResultado> AddJulgamentoRecursoAsync(JulgamentoRecursoImpugResultado julgamento);
        
        // Status
        Task AtualizarStatusAsync(int impugnacaoId, StatusImpugnacaoResultadoEnum novoStatus);
    }
}