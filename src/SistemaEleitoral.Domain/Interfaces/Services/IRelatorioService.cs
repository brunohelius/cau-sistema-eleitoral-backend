using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de relatórios
    /// </summary>
    public interface IRelatorioService
    {
        // Relatórios Gerenciais
        Task<RelatorioDTO> GerarRelatorioConsolidadoAsync(int calendarioId, int? ufId = null);
        Task<RelatorioDTO> GerarRelatorioParticipacaoAsync(int calendarioId);
        Task<RelatorioDTO> GerarRelatorioResultadosAsync(int calendarioId, int? ufId = null);
        
        // Relatórios de Chapas
        Task<RelatorioDTO> GerarRelatorioChapaAsync(int chapaId, bool incluirMembros = true);
        Task<RelatorioDTO> GerarComparativoChaapsAsync(ComparativoChapaDTO dto);
        Task<RelatorioDTO> GerarListaChapasPDFAsync(int calendarioId);
        Task<ExcelDTO> ExportarChaapsExcelAsync(int calendarioId);
        
        // Relatórios de Votação
        Task<RelatorioDTO> GerarRelatorioVotacaoPeriodoAsync(DateTime dataInicio, DateTime dataFim);
        Task<RelatorioDTO> GerarRelatorioAbstencaoAsync(int calendarioId);
        Task<RelatorioDTO> GerarMapaVotacaoAsync(int calendarioId);
        
        // Relatórios Judiciais
        Task<RelatorioDTO> GerarRelatorioDenunciasAsync(int calendarioId);
        Task<RelatorioDTO> GerarRelatorioImpugnacoesAsync(int calendarioId);
        Task<RelatorioDTO> GerarRelatorioProcessosAsync(int calendarioId, string status = null);
        
        // Relatórios Estatísticos
        Task<DashboardDTO> GerarDashboardAsync(int calendarioId);
        Task<RelatorioDTO> GerarRelatorioAnaliticoAsync(int calendarioId);
        Task<RelatorioDTO> GerarComparativoEleicoesAsync(ComparativoEleicoesDTO dto);
        
        // Relatórios Personalizados
        Task<RelatorioDTO> GerarRelatorioPersonalizadoAsync(RelatorioPersonalizadoDTO dto);
        Task<AgendamentoRelatorio> AgendarRelatorioAsync(AgendarRelatorioDTO dto);
        
        // Histórico
        Task<PaginatedResult<HistoricoRelatorio>> ObterHistoricoAsync(int pagina, int tamanhoPagina);
        Task<RelatorioDTO> ReexecutarRelatorioAsync(int historicoId);
        
        // Comissão Eleitoral
        Task<RelatorioDTO> GerarRelatorioAtividadesAsync(int comissaoId, DateTime? dataInicio, DateTime? dataFim);
        Task<ExportacaoDTO> ExportarMembrosAsync(int comissaoId, string formato);
    }

    #region DTOs

    public class RelatorioDTO
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public byte[] ConteudoPDF { get; set; }
        public DateTime DataGeracao { get; set; }
        public int TotalPaginas { get; set; }
        public string HashDocumento { get; set; }
    }

    public class ExcelDTO
    {
        public byte[] Conteudo { get; set; }
        public string NomeArquivo { get; set; }
        public int TotalLinhas { get; set; }
    }

    public class ExportacaoDTO
    {
        public byte[] Conteudo { get; set; }
        public string TipoArquivo { get; set; }
        public string NomeArquivo { get; set; }
    }

    public class DashboardDTO
    {
        public Dictionary<string, object> Metricas { get; set; }
        public List<GraficoDTO> Graficos { get; set; }
        public List<TabelaDTO> Tabelas { get; set; }
        public DateTime UltimaAtualizacao { get; set; }
    }

    public class GraficoDTO
    {
        public string Tipo { get; set; }
        public string Titulo { get; set; }
        public object Dados { get; set; }
        public object Opcoes { get; set; }
    }

    public class TabelaDTO
    {
        public string Titulo { get; set; }
        public List<string> Colunas { get; set; }
        public List<object[]> Linhas { get; set; }
        public Dictionary<string, object> Totalizadores { get; set; }
    }

    public class ComparativoChapaDTO
    {
        public List<int> ChapaIds { get; set; }
        public int CalendarioId { get; set; }
        public bool IncluirMembros { get; set; }
        public bool IncluirPropostas { get; set; }
        public bool IncluirHistorico { get; set; }
    }

    public class ComparativoEleicoesDTO
    {
        public List<int> CalendarioIds { get; set; }
        public int? UfId { get; set; }
        public List<string> MetricasComparar { get; set; }
        public bool IncluirGraficos { get; set; }
    }

    public class RelatorioPersonalizadoDTO
    {
        public string TipoRelatorio { get; set; }
        public int CalendarioId { get; set; }
        public Dictionary<string, object> Parametros { get; set; }
        public List<string> SecõesIncluir { get; set; }
        public string FormatoSaida { get; set; }
        public int SolicitadoPorId { get; set; }
        public bool AgendarEnvio { get; set; }
        public string EmailDestino { get; set; }
    }

    public class AgendarRelatorioDTO
    {
        public string TipoRelatorio { get; set; }
        public Dictionary<string, object> Parametros { get; set; }
        public bool Recorrente { get; set; }
        public string CronExpression { get; set; }
        public DateTime? DataExecucao { get; set; }
        public List<string> EmailsDestino { get; set; }
        public int AgendadoPorId { get; set; }
    }

    public class AgendamentoRelatorio
    {
        public int Id { get; set; }
        public string TipoRelatorio { get; set; }
        public bool Ativo { get; set; }
        public DateTime ProximaExecucao { get; set; }
        public int TotalExecucoes { get; set; }
    }

    public class HistoricoRelatorio
    {
        public int Id { get; set; }
        public string TipoRelatorio { get; set; }
        public DateTime DataGeracao { get; set; }
        public int SolicitadoPorId { get; set; }
        public string NomeArquivo { get; set; }
        public long TamanhoBytes { get; set; }
        public string Status { get; set; }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    #endregion
}