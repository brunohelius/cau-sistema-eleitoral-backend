using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eleitoral.Domain.Entities.Apuracao;

namespace Eleitoral.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Interface do repositório para BoletimUrna
    /// </summary>
    public interface IBoletimUrnaRepository : IBaseRepository<BoletimUrna>
    {
        /// <summary>
        /// Obtém boletim de urna por número
        /// </summary>
        Task<BoletimUrna> ObterPorNumeroUrnaAsync(int numeroUrna, int resultadoApuracaoId);
        
        /// <summary>
        /// Obtém boletim de urna por código de identificação
        /// </summary>
        Task<BoletimUrna> ObterPorCodigoIdentificacaoAsync(string codigoIdentificacao);
        
        /// <summary>
        /// Obtém boletins por status
        /// </summary>
        Task<IEnumerable<BoletimUrna>> ObterPorStatusAsync(StatusBoletim status);
        
        /// <summary>
        /// Obtém boletins pendentes de processamento
        /// </summary>
        Task<IEnumerable<BoletimUrna>> ObterPendentesAsync();
        
        /// <summary>
        /// Obtém boletins processados
        /// </summary>
        Task<IEnumerable<BoletimUrna>> ObterProcessadosAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Obtém boletins por local de votação
        /// </summary>
        Task<IEnumerable<BoletimUrna>> ObterPorLocalVotacaoAsync(string localVotacao);
        
        /// <summary>
        /// Obtém boletins por zona eleitoral
        /// </summary>
        Task<IEnumerable<BoletimUrna>> ObterPorZonaAsync(string zona);
        
        /// <summary>
        /// Obtém boletins por seção
        /// </summary>
        Task<IEnumerable<BoletimUrna>> ObterPorSecaoAsync(string secao);
        
        /// <summary>
        /// Verifica se o boletim já foi processado
        /// </summary>
        Task<bool> BoletimJaProcessadoAsync(int numeroUrna, int resultadoApuracaoId);
        
        /// <summary>
        /// Obtém boletins não conferidos
        /// </summary>
        Task<IEnumerable<BoletimUrna>> ObterNaoConferidosAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Obtém boletins rejeitados
        /// </summary>
        Task<IEnumerable<BoletimUrna>> ObterRejeitadosAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Obtém total de urnas processadas
        /// </summary>
        Task<int> ObterTotalProcessadasAsync(int resultadoApuracaoId);
        
        /// <summary>
        /// Obtém votos por chapa de um boletim
        /// </summary>
        Task<IEnumerable<VotoChapa>> ObterVotosChapaAsync(int boletimUrnaId);
        
        /// <summary>
        /// Salva boletim com votos
        /// </summary>
        Task<BoletimUrna> SalvarComVotosAsync(BoletimUrna boletimUrna);
        
        /// <summary>
        /// Obtém estatísticas de boletins por apuração
        /// </summary>
        Task<(int Total, int Processados, int Pendentes, int Rejeitados)> ObterEstatisticasAsync(int resultadoApuracaoId);
    }
}