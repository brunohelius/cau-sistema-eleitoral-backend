using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de resultados eleitorais
    /// </summary>
    public interface IResultadoService
    {
        // Apuração
        Task<ResultadoApuracao> IniciarApuracaoAsync(ApuracaoDTO dto);
        
        // Recontagem
        Task<bool> SolicitarRecontagemAsync(SolicitarRecontagemDTO dto);
        Task<ResultadoRecontagem> ExecutarRecontagemAsync(ExecutarRecontagemDTO dto);
        
        // Homologação
        Task<bool> HomologarResultadoAsync(HomologarResultadoDTO dto);
        Task<bool> ImpugnarResultadoAsync(ImpugnarResultadoDTO dto);
        
        // Publicação
        Task<bool> PublicarResultadoAsync(PublicarResultadoDTO dto);
        Task<ResultadoDivulgacao> ObterResultadoPublicoAsync(int calendarioId, int? ufId = null);
        
        // Relatórios
        Task<RelatorioEstatistico> GerarRelatorioEstatisticoAsync(int resultadoId);
        Task<List<HistoricoResultado>> ObterHistoricoResultadosAsync(int? ufId = null, int quantidade = 10);
    }

    #region DTOs

    public class ApuracaoDTO
    {
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public int ResponsavelId { get; set; }
        public bool GerarBoletimAutomatico { get; set; }
    }

    public class SolicitarRecontagemDTO
    {
        public int ResultadoId { get; set; }
        public int SolicitanteId { get; set; }
        public string Motivo { get; set; }
        public List<string> DocumentosAnexos { get; set; }
    }

    public class ExecutarRecontagemDTO
    {
        public int SolicitacaoId { get; set; }
        public int ResponsavelId { get; set; }
        public string Observacoes { get; set; }
    }

    public class HomologarResultadoDTO
    {
        public int ResultadoId { get; set; }
        public int ResponsavelId { get; set; }
        public string Observacoes { get; set; }
        public bool PublicarImediatamente { get; set; }
    }

    public class ImpugnarResultadoDTO
    {
        public int ResultadoId { get; set; }
        public int ImpugnanteId { get; set; }
        public string Motivo { get; set; }
        public string Fundamentacao { get; set; }
        public List<string> DocumentosProva { get; set; }
    }

    public class PublicarResultadoDTO
    {
        public int ResultadoId { get; set; }
        public int ResponsavelId { get; set; }
        public bool EnviarNotificacaoMassa { get; set; }
        public bool GerarRelatorioCompleto { get; set; }
    }

    public class ResultadoDivulgacao
    {
        public int CalendarioId { get; set; }
        public int? UfId { get; set; }
        public DateTime DataApuracao { get; set; }
        public int TotalEleitores { get; set; }
        public int TotalVotantes { get; set; }
        public decimal PercentualParticipacao { get; set; }
        public object ChapaVencedora { get; set; }
        public List<object> ResultadosPorChapa { get; set; }
        public bool NecessitaSegundoTurno { get; set; }
    }

    public class RelatorioEstatistico
    {
        public int ResultadoId { get; set; }
        public DateTime DataGeracao { get; set; }
        public int TotalEleitores { get; set; }
        public int TotalVotantes { get; set; }
        public int TotalAbstencoes { get; set; }
        public decimal PercentualParticipacao { get; set; }
        public decimal PercentualAbstencao { get; set; }
        public int TotalVotosValidos { get; set; }
        public int TotalVotosBrancos { get; set; }
        public int TotalVotosNulos { get; set; }
        public object DistribuicaoPorHora { get; set; }
        public object DistribuicaoGeografica { get; set; }
    }

    public class HistoricoResultado
    {
        public int CalendarioId { get; set; }
        public int Ano { get; set; }
        public int? UfId { get; set; }
        public int TotalVotantes { get; set; }
        public decimal PercentualParticipacao { get; set; }
        public int? ChapaVencedoraId { get; set; }
        public bool SegundoTurno { get; set; }
    }

    #endregion
}