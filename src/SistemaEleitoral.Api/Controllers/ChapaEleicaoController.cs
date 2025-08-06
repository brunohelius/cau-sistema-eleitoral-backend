using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Api.Controllers
{
    /// <summary>
    /// Controller responsável pela gestão de chapas eleitorais
    /// </summary>
    [ApiController]
    [Route("api/chapas")]
    [Authorize]
    public class ChapaEleicaoController : ControllerBase
    {
        private readonly IChapaEleicaoService _chapaService;
        private readonly IMembroChapaService _membroService;
        private readonly IValidacaoElegibilidadeService _validacaoService;
        private readonly ILogger<ChapaEleicaoController> _logger;

        public ChapaEleicaoController(
            IChapaEleicaoService chapaService,
            IMembroChapaService membroService,
            IValidacaoElegibilidadeService validacaoService,
            ILogger<ChapaEleicaoController> logger)
        {
            _chapaService = chapaService;
            _membroService = membroService;
            _validacaoService = validacaoService;
            _logger = logger;
        }

        #region Registro e Criação de Chapas

        /// <summary>
        /// Registra uma nova chapa eleitoral
        /// </summary>
        [HttpPost("registrar")]
        [Authorize(Roles = "Profissional")]
        public async Task<IActionResult> RegistrarChapa([FromBody] RegistrarChapaDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Adicionar responsável como usuário logado
                dto.ResponsavelId = GetProfissionalLogadoId();
                dto.UsuarioCriacaoId = GetUsuarioLogadoId();

                var chapa = await _chapaService.RegistrarChapaAsync(dto);
                
                _logger.LogInformation($"Chapa {chapa.NumeroChapa} registrada com sucesso");
                
                return CreatedAtAction(
                    nameof(ObterPorId), 
                    new { id = chapa.Id }, 
                    new { success = true, data = chapa });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar chapa");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Confirma e finaliza o registro da chapa
        /// </summary>
        [HttpPost("{id}/confirmar")]
        [Authorize(Roles = "Profissional")]
        public async Task<IActionResult> ConfirmarChapa(int id, [FromBody] ConfirmarChapaDTO dto)
        {
            try
            {
                dto.UsuarioConfirmacaoId = GetUsuarioLogadoId();
                dto.IpConfirmacao = HttpContext.Connection.RemoteIpAddress?.ToString();

                var sucesso = await _chapaService.ConfirmarChapaAsync(id, dto);
                
                if (sucesso)
                {
                    _logger.LogInformation($"Chapa {id} confirmada com sucesso");
                    return Ok(new { success = true, message = "Chapa confirmada com sucesso" });
                }
                
                return BadRequest(new { success = false, message = "Não foi possível confirmar a chapa" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao confirmar chapa {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cancela uma chapa
        /// </summary>
        [HttpPost("{id}/cancelar")]
        [Authorize(Roles = "Profissional,Admin")]
        public async Task<IActionResult> CancelarChapa(int id, [FromBody] CancelarChapaDTO dto)
        {
            try
            {
                var usuarioId = GetUsuarioLogadoId();
                var sucesso = await _chapaService.CancelarChapaAsync(id, dto.Motivo, usuarioId);
                
                if (sucesso)
                {
                    _logger.LogInformation($"Chapa {id} cancelada");
                    return Ok(new { success = true, message = "Chapa cancelada com sucesso" });
                }
                
                return BadRequest(new { success = false, message = "Não foi possível cancelar a chapa" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao cancelar chapa {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Gestão de Membros

        /// <summary>
        /// Adiciona um membro à chapa
        /// </summary>
        [HttpPost("{id}/membros")]
        [Authorize(Roles = "Profissional")]
        public async Task<IActionResult> AdicionarMembro(int id, [FromBody] AdicionarMembroChapaDTO dto)
        {
            try
            {
                dto.UsuarioAdicaoId = GetUsuarioLogadoId();
                
                var sucesso = await _chapaService.AdicionarMembroAsync(id, dto);
                
                if (sucesso)
                {
                    _logger.LogInformation($"Membro {dto.ProfissionalId} adicionado à chapa {id}");
                    return Ok(new { success = true, message = "Membro adicionado com sucesso" });
                }
                
                return BadRequest(new { success = false, message = "Não foi possível adicionar o membro" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao adicionar membro à chapa {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Confirma participação de um membro na chapa
        /// </summary>
        [HttpPost("{chapaId}/membros/{membroId}/confirmar")]
        [Authorize(Roles = "Profissional")]
        public async Task<IActionResult> ConfirmarParticipacao(int chapaId, int membroId)
        {
            try
            {
                var profissionalId = GetProfissionalLogadoId();
                var sucesso = await _chapaService.ConfirmarParticipaçãoMembroAsync(
                    chapaId, 
                    membroId, 
                    profissionalId);
                
                if (sucesso)
                {
                    _logger.LogInformation($"Participação do membro {membroId} confirmada na chapa {chapaId}");
                    return Ok(new { success = true, message = "Participação confirmada com sucesso" });
                }
                
                return BadRequest(new { success = false, message = "Não foi possível confirmar a participação" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao confirmar participação do membro {membroId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Remove um membro da chapa
        /// </summary>
        [HttpDelete("{chapaId}/membros/{membroId}")]
        [Authorize(Roles = "Profissional,Admin")]
        public async Task<IActionResult> RemoverMembro(int chapaId, int membroId)
        {
            try
            {
                var usuarioId = GetUsuarioLogadoId();
                var sucesso = await _chapaService.RemoverMembroAsync(chapaId, membroId, usuarioId);
                
                if (sucesso)
                {
                    _logger.LogInformation($"Membro {membroId} removido da chapa {chapaId}");
                    return Ok(new { success = true, message = "Membro removido com sucesso" });
                }
                
                return BadRequest(new { success = false, message = "Não foi possível remover o membro" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao remover membro {membroId} da chapa {chapaId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém os membros de uma chapa
        /// </summary>
        [HttpGet("{id}/membros")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterMembros(int id)
        {
            try
            {
                var membros = await _membroService.ObterMembrosPorChapaAsync(id);
                return Ok(new { success = true, data = membros });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter membros da chapa {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Consultas e Listagens

        /// <summary>
        /// Obtém chapa por ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                var chapa = await _chapaService.ObterChapaPorIdAsync(id);
                if (chapa == null)
                    return NotFound(new { success = false, message = "Chapa não encontrada" });

                return Ok(new { success = true, data = chapa });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter chapa {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista chapas com filtros e paginação
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ListarChapas([FromQuery] FiltroChapasDTO filtro)
        {
            try
            {
                var resultado = await _chapaService.ListarChapasAsync(filtro);
                return Ok(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar chapas");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém chapas por calendário
        /// </summary>
        [HttpGet("calendario/{calendarioId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPorCalendario(int calendarioId)
        {
            try
            {
                var chapas = await _chapaService.ObterChapasPorCalendarioAsync(calendarioId);
                return Ok(new { success = true, data = chapas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter chapas do calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém chapas por UF
        /// </summary>
        [HttpGet("uf/{ufId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPorUF(int ufId)
        {
            try
            {
                var chapas = await _chapaService.ObterChapasPorUFAsync(ufId);
                return Ok(new { success = true, data = chapas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter chapas da UF {ufId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Validações

        /// <summary>
        /// Valida uma chapa
        /// </summary>
        [HttpGet("{id}/validar")]
        [Authorize]
        public async Task<IActionResult> ValidarChapa(int id)
        {
            try
            {
                var resultado = await _chapaService.ValidarChapaAsync(id);
                return Ok(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao validar chapa {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Verifica se um profissional já está em alguma chapa
        /// </summary>
        [HttpGet("verificar-profissional")]
        [Authorize]
        public async Task<IActionResult> VerificarProfissionalEmChapa(
            [FromQuery] int profissionalId, 
            [FromQuery] int calendarioId)
        {
            try
            {
                var estaEmChapa = await _chapaService.VerificarProfissionalEmChapaAsync(
                    profissionalId, 
                    calendarioId);
                    
                return Ok(new { 
                    success = true, 
                    estaEmChapa = estaEmChapa 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar profissional {profissionalId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Valida elegibilidade de um profissional
        /// </summary>
        [HttpGet("validar-elegibilidade/{profissionalId}")]
        [Authorize]
        public async Task<IActionResult> ValidarElegibilidade(int profissionalId)
        {
            try
            {
                var resultado = await _validacaoService.ValidarElegibilidadeAsync(profissionalId);
                return Ok(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao validar elegibilidade do profissional {profissionalId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Documentação

        /// <summary>
        /// Anexa documento à chapa
        /// </summary>
        [HttpPost("{id}/documentos")]
        [Authorize(Roles = "Profissional")]
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

                var anexarDto = new AnexarDocumentoDTO
                {
                    TipoDocumento = dto.TipoDocumento,
                    NomeArquivo = dto.Arquivo?.FileName,
                    ConteudoArquivo = conteudo,
                    UsuarioUploadId = GetUsuarioLogadoId()
                };

                var sucesso = await _chapaService.AnexarDocumentoAsync(id, anexarDto);
                
                if (sucesso)
                {
                    _logger.LogInformation($"Documento anexado à chapa {id}");
                    return Ok(new { success = true, message = "Documento anexado com sucesso" });
                }
                
                return BadRequest(new { success = false, message = "Não foi possível anexar o documento" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao anexar documento à chapa {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Remove documento da chapa
        /// </summary>
        [HttpDelete("{chapaId}/documentos/{documentoId}")]
        [Authorize(Roles = "Profissional,Admin")]
        public async Task<IActionResult> RemoverDocumento(int chapaId, int documentoId)
        {
            try
            {
                var sucesso = await _chapaService.RemoverDocumentoAsync(chapaId, documentoId);
                
                if (sucesso)
                {
                    _logger.LogInformation($"Documento {documentoId} removido da chapa {chapaId}");
                    return Ok(new { success = true, message = "Documento removido com sucesso" });
                }
                
                return BadRequest(new { success = false, message = "Não foi possível remover o documento" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao remover documento {documentoId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Estatísticas

        /// <summary>
        /// Obtém estatísticas das chapas
        /// </summary>
        [HttpGet("estatisticas/{calendarioId}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterEstatisticas(int calendarioId)
        {
            try
            {
                var estatisticas = await _chapaService.ObterEstatisticasAsync(calendarioId);
                return Ok(new { success = true, data = estatisticas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter estatísticas do calendário {calendarioId}");
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

        #endregion
    }

    #region DTOs Adicionais

    public class CancelarChapaDTO
    {
        public string Motivo { get; set; }
    }

    public class AnexarDocumentoFormDTO
    {
        public string TipoDocumento { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile Arquivo { get; set; }
    }

    #endregion
}