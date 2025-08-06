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
    /// Controller responsável pelo sistema de impugnações
    /// </summary>
    [ApiController]
    [Route("api/impugnacoes")]
    [Authorize]
    public class ImpugnacaoController : ControllerBase
    {
        private readonly IImpugnacaoService _impugnacaoService;
        private readonly ILogger<ImpugnacaoController> _logger;

        public ImpugnacaoController(
            IImpugnacaoService impugnacaoService,
            ILogger<ImpugnacaoController> logger)
        {
            _impugnacaoService = impugnacaoService;
            _logger = logger;
        }

        #region Registro de Impugnações

        /// <summary>
        /// Registra um pedido de impugnação
        /// </summary>
        [HttpPost("registrar")]
        [Authorize]
        public async Task<IActionResult> RegistrarImpugnacao([FromBody] RegistrarImpugnacaoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.SolicitanteId = GetProfissionalLogadoId();
                dto.UsuarioRegistroId = GetUsuarioLogadoId();

                var pedido = await _impugnacaoService.RegistrarPedidoImpugnacaoAsync(dto);

                _logger.LogInformation($"Pedido de impugnação {pedido.Protocolo} registrado");

                // Agendar emails de notificação
                BackgroundJob.Enqueue(() => NotificarNovaImpugnacao(pedido.Id));

                return CreatedAtAction(
                    nameof(ObterPorId),
                    new { id = pedido.Id },
                    new 
                    { 
                        success = true, 
                        data = pedido,
                        protocolo = pedido.Protocolo,
                        message = "Pedido de impugnação registrado com sucesso"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar pedido de impugnação");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Defesa

        /// <summary>
        /// Registra defesa contra impugnação
        /// </summary>
        [HttpPost("{id}/defesa")]
        [Authorize]
        public async Task<IActionResult> RegistrarDefesa(int id, [FromBody] RegistrarDefesaImpugnacaoDTO dto)
        {
            try
            {
                dto.ApresentadaPorId = GetProfissionalLogadoId();
                dto.UsuarioRegistroId = GetUsuarioLogadoId();

                var sucesso = await _impugnacaoService.RegistrarDefesaImpugnacaoAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Defesa registrada para impugnação {id}");
                    
                    // Notificar solicitante
                    BackgroundJob.Enqueue(() => NotificarDefesaApresentada(id));
                    
                    return Ok(new 
                    { 
                        success = true, 
                        message = "Defesa registrada com sucesso" 
                    });
                }

                return BadRequest(new { success = false, message = "Não foi possível registrar a defesa" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar defesa para impugnação {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Julgamento

        /// <summary>
        /// Registra julgamento de impugnação
        /// </summary>
        [HttpPost("{id}/julgar")]
        [Authorize(Roles = "ComissaoEleitoral,Relator,Admin")]
        public async Task<IActionResult> JulgarImpugnacao(int id, [FromBody] JulgarImpugnacaoDTO dto)
        {
            try
            {
                dto.RelatorId = GetProfissionalLogadoId();
                dto.UsuarioJulgamentoId = GetUsuarioLogadoId();

                var sucesso = await _impugnacaoService.JulgarImpugnacaoAsync(id, dto);

                if (sucesso)
                {
                    var resultado = dto.Decisao == DecisaoJulgamento.Procedente ? "DEFERIDA" : "INDEFERIDA";
                    _logger.LogInformation($"Impugnação {id} julgada: {resultado}");
                    
                    // Notificar partes
                    BackgroundJob.Enqueue(() => NotificarJulgamentoImpugnacao(id));
                    
                    return Ok(new 
                    { 
                        success = true, 
                        message = $"Impugnação {resultado.ToLower()} com sucesso",
                        decisao = dto.Decisao.ToString()
                    });
                }

                return BadRequest(new { success = false, message = "Não foi possível julgar a impugnação" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao julgar impugnação {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Recursos

        /// <summary>
        /// Registra recurso contra decisão
        /// </summary>
        [HttpPost("{id}/recurso")]
        [Authorize]
        public async Task<IActionResult> RegistrarRecurso(int id, [FromBody] RegistrarRecursoImpugnacaoDTO dto)
        {
            try
            {
                dto.RecorrenteId = GetProfissionalLogadoId();
                dto.UsuarioRegistroId = GetUsuarioLogadoId();

                var sucesso = await _impugnacaoService.RegistrarRecursoImpugnacaoAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Recurso registrado para impugnação {id}");
                    
                    // Notificar partes
                    BackgroundJob.Enqueue(() => NotificarRecursoApresentado(id));
                    
                    return Ok(new 
                    { 
                        success = true, 
                        message = "Recurso registrado com sucesso" 
                    });
                }

                return BadRequest(new { success = false, message = "Não foi possível registrar o recurso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar recurso para impugnação {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Julga recurso de impugnação
        /// </summary>
        [HttpPost("recurso/{recursoId}/julgar")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> JulgarRecurso(int recursoId, [FromBody] JulgarRecursoDTO dto)
        {
            try
            {
                dto.RelatorId = GetProfissionalLogadoId();
                dto.UsuarioJulgamentoId = GetUsuarioLogadoId();

                var sucesso = await _impugnacaoService.JulgarRecursoImpugnacaoAsync(recursoId, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Recurso {recursoId} julgado");
                    
                    return Ok(new 
                    { 
                        success = true, 
                        message = "Recurso julgado com sucesso",
                        decisao = dto.Decisao.ToString()
                    });
                }

                return BadRequest(new { success = false, message = "Não foi possível julgar o recurso" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao julgar recurso {recursoId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Consultas

        /// <summary>
        /// Obtém pedido de impugnação por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                var pedido = await _impugnacaoService.ObterPedidoPorIdAsync(id);
                
                if (pedido == null)
                    return NotFound(new { success = false, message = "Pedido de impugnação não encontrado" });

                return Ok(new { success = true, data = pedido });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter pedido de impugnação {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém pedido por protocolo
        /// </summary>
        [HttpGet("protocolo/{protocolo}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPorProtocolo(string protocolo)
        {
            try
            {
                var pedido = await _impugnacaoService.ObterPedidoPorProtocoloAsync(protocolo);
                
                if (pedido == null)
                    return NotFound(new { success = false, message = "Pedido não encontrado" });

                // Se não autenticado, retornar versão resumida
                if (!User.Identity.IsAuthenticated)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            Protocolo = pedido.Protocolo,
                            Status = pedido.Status,
                            DataSolicitacao = pedido.DataSolicitacao,
                            FoiJulgado = pedido.FoiJulgado,
                            Decisao = pedido.Decisao
                        }
                    });
                }

                return Ok(new { success = true, data = pedido });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter pedido por protocolo {protocolo}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista pedidos por chapa
        /// </summary>
        [HttpGet("chapa/{chapaId}")]
        [Authorize]
        public async Task<IActionResult> ObterPorChapa(int chapaId)
        {
            try
            {
                var pedidos = await _impugnacaoService.ObterPedidosPorChapaAsync(chapaId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = pedidos,
                    total = pedidos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter pedidos da chapa {chapaId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista pedidos por calendário
        /// </summary>
        [HttpGet("calendario/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ObterPorCalendario(int calendarioId)
        {
            try
            {
                var pedidos = await _impugnacaoService.ObterPedidosPorCalendarioAsync(calendarioId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = pedidos,
                    total = pedidos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter pedidos do calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista pedidos do solicitante
        /// </summary>
        [HttpGet("meus-pedidos")]
        [Authorize]
        public async Task<IActionResult> ObterMeusPedidos()
        {
            try
            {
                var solicitanteId = GetProfissionalLogadoId();
                var pedidos = await _impugnacaoService.ObterPedidosPorSolicitanteAsync(solicitanteId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = pedidos,
                    total = pedidos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter pedidos do solicitante");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Estatísticas

        /// <summary>
        /// Obtém quantidade de pedidos por UF
        /// </summary>
        [HttpGet("estatisticas/por-uf/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ObterQuantidadePorUf(int calendarioId)
        {
            try
            {
                var resultado = await _impugnacaoService.ObterQuantidadePorUfAsync(calendarioId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = resultado 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter quantidade por UF para calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém estatísticas gerais
        /// </summary>
        [HttpGet("estatisticas/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ObterEstatisticas(int calendarioId)
        {
            try
            {
                var estatisticas = await _impugnacaoService.ObterEstatisticasAsync(calendarioId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = estatisticas 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter estatísticas do calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Documentos

        /// <summary>
        /// Anexa documento ao pedido
        /// </summary>
        [HttpPost("{id}/documentos")]
        [Authorize]
        public async Task<IActionResult> AnexarDocumento(int id, [FromForm] AnexarDocumentoFormDTO dto)
        {
            try
            {
                // Converter arquivo para bytes
                byte[] conteudo = null;
                if (dto.Arquivo != null && dto.Arquivo.Length > 0)
                {
                    using (var ms = new System.IO.MemoryStream())
                    {
                        await dto.Arquivo.CopyToAsync(ms);
                        conteudo = ms.ToArray();
                    }
                }

                var anexarDto = new AnexarDocumentoImpugnacaoDTO
                {
                    TipoDocumento = dto.TipoDocumento,
                    NomeArquivo = dto.Arquivo?.FileName,
                    ConteudoArquivo = conteudo,
                    UsuarioUploadId = GetUsuarioLogadoId()
                };

                var sucesso = await _impugnacaoService.AnexarDocumentoAsync(id, anexarDto);

                if (sucesso)
                {
                    _logger.LogInformation($"Documento anexado ao pedido {id}");
                    return Ok(new { success = true, message = "Documento anexado com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível anexar o documento" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao anexar documento ao pedido {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Remove documento do pedido
        /// </summary>
        [HttpDelete("{pedidoId}/documentos/{documentoId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> RemoverDocumento(int pedidoId, int documentoId)
        {
            try
            {
                var sucesso = await _impugnacaoService.RemoverDocumentoAsync(pedidoId, documentoId);

                if (sucesso)
                {
                    _logger.LogInformation($"Documento {documentoId} removido do pedido {pedidoId}");
                    return Ok(new { success = true, message = "Documento removido com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível remover o documento" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao remover documento {documentoId} do pedido {pedidoId}");
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
        public async Task NotificarNovaImpugnacao(int pedidoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarDefesaApresentada(int pedidoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarJulgamentoImpugnacao(int pedidoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarRecursoApresentado(int pedidoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        #endregion
    }

    /// <summary>
    /// DTO para upload de documento
    /// </summary>
    public class AnexarDocumentoFormDTO
    {
        public string TipoDocumento { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile Arquivo { get; set; }
    }
}