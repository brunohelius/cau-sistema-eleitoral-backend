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
    /// Controller responsável pelo sistema de votação eletrônica
    /// </summary>
    [ApiController]
    [Route("api/votacao")]
    [Authorize]
    public class VotacaoController : ControllerBase
    {
        private readonly IVotacaoService _votacaoService;
        private readonly ILogger<VotacaoController> _logger;

        public VotacaoController(
            IVotacaoService votacaoService,
            ILogger<VotacaoController> logger)
        {
            _votacaoService = votacaoService;
            _logger = logger;
        }

        #region Gerenciamento de Sessão

        /// <summary>
        /// Abre sessão de votação
        /// </summary>
        [HttpPost("sessao/abrir")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> AbrirVotacao([FromBody] AbrirVotacaoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.UsuarioAberturaId = GetUsuarioLogadoId();

                var sucesso = await _votacaoService.AbrirVotacaoAsync(dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Votação aberta para calendário {dto.CalendarioId}, UF {dto.UfSigla}");
                    
                    // Agendar notificações
                    BackgroundJob.Enqueue(() => NotificarAberturaVotacao(dto.CalendarioId, dto.UfId));
                    
                    return Ok(new 
                    { 
                        success = true, 
                        message = "Votação aberta com sucesso",
                        data = new { calendarioId = dto.CalendarioId, ufId = dto.UfId }
                    });
                }

                return BadRequest(new { success = false, message = "Não foi possível abrir a votação" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao abrir votação");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Fecha sessão de votação
        /// </summary>
        [HttpPost("sessao/fechar")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> FecharVotacao([FromBody] FecharVotacaoDTO dto)
        {
            try
            {
                dto.UsuarioFechamentoId = GetUsuarioLogadoId();

                var sucesso = await _votacaoService.FecharVotacaoAsync(dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Votação fechada para calendário {dto.CalendarioId}, UF {dto.UfId}");
                    
                    if (dto.IniciarApuracao)
                    {
                        // Agendar apuração automática
                        BackgroundJob.Enqueue(() => IniciarApuracaoAutomatica(dto.CalendarioId, dto.UfId));
                    }
                    
                    return Ok(new 
                    { 
                        success = true, 
                        message = "Votação fechada com sucesso",
                        iniciarApuracao = dto.IniciarApuracao
                    });
                }

                return BadRequest(new { success = false, message = "Não foi possível fechar a votação" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fechar votação");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém status da votação
        /// </summary>
        [HttpGet("sessao/status/{calendarioId}/{ufId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterStatusVotacao(int calendarioId, int ufId)
        {
            try
            {
                var status = await _votacaoService.ObterStatusVotacaoAsync(calendarioId, ufId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = status 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter status da votação para calendário {calendarioId}, UF {ufId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Registro de Voto

        /// <summary>
        /// Registra voto do eleitor
        /// </summary>
        [HttpPost("votar")]
        [Authorize]
        public async Task<IActionResult> RegistrarVoto([FromBody] RegistrarVotoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Pegar eleitor logado
                dto.EleitorId = GetProfissionalLogadoId();
                
                // Adicionar informações de auditoria
                dto.IpOrigem = HttpContext.Connection.RemoteIpAddress?.ToString();
                dto.UserAgent = Request.Headers["User-Agent"].ToString();

                var comprovante = await _votacaoService.RegistrarVotoAsync(dto);

                _logger.LogInformation($"Voto registrado com protocolo {comprovante.ProtocoloComprovante}");

                // Agendar envio de comprovante por email
                BackgroundJob.Enqueue(() => EnviarComprovanteVoto(dto.EleitorId, comprovante.ProtocoloComprovante));

                return Ok(new 
                { 
                    success = true, 
                    message = "Voto registrado com sucesso",
                    data = comprovante
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar voto");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Verifica se eleitor já votou
        /// </summary>
        [HttpGet("verificar-voto/{sessaoVotacaoId}")]
        [Authorize]
        public async Task<IActionResult> VerificarSeVotou(int sessaoVotacaoId)
        {
            try
            {
                var eleitorId = GetProfissionalLogadoId();
                var jaVotou = await _votacaoService.VerificarSeEleitorVotouAsync(eleitorId, sessaoVotacaoId);

                return Ok(new 
                { 
                    success = true,
                    jaVotou = jaVotou,
                    message = jaVotou ? "Você já votou nesta eleição" : "Você ainda não votou"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar voto do eleitor");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Verifica comprovante de voto
        /// </summary>
        [HttpGet("comprovante/verificar/{protocolo}/{codigoVerificacao}")]
        [AllowAnonymous]
        public async Task<IActionResult> VerificarComprovante(string protocolo, string codigoVerificacao)
        {
            try
            {
                var valido = await _votacaoService.VerificarComprovanteAsync(protocolo, codigoVerificacao);

                return Ok(new 
                { 
                    success = true,
                    valido = valido,
                    message = valido ? 
                        "Comprovante válido - Voto registrado com sucesso" : 
                        "Comprovante inválido ou não encontrado"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar comprovante {protocolo}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Apuração

        /// <summary>
        /// Inicia apuração de votos
        /// </summary>
        [HttpPost("apuracao/iniciar/{sessaoVotacaoId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> IniciarApuracao(int sessaoVotacaoId)
        {
            try
            {
                var resultado = await _votacaoService.IniciarApuracaoAsync(sessaoVotacaoId);

                _logger.LogInformation($"Apuração iniciada para sessão {sessaoVotacaoId}");

                // Agendar notificação de resultado
                BackgroundJob.Enqueue(() => NotificarResultadoApuracao(resultado.Id));

                return Ok(new 
                { 
                    success = true, 
                    message = "Apuração concluída com sucesso",
                    data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao iniciar apuração para sessão {sessaoVotacaoId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Gera boletim de urna
        /// </summary>
        [HttpGet("apuracao/boletim/{resultadoApuracaoId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> GerarBoletimUrna(int resultadoApuracaoId)
        {
            try
            {
                var boletim = await _votacaoService.GerarBoletimUrnaAsync(resultadoApuracaoId);

                _logger.LogInformation($"Boletim de urna gerado para resultado {resultadoApuracaoId}");

                return File(
                    boletim.ConteudoPDF, 
                    "application/pdf", 
                    $"boletim_urna_{resultadoApuracaoId}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar boletim de urna para resultado {resultadoApuracaoId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Estatísticas

        /// <summary>
        /// Obtém estatísticas da votação
        /// </summary>
        [HttpGet("estatisticas/{sessaoVotacaoId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ObterEstatisticas(int sessaoVotacaoId)
        {
            try
            {
                var estatisticas = await _votacaoService.ObterEstatisticasAsync(sessaoVotacaoId);

                return Ok(new 
                { 
                    success = true, 
                    data = estatisticas 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter estatísticas da sessão {sessaoVotacaoId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém participação geral da eleição
        /// </summary>
        [HttpGet("participacao/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ObterParticipacao(int calendarioId)
        {
            try
            {
                var participacao = await _votacaoService.ObterParticipacaoAsync(calendarioId);

                return Ok(new 
                { 
                    success = true, 
                    data = participacao 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter participação do calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Dashboard de votação em tempo real
        /// </summary>
        [HttpGet("dashboard/{sessaoVotacaoId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ObterDashboard(int sessaoVotacaoId)
        {
            try
            {
                var estatisticas = await _votacaoService.ObterEstatisticasAsync(sessaoVotacaoId);
                var status = await _votacaoService.ObterStatusVotacaoAsync(0, 0); // Ajustar para pegar da sessão

                var dashboard = new
                {
                    status = status,
                    estatisticas = estatisticas,
                    ultimaAtualizacao = DateTime.Now
                };

                return Ok(new 
                { 
                    success = true, 
                    data = dashboard 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter dashboard da sessão {sessaoVotacaoId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Segundo Turno

        /// <summary>
        /// Configura segundo turno
        /// </summary>
        [HttpPost("segundo-turno/configurar")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ConfigurarSegundoTurno([FromBody] ConfigurarSegundoTurnoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.UsuarioConfiguradorId = GetUsuarioLogadoId();

                var sucesso = await _votacaoService.ConfigurarSegundoTurnoAsync(dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Segundo turno configurado para calendário {dto.CalendarioId}");
                    
                    return Ok(new 
                    { 
                        success = true, 
                        message = "Segundo turno configurado com sucesso",
                        dataSegundoTurno = dto.DataSegundoTurno
                    });
                }

                return BadRequest(new { success = false, message = "Não foi possível configurar segundo turno" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao configurar segundo turno");
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

        private int GetProfissionalLogadoId()
        {
            var profissionalIdClaim = User.Claims.FirstOrDefault(c => c.Type == "ProfissionalId");
            if (profissionalIdClaim != null && int.TryParse(profissionalIdClaim.Value, out var profissionalId))
            {
                return profissionalId;
            }
            return 0;
        }

        // Jobs em background
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarAberturaVotacao(int calendarioId, int ufId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task IniciarApuracaoAutomatica(int calendarioId, int ufId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task EnviarComprovanteVoto(int eleitorId, string protocolo)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarResultadoApuracao(int resultadoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        #endregion
    }
}