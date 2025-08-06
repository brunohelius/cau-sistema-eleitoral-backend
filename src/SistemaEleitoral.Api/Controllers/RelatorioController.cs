using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Domain.Enums;
using Hangfire;

namespace SistemaEleitoral.Api.Controllers
{
    /// <summary>
    /// Controller responsável pela geração de relatórios do sistema eleitoral
    /// </summary>
    [ApiController]
    [Route("api/relatorios")]
    [Authorize]
    public class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _relatorioService;
        private readonly ILogger<RelatorioController> _logger;

        public RelatorioController(
            IRelatorioService relatorioService,
            ILogger<RelatorioController> logger)
        {
            _relatorioService = relatorioService;
            _logger = logger;
        }

        #region Relatórios Gerenciais

        /// <summary>
        /// Gera relatório consolidado da eleição
        /// </summary>
        [HttpGet("eleicao/consolidado/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin,GestorEleitoral")]
        public async Task<IActionResult> GerarRelatorioConsolidado(int calendarioId, [FromQuery] int? ufId = null)
        {
            try
            {
                var relatorio = await _relatorioService.GerarRelatorioConsolidadoAsync(calendarioId, ufId);
                
                _logger.LogInformation($"Relatório consolidado gerado para calendário {calendarioId}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_consolidado_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório consolidado para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gera relatório de participação eleitoral
        /// </summary>
        [HttpGet("participacao/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarRelatorioParticipacao(int calendarioId)
        {
            try
            {
                var relatorio = await _relatorioService.GerarRelatorioParticipacaoAsync(calendarioId);
                
                _logger.LogInformation($"Relatório de participação gerado para calendário {calendarioId}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_participacao_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório de participação para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gera relatório de resultados da eleição
        /// </summary>
        [HttpGet("resultados/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarRelatorioResultados(int calendarioId, [FromQuery] int? ufId = null)
        {
            try
            {
                var relatorio = await _relatorioService.GerarRelatorioResultadosAsync(calendarioId, ufId);
                
                _logger.LogInformation($"Relatório de resultados gerado para calendário {calendarioId}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_resultados_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório de resultados para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Relatórios de Chapas

        /// <summary>
        /// Gera relatório detalhado de chapa
        /// </summary>
        [HttpGet("chapa/{chapaId}")]
        [Authorize]
        public async Task<IActionResult> GerarRelatorioChapa(int chapaId, [FromQuery] bool incluirMembros = true)
        {
            try
            {
                var relatorio = await _relatorioService.GerarRelatorioChapaAsync(chapaId, incluirMembros);
                
                _logger.LogInformation($"Relatório de chapa gerado para chapa {chapaId}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_chapa_{chapaId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório da chapa {chapaId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gera comparativo entre chapas
        /// </summary>
        [HttpPost("chapas/comparativo")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarComparativoChapas([FromBody] ComparativoChapaDTO dto)
        {
            try
            {
                var relatorio = await _relatorioService.GerarComparativoChaapsAsync(dto);
                
                _logger.LogInformation($"Comparativo de chapas gerado");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"comparativo_chapas_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar comparativo de chapas");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista todas as chapas de um calendário
        /// </summary>
        [HttpGet("chapas/lista/{calendarioId}")]
        [Authorize]
        public async Task<IActionResult> GerarListaChapas(int calendarioId, [FromQuery] string formato = "pdf")
        {
            try
            {
                if (formato.ToLower() == "excel")
                {
                    var excel = await _relatorioService.ExportarChaapsExcelAsync(calendarioId);
                    
                    return File(
                        excel.Conteudo, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"lista_chapas_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
                else
                {
                    var pdf = await _relatorioService.GerarListaChapasPDFAsync(calendarioId);
                    
                    return File(
                        pdf.ConteudoPDF, 
                        "application/pdf", 
                        $"lista_chapas_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar lista de chapas para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Relatórios de Votação

        /// <summary>
        /// Gera relatório de votação por período
        /// </summary>
        [HttpGet("votacao/periodo")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarRelatorioVotacaoPeriodo([FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
        {
            try
            {
                var relatorio = await _relatorioService.GerarRelatorioVotacaoPeriodoAsync(dataInicio, dataFim);
                
                _logger.LogInformation($"Relatório de votação gerado para período {dataInicio:dd/MM/yyyy} a {dataFim:dd/MM/yyyy}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_votacao_{dataInicio:yyyyMMdd}_{dataFim:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório de votação por período");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gera relatório de abstenção
        /// </summary>
        [HttpGet("votacao/abstencao/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarRelatorioAbstencao(int calendarioId)
        {
            try
            {
                var relatorio = await _relatorioService.GerarRelatorioAbstencaoAsync(calendarioId);
                
                _logger.LogInformation($"Relatório de abstenção gerado para calendário {calendarioId}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_abstencao_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório de abstenção para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gera mapa de votação por região
        /// </summary>
        [HttpGet("votacao/mapa/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarMapaVotacao(int calendarioId)
        {
            try
            {
                var relatorio = await _relatorioService.GerarMapaVotacaoAsync(calendarioId);
                
                _logger.LogInformation($"Mapa de votação gerado para calendário {calendarioId}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"mapa_votacao_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar mapa de votação para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Relatórios Judiciais

        /// <summary>
        /// Gera relatório de denúncias
        /// </summary>
        [HttpGet("denuncias/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarRelatorioDenuncias(int calendarioId)
        {
            try
            {
                var relatorio = await _relatorioService.GerarRelatorioDenunciasAsync(calendarioId);
                
                _logger.LogInformation($"Relatório de denúncias gerado para calendário {calendarioId}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_denuncias_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório de denúncias para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gera relatório de impugnações
        /// </summary>
        [HttpGet("impugnacoes/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarRelatorioImpugnacoes(int calendarioId)
        {
            try
            {
                var relatorio = await _relatorioService.GerarRelatorioImpugnacoesAsync(calendarioId);
                
                _logger.LogInformation($"Relatório de impugnações gerado para calendário {calendarioId}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_impugnacoes_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório de impugnações para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gera relatório de processos judiciais
        /// </summary>
        [HttpGet("processos/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarRelatorioProcessos(int calendarioId, [FromQuery] string status = null)
        {
            try
            {
                var relatorio = await _relatorioService.GerarRelatorioProcessosAsync(calendarioId, status);
                
                _logger.LogInformation($"Relatório de processos gerado para calendário {calendarioId}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_processos_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório de processos para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Relatórios Estatísticos

        /// <summary>
        /// Gera dashboard estatístico
        /// </summary>
        [HttpGet("dashboard/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarDashboard(int calendarioId)
        {
            try
            {
                var dashboard = await _relatorioService.GerarDashboardAsync(calendarioId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = dashboard,
                    geradoEm = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar dashboard para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gera relatório analítico
        /// </summary>
        [HttpGet("analitico/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarRelatorioAnalitico(int calendarioId)
        {
            try
            {
                var relatorio = await _relatorioService.GerarRelatorioAnaliticoAsync(calendarioId);
                
                _logger.LogInformation($"Relatório analítico gerado para calendário {calendarioId}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_analitico_{calendarioId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório analítico para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gera relatório comparativo entre eleições
        /// </summary>
        [HttpPost("comparativo/eleicoes")]
        [Authorize(Roles = "Admin,GestorEleitoral")]
        public async Task<IActionResult> GerarComparativoEleicoes([FromBody] ComparativoEleicoesDTO dto)
        {
            try
            {
                var relatorio = await _relatorioService.GerarComparativoEleicoesAsync(dto);
                
                _logger.LogInformation($"Comparativo de eleições gerado");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"comparativo_eleicoes_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar comparativo de eleições");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Relatórios Personalizados

        /// <summary>
        /// Gera relatório personalizado
        /// </summary>
        [HttpPost("personalizado")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarRelatorioPersonalizado([FromBody] RelatorioPersonalizadoDTO dto)
        {
            try
            {
                dto.SolicitadoPorId = GetUsuarioLogadoId();

                var relatorio = await _relatorioService.GerarRelatorioPersonalizadoAsync(dto);
                
                _logger.LogInformation($"Relatório personalizado gerado: {dto.TipoRelatorio}");

                if (dto.AgendarEnvio)
                {
                    // Agendar envio por email
                    BackgroundJob.Enqueue(() => EnviarRelatorioPorEmail(relatorio.Id, dto.EmailDestino));
                }

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_{dto.TipoRelatorio}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório personalizado");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Agenda geração de relatório
        /// </summary>
        [HttpPost("agendar")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> AgendarRelatorio([FromBody] AgendarRelatorioDTO dto)
        {
            try
            {
                dto.AgendadoPorId = GetUsuarioLogadoId();

                var agendamento = await _relatorioService.AgendarRelatorioAsync(dto);

                // Agendar job
                if (dto.Recorrente)
                {
                    RecurringJob.AddOrUpdate(
                        $"relatorio_{agendamento.Id}",
                        () => GerarRelatorioAgendado(agendamento.Id),
                        dto.CronExpression);
                }
                else
                {
                    BackgroundJob.Schedule(
                        () => GerarRelatorioAgendado(agendamento.Id),
                        dto.DataExecucao.Value);
                }

                _logger.LogInformation($"Relatório agendado: {agendamento.Id}");

                return Ok(new 
                { 
                    success = true, 
                    data = agendamento,
                    message = "Relatório agendado com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao agendar relatório");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Histórico e Logs

        /// <summary>
        /// Lista histórico de relatórios gerados
        /// </summary>
        [HttpGet("historico")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ObterHistorico([FromQuery] int? pagina = 1, [FromQuery] int? tamanhoPagina = 20)
        {
            try
            {
                var historico = await _relatorioService.ObterHistoricoAsync(pagina.Value, tamanhoPagina.Value);
                
                return Ok(new 
                { 
                    success = true, 
                    data = historico
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter histórico de relatórios");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Reexecuta relatório do histórico
        /// </summary>
        [HttpPost("historico/{historicoId}/reexecutar")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ReexecutarRelatorio(int historicoId)
        {
            try
            {
                var relatorio = await _relatorioService.ReexecutarRelatorioAsync(historicoId);
                
                _logger.LogInformation($"Relatório {historicoId} reexecutado");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_reexecutado_{historicoId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao reexecutar relatório {historicoId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Métodos Auxiliares

        private int GetUsuarioLogadoId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return 0;
        }

        // Jobs em background
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task EnviarRelatorioPorEmail(int relatorioId, string emailDestino)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task GerarRelatorioAgendado(int agendamentoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        #endregion
    }
}