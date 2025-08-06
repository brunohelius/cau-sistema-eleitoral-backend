using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Application.DTOs.ImpugnacaoResultado;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface do serviço de impugnações de resultado
    /// </summary>
    public interface IImpugnacaoResultadoService
    {
        // Operações principais
        Task<ImpugnacaoResultadoDTO> RegistrarImpugnacaoAsync(RegistrarImpugnacaoResultadoDTO dto);
        Task<AlegacaoImpugnacaoResultadoDTO> AdicionarAlegacaoAsync(AdicionarAlegacaoDTO dto);
        Task<RecursoImpugnacaoResultadoDTO> AdicionarRecursoAsync(AdicionarRecursoDTO dto);
        Task<ContrarrazaoImpugnacaoResultadoDTO> AdicionarContrarrazaoAsync(AdicionarContrarrazaoDTO dto);
        
        // Julgamentos
        Task<JulgamentoAlegacaoImpugResultadoDTO> JulgarAlegacaoAsync(JulgarAlegacaoDTO dto);
        Task<JulgamentoRecursoImpugResultadoDTO> JulgarRecursoAsync(JulgarRecursoDTO dto);
        
        // Consultas
        Task<ImpugnacaoResultadoDetalheDTO?> ObterPorIdAsync(int id);
        Task<IEnumerable<ImpugnacaoResultadoDTO>> ListarPorCalendarioAsync(int calendarioId);
        Task<IEnumerable<ImpugnacaoResultadoDTO>> ListarPorProfissionalAsync(int profissionalId);
        Task<IEnumerable<ImpugnacaoResultadoDTO>> ListarPendentesJulgamentoAsync();
    }
}