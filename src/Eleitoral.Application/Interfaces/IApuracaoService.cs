using System.Collections.Generic;
using System.Threading.Tasks;
using Eleitoral.Application.DTOs.Apuracao;

namespace Eleitoral.Application.Interfaces
{
    /// <summary>
    /// Interface do serviço de apuração
    /// </summary>
    public interface IApuracaoService
    {
        /// <summary>
        /// Inicia a apuração de uma eleição
        /// </summary>
        Task<ResultadoApuracaoDto> IniciarApuracaoAsync(int eleicaoId);
        
        /// <summary>
        /// Processa um boletim de urna
        /// </summary>
        Task<ResultadoApuracaoDto> ProcessarBoletimUrnaAsync(ProcessarBoletimDto dto);
        
        /// <summary>
        /// Obtém o resultado da apuração em tempo real
        /// </summary>
        Task<ResultadoApuracaoDto> ObterResultadoTempoRealAsync(int eleicaoId);
        
        /// <summary>
        /// Finaliza a apuração
        /// </summary>
        Task<ResultadoApuracaoDto> FinalizarApuracaoAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Audita o resultado da apuração
        /// </summary>
        Task<ResultadoApuracaoDto> AuditarApuracaoAsync(int resultadoApuracaoId, string auditorId);
        
        /// <summary>
        /// Reabre a apuração para correções
        /// </summary>
        Task<ResultadoApuracaoDto> ReabrirApuracaoAsync(int resultadoApuracaoId, string motivo);
        
        /// <summary>
        /// Obtém as estatísticas da apuração
        /// </summary>
        Task<EstatisticasApuracaoDto> ObterEstatisticasAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Obtém os boletins de urna pendentes
        /// </summary>
        Task<IEnumerable<BoletimUrnaDto>> ObterBoletinsPendentesAsync();
        
        /// <summary>
        /// Obtém os logs da apuração
        /// </summary>
        Task<IEnumerable<LogApuracaoDto>> ObterLogsApuracaoAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Valida a integridade da apuração
        /// </summary>
        Task<bool> ValidarIntegridadeApuracaoAsync(int resultadoApuracaoId);
    }
}