using System.Threading.Tasks;

namespace Eleitoral.Application.Interfaces
{
    /// <summary>
    /// Interface do serviço de notificações
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Notifica o início da apuração
        /// </summary>
        Task NotificarInicioApuracaoAsync(int eleicaoId);
        
        /// <summary>
        /// Notifica atualização no progresso da apuração
        /// </summary>
        Task NotificarAtualizacaoApuracaoAsync(int resultadoApuracaoId, decimal percentualApuracao);
        
        /// <summary>
        /// Notifica o fim da apuração
        /// </summary>
        Task NotificarFimApuracaoAsync(int eleicaoId, int resultadoApuracaoId);
        
        /// <summary>
        /// Notifica a reabertura da apuração
        /// </summary>
        Task NotificarReaberturaApuracaoAsync(int eleicaoId, string motivo);
        
        /// <summary>
        /// Notifica erro na apuração
        /// </summary>
        Task NotificarErroApuracaoAsync(int eleicaoId, string erro);
        
        /// <summary>
        /// Envia notificação em tempo real via WebSocket/SignalR
        /// </summary>
        Task EnviarNotificacaoTempoRealAsync(string canal, object dados);
    }
}