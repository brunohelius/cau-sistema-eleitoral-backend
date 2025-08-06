using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Eleitoral.Application.Interfaces;
using Eleitoral.Application.DTOs.Apuracao;

namespace Eleitoral.API.Controllers
{
    /// <summary>
    /// Controller para gerenciar a apuração de eleições
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ApuracaoController : ControllerBase
    {
        private readonly IApuracaoService _apuracaoService;
        private readonly ILogger<ApuracaoController> _logger;
        
        public ApuracaoController(
            IApuracaoService apuracaoService,
            ILogger<ApuracaoController> logger)
        {
            _apuracaoService = apuracaoService;
            _logger = logger;
        }
        
        /// <summary>
        /// Inicia a apuração de uma eleição
        /// </summary>
        /// <param name="eleicaoId">ID da eleição</param>
        /// <returns>Resultado da apuração iniciada</returns>
        [HttpPost("eleicao/{eleicaoId}/iniciar")]
        [Authorize(Roles = "Admin,ComissaoEleitoral")]
        public async Task<ActionResult<ResultadoApuracaoDto>> IniciarApuracao(int eleicaoId)
        {
            try
            {
                _logger.LogInformation($"Iniciando apuração da eleição {eleicaoId}");
                
                var resultado = await _apuracaoService.IniciarApuracaoAsync(eleicaoId);
                
                return Ok(new
                {
                    success = true,
                    message = "Apuração iniciada com sucesso",
                    data = resultado
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Tentativa de iniciar apuração já existente para eleição {eleicaoId}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, $"Erro de argumento ao iniciar apuração da eleição {eleicaoId}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao iniciar apuração da eleição {eleicaoId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao iniciar apuração"
                });
            }
        }
        
        /// <summary>
        /// Processa um boletim de urna
        /// </summary>
        /// <param name="dto">Dados do boletim de urna</param>
        /// <returns>Resultado atualizado da apuração</returns>
        [HttpPost("boletim/processar")]
        [Authorize(Roles = "Admin,ComissaoEleitoral,Mesario")]
        public async Task<ActionResult<ResultadoApuracaoDto>> ProcessarBoletimUrna([FromBody] ProcessarBoletimDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                
                _logger.LogInformation($"Processando boletim da urna {dto.NumeroUrna}");
                
                var resultado = await _apuracaoService.ProcessarBoletimUrnaAsync(dto);
                
                return Ok(new
                {
                    success = true,
                    message = $"Boletim da urna {dto.NumeroUrna} processado com sucesso",
                    data = resultado
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Erro ao processar boletim da urna {dto.NumeroUrna}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar boletim da urna {dto.NumeroUrna}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao processar boletim"
                });
            }
        }
        
        /// <summary>
        /// Obtém o resultado da apuração em tempo real
        /// </summary>
        /// <param name="eleicaoId">ID da eleição</param>
        /// <returns>Resultado atual da apuração</returns>
        [HttpGet("eleicao/{eleicaoId}/tempo-real")]
        [AllowAnonymous] // Permitir acesso público aos resultados
        public async Task<ActionResult<ResultadoApuracaoDto>> ObterResultadoTempoReal(int eleicaoId)
        {
            try
            {
                var resultado = await _apuracaoService.ObterResultadoTempoRealAsync(eleicaoId);
                
                if (resultado == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Nenhuma apuração encontrada para esta eleição"
                    });
                }
                
                return Ok(new
                {
                    success = true,
                    data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter resultado em tempo real da eleição {eleicaoId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao obter resultado"
                });
            }
        }
        
        /// <summary>
        /// Finaliza a apuração
        /// </summary>
        /// <param name="resultadoApuracaoId">ID do resultado da apuração</param>
        /// <returns>Resultado final da apuração</returns>
        [HttpPost("{resultadoApuracaoId}/finalizar")]
        [Authorize(Roles = "Admin,ComissaoEleitoral")]
        public async Task<ActionResult<ResultadoApuracaoDto>> FinalizarApuracao(int resultadoApuracaoId)
        {
            try
            {
                _logger.LogInformation($"Finalizando apuração {resultadoApuracaoId}");
                
                var resultado = await _apuracaoService.FinalizarApuracaoAsync(resultadoApuracaoId);
                
                return Ok(new
                {
                    success = true,
                    message = "Apuração finalizada com sucesso",
                    data = resultado
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Erro ao finalizar apuração {resultadoApuracaoId}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao finalizar apuração {resultadoApuracaoId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao finalizar apuração"
                });
            }
        }
        
        /// <summary>
        /// Audita o resultado da apuração
        /// </summary>
        /// <param name="resultadoApuracaoId">ID do resultado da apuração</param>
        /// <returns>Resultado auditado</returns>
        [HttpPost("{resultadoApuracaoId}/auditar")]
        [Authorize(Roles = "Admin,Auditor")]
        public async Task<ActionResult<ResultadoApuracaoDto>> AuditarApuracao(int resultadoApuracaoId)
        {
            try
            {
                var auditorId = User.Identity.Name;
                _logger.LogInformation($"Auditando apuração {resultadoApuracaoId} por {auditorId}");
                
                var resultado = await _apuracaoService.AuditarApuracaoAsync(resultadoApuracaoId, auditorId);
                
                return Ok(new
                {
                    success = true,
                    message = "Apuração auditada com sucesso",
                    data = resultado
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Erro ao auditar apuração {resultadoApuracaoId}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao auditar apuração {resultadoApuracaoId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao auditar apuração"
                });
            }
        }
        
        /// <summary>
        /// Reabre a apuração para correções
        /// </summary>
        /// <param name="resultadoApuracaoId">ID do resultado da apuração</param>
        /// <param name="request">Dados da reabertura</param>
        /// <returns>Resultado reaberto</returns>
        [HttpPost("{resultadoApuracaoId}/reabrir")]
        [Authorize(Roles = "Admin,ComissaoEleitoral")]
        public async Task<ActionResult<ResultadoApuracaoDto>> ReabrirApuracao(
            int resultadoApuracaoId,
            [FromBody] ReabrirApuracaoRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Motivo))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Motivo da reabertura é obrigatório"
                    });
                }
                
                _logger.LogInformation($"Reabrindo apuração {resultadoApuracaoId}. Motivo: {request.Motivo}");
                
                var resultado = await _apuracaoService.ReabrirApuracaoAsync(resultadoApuracaoId, request.Motivo);
                
                return Ok(new
                {
                    success = true,
                    message = "Apuração reaberta com sucesso",
                    data = resultado
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Erro ao reabrir apuração {resultadoApuracaoId}");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao reabrir apuração {resultadoApuracaoId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao reabrir apuração"
                });
            }
        }
        
        /// <summary>
        /// Obtém as estatísticas da apuração
        /// </summary>
        /// <param name="resultadoApuracaoId">ID do resultado da apuração</param>
        /// <returns>Estatísticas detalhadas</returns>
        [HttpGet("{resultadoApuracaoId}/estatisticas")]
        [AllowAnonymous]
        public async Task<ActionResult<EstatisticasApuracaoDto>> ObterEstatisticas(int resultadoApuracaoId)
        {
            try
            {
                var estatisticas = await _apuracaoService.ObterEstatisticasAsync(resultadoApuracaoId);
                
                if (estatisticas == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Estatísticas não encontradas"
                    });
                }
                
                return Ok(new
                {
                    success = true,
                    data = estatisticas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter estatísticas da apuração {resultadoApuracaoId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao obter estatísticas"
                });
            }
        }
        
        /// <summary>
        /// Obtém os boletins de urna pendentes
        /// </summary>
        /// <returns>Lista de boletins pendentes</returns>
        [HttpGet("boletins/pendentes")]
        [Authorize(Roles = "Admin,ComissaoEleitoral")]
        public async Task<ActionResult<IEnumerable<BoletimUrnaDto>>> ObterBoletinsPendentes()
        {
            try
            {
                var boletins = await _apuracaoService.ObterBoletinsPendentesAsync();
                
                return Ok(new
                {
                    success = true,
                    data = boletins
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter boletins pendentes");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao obter boletins pendentes"
                });
            }
        }
        
        /// <summary>
        /// Obtém os logs da apuração
        /// </summary>
        /// <param name="resultadoApuracaoId">ID do resultado da apuração</param>
        /// <returns>Lista de logs</returns>
        [HttpGet("{resultadoApuracaoId}/logs")]
        [Authorize(Roles = "Admin,Auditor")]
        public async Task<ActionResult<IEnumerable<LogApuracaoDto>>> ObterLogsApuracao(int resultadoApuracaoId)
        {
            try
            {
                var logs = await _apuracaoService.ObterLogsApuracaoAsync(resultadoApuracaoId);
                
                return Ok(new
                {
                    success = true,
                    data = logs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter logs da apuração {resultadoApuracaoId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao obter logs"
                });
            }
        }
        
        /// <summary>
        /// Valida a integridade da apuração
        /// </summary>
        /// <param name="resultadoApuracaoId">ID do resultado da apuração</param>
        /// <returns>Resultado da validação</returns>
        [HttpGet("{resultadoApuracaoId}/validar-integridade")]
        [Authorize(Roles = "Admin,Auditor")]
        public async Task<ActionResult<bool>> ValidarIntegridadeApuracao(int resultadoApuracaoId)
        {
            try
            {
                var valido = await _apuracaoService.ValidarIntegridadeApuracaoAsync(resultadoApuracaoId);
                
                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        integridadeValida = valido,
                        mensagem = valido ? "Integridade validada com sucesso" : "Foram encontradas inconsistências na apuração"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao validar integridade da apuração {resultadoApuracaoId}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Erro interno ao validar integridade"
                });
            }
        }
    }
    
    /// <summary>
    /// Request para reabrir apuração
    /// </summary>
    public class ReabrirApuracaoRequest
    {
        public string Motivo { get; set; }
    }
}