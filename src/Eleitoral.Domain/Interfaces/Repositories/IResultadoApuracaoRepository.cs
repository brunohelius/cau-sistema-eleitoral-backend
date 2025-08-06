using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eleitoral.Domain.Entities.Apuracao;

namespace Eleitoral.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Interface do repositório para ResultadoApuracao
    /// </summary>
    public interface IResultadoApuracaoRepository : IBaseRepository<ResultadoApuracao>
    {
        /// <summary>
        /// Obtém o resultado da apuração por eleição
        /// </summary>
        Task<ResultadoApuracao> ObterPorEleicaoAsync(int eleicaoId);
        
        /// <summary>
        /// Obtém o resultado da apuração com todas as relações carregadas
        /// </summary>
        Task<ResultadoApuracao> ObterCompletoAsync(int id);
        
        /// <summary>
        /// Obtém resultados de apuração por status
        /// </summary>
        Task<IEnumerable<ResultadoApuracao>> ObterPorStatusAsync(StatusApuracao status);
        
        /// <summary>
        /// Obtém resultados de apuração em andamento
        /// </summary>
        Task<IEnumerable<ResultadoApuracao>> ObterEmAndamentoAsync();
        
        /// <summary>
        /// Obtém resultados de apuração finalizados
        /// </summary>
        Task<IEnumerable<ResultadoApuracao>> ObterFinalizadosAsync();
        
        /// <summary>
        /// Obtém resultados de apuração por período
        /// </summary>
        Task<IEnumerable<ResultadoApuracao>> ObterPorPeriodoAsync(DateTime inicio, DateTime fim);
        
        /// <summary>
        /// Verifica se existe apuração em andamento para a eleição
        /// </summary>
        Task<bool> ExisteApuracaoEmAndamentoAsync(int eleicaoId);
        
        /// <summary>
        /// Obtém o último resultado de apuração de uma eleição
        /// </summary>
        Task<ResultadoApuracao> ObterUltimoResultadoAsync(int eleicaoId);
        
        /// <summary>
        /// Obtém resultados de chapas por apuração
        /// </summary>
        Task<IEnumerable<ResultadoChapa>> ObterResultadosChapasAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Obtém boletins de urna por apuração
        /// </summary>
        Task<IEnumerable<BoletimUrna>> ObterBoletinsUrnaAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Obtém logs de apuração
        /// </summary>
        Task<IEnumerable<LogApuracao>> ObterLogsApuracaoAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Obtém estatísticas da apuração
        /// </summary>
        Task<EstatisticasApuracao> ObterEstatisticasAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Salva resultado de apuração com todas as dependências
        /// </summary>
        Task<ResultadoApuracao> SalvarCompletoAsync(ResultadoApuracao resultadoApuracao);
        
        /// <summary>
        /// Atualiza apenas os totais da apuração
        /// </summary>
        Task AtualizarTotaisAsync(int id, int totalVotantes, int votosValidos, int votosBrancos, int votosNulos);
        
        /// <summary>
        /// Obtém resultados pendentes de auditoria
        /// </summary>
        Task<IEnumerable<ResultadoApuracao>> ObterPendentesAuditoriaAsync();
    }
}