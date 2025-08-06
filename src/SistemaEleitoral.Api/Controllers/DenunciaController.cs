using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Interfaces.Services;

namespace SistemaEleitoral.Api.Controllers
{
    /// <summary>
    /// Controller responsável pela gestão de denúncias eleitorais
    /// </summary>
    [ApiController]
    [Route("api/denuncias")]
    [Authorize]
    public class DenunciaController : ControllerBase
    {
        private readonly IDenunciaService _denunciaService;
        private readonly ILogger<DenunciaController> _logger;

        public DenunciaController(
            IDenunciaService denunciaService,
            ILogger<DenunciaController> logger)
        {
            _denunciaService = denunciaService;
            _logger = logger;
        }

        #region Registro de Denúncias

        /// <summary>
        /// Registra uma nova denúncia
        /// </summary>
        [HttpPost("registrar")]
        [AllowAnonymous] // Permite denúncias anônimas
        public async Task<IActionResult> RegistrarDenuncia([FromBody] RegistrarDenunciaDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Se não for anônima, pegar o denunciante logado
                if (!dto.DenuncianteAnonimo && User.Identity.IsAuthenticated)
                {
                    dto.DenuncianteId = GetProfissionalLogadoId();
                }

                dto.UsuarioRegistroId = User.Identity.IsAuthenticated ? GetUsuarioLogadoId() : 0;

                var denuncia = await _denunciaService.RegistrarDenunciaAsync(dto);

                _logger.LogInformation($"Denúncia {denuncia.ProtocoloNumero} registrada com sucesso");

                return CreatedAtAction(
                    nameof(ObterPorId),
                    new { id = denuncia.Id },
                    new { success = true, data = denuncia, protocolo = denuncia.ProtocoloNumero });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar denúncia");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Encaminhamento e Análise

        /// <summary>
        /// Encaminha denúncia para relator
        /// </summary>
        [HttpPost("{id}/encaminhar")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> EncaminharParaRelator(int id, [FromBody] EncaminharDenunciaDTO dto)
        {
            try
            {
                dto.UsuarioEncaminhamentoId = GetUsuarioLogadoId();

                var sucesso = await _denunciaService.EncaminharParaRelatorAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Denúncia {id} encaminhada para relator {dto.RelatorId}");
                    return Ok(new { success = true, message = "Denúncia encaminhada com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível encaminhar a denúncia" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao encaminhar denúncia {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Admite ou inadmite uma denúncia
        /// </summary>
        [HttpPost("{id}/admissibilidade")]
        [Authorize(Roles = "Relator,ComissaoEleitoral,Admin")]
        public async Task<IActionResult> AdmitirInadmitir(int id, [FromBody] AdmitirDenunciaDTO dto)
        {
            try
            {
                dto.UsuarioId = GetUsuarioLogadoId();

                var sucesso = await _denunciaService.AdmitirInadmitirDenunciaAsync(id, dto);

                if (sucesso)
                {
                    var acao = dto.Admitir ? "admitida" : "inadmitida";
                    _logger.LogInformation($"Denúncia {id} {acao}");
                    return Ok(new { success = true, message = $"Denúncia {acao} com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível processar admissibilidade" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar admissibilidade da denúncia {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Defesa e Contraditório

        /// <summary>
        /// Registra defesa para uma denúncia
        /// </summary>
        [HttpPost("{id}/defesa")]
        [Authorize]
        public async Task<IActionResult> RegistrarDefesa(int id, [FromBody] RegistrarDefesaDTO dto)
        {
            try
            {
                dto.ApresentadaPorId = GetProfissionalLogadoId();
                dto.UsuarioRegistroId = GetUsuarioLogadoId();

                var sucesso = await _denunciaService.RegistrarDefesaAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Defesa registrada para denúncia {id}");
                    return Ok(new { success = true, message = "Defesa registrada com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível registrar a defesa" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar defesa para denúncia {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Registra provas adicionais
        /// </summary>
        [HttpPost("{id}/provas")]
        [Authorize]
        public async Task<IActionResult> RegistrarProvas(int id, [FromBody] RegistrarProvasDTO dto)
        {
            try
            {
                dto.UsuarioRegistroId = GetUsuarioLogadoId();

                var sucesso = await _denunciaService.RegistrarProvasAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Provas registradas para denúncia {id}");
                    return Ok(new { success = true, message = "Provas registradas com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível registrar as provas" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar provas para denúncia {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Consultas

        /// <summary>
        /// Obtém denúncia por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "ComissaoEleitoral,Relator,Admin")]
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                var denuncia = await _denunciaService.ObterDenunciaPorIdAsync(id);
                if (denuncia == null)
                    return NotFound(new { success = false, message = "Denúncia não encontrada" });

                return Ok(new { success = true, data = denuncia });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter denúncia {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém denúncia por protocolo
        /// </summary>
        [HttpGet("protocolo/{protocolo}")]
        [AllowAnonymous] // Permite consulta pública por protocolo
        public async Task<IActionResult> ObterPorProtocolo(string protocolo)
        {
            try
            {
                var denuncia = await _denunciaService.ObterDenunciaPorProtocoloAsync(protocolo);
                if (denuncia == null)
                    return NotFound(new { success = false, message = "Denúncia não encontrada" });

                // Retornar versão resumida para consulta pública
                if (!User.Identity.IsAuthenticated)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            ProtocoloNumero = denuncia.ProtocoloNumero,
                            Situacao = denuncia.Situacao,
                            DataRegistro = denuncia.DataRegistro,
                            TipoDenuncia = denuncia.TipoDenuncia
                        }
                    });
                }

                return Ok(new { success = true, data = denuncia });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter denúncia por protocolo {protocolo}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista denúncias com filtros
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "ComissaoEleitoral,Relator,Admin")]
        public async Task<IActionResult> ListarDenuncias([FromQuery] FiltroDenunciasDTO filtro)
        {
            try
            {
                var resultado = await _denunciaService.ListarDenunciasAsync(filtro);
                return Ok(new { success = true, data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar denúncias");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém denúncias por chapa
        /// </summary>
        [HttpGet("chapa/{chapaId}")]
        [Authorize]
        public async Task<IActionResult> ObterPorChapa(int chapaId)
        {
            try
            {
                var denuncias = await _denunciaService.ObterDenunciasPorChapaAsync(chapaId);
                return Ok(new { success = true, data = denuncias });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter denúncias da chapa {chapaId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém denúncias por membro
        /// </summary>
        [HttpGet("membro/{membroId}")]
        [Authorize]
        public async Task<IActionResult> ObterPorMembro(int membroId)
        {
            try
            {
                var denuncias = await _denunciaService.ObterDenunciasPorMembroAsync(membroId);
                return Ok(new { success = true, data = denuncias });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter denúncias do membro {membroId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém denúncias por relator
        /// </summary>
        [HttpGet("relator/{relatorId}")]
        [Authorize(Roles = "Relator,ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ObterPorRelator(int relatorId)
        {
            try
            {
                var denuncias = await _denunciaService.ObterDenunciasPorRelatorAsync(relatorId);
                return Ok(new { success = true, data = denuncias });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter denúncias do relator {relatorId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Acompanhamento

        /// <summary>
        /// Obtém acompanhamento de denúncia
        /// </summary>
        [HttpGet("acompanhamento/{protocolo}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterAcompanhamento(string protocolo)
        {
            try
            {
                var acompanhamento = await _denunciaService.ObterAcompanhamentoAsync(protocolo);
                if (acompanhamento == null)
                    return NotFound(new { success = false, message = "Denúncia não encontrada" });

                return Ok(new { success = true, data = acompanhamento });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter acompanhamento do protocolo {protocolo}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Estatísticas

        /// <summary>
        /// Obtém estatísticas de denúncias
        /// </summary>
        [HttpGet("estatisticas/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ObterEstatisticas(int calendarioId)
        {
            try
            {
                var estatisticas = await _denunciaService.ObterEstatisticasAsync(calendarioId);
                return Ok(new { success = true, data = estatisticas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter estatísticas do calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Impedimentos e Suspeições

        /// <summary>
        /// Registra impedimento ou suspeição
        /// </summary>
        [HttpPost("{id}/impedimento-suspeicao")]
        [Authorize(Roles = "Relator,ComissaoEleitoral")]
        public async Task<IActionResult> RegistrarImpedimentoSuspeicao(int id, [FromBody] ImpedimentoSuspeicaoDTO dto)
        {
            try
            {
                dto.SolicitanteId = GetProfissionalLogadoId();

                var sucesso = await _denunciaService.RegistrarImpedimentoSuspeicaoAsync(id, dto);

                if (sucesso)
                {
                    var tipo = dto.Tipo == TipoImpedimento.Impedimento ? "Impedimento" : "Suspeição";
                    _logger.LogInformation($"{tipo} registrado para denúncia {id}");
                    return Ok(new { success = true, message = $"{tipo} registrado com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível registrar impedimento/suspeição" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar impedimento/suspeição para denúncia {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Documentos

        /// <summary>
        /// Anexa documento à denúncia
        /// </summary>
        [HttpPost("{id}/documentos")]
        [Authorize]
        public async Task<IActionResult> AnexarDocumento(int id, [FromForm] AnexarDocumentoDenunciaFormDTO dto)
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

                var anexarDto = new AnexarDocumentoDenunciaDTO
                {
                    TipoDocumento = dto.TipoDocumento,
                    NomeArquivo = dto.Arquivo?.FileName,
                    ConteudoArquivo = conteudo,
                    UsuarioUploadId = GetUsuarioLogadoId()
                };

                var sucesso = await _denunciaService.AnexarDocumentoAsync(id, anexarDto);

                if (sucesso)
                {
                    _logger.LogInformation($"Documento anexado à denúncia {id}");
                    return Ok(new { success = true, message = "Documento anexado com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível anexar o documento" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao anexar documento à denúncia {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Remove documento da denúncia
        /// </summary>
        [HttpDelete("{denunciaId}/documentos/{documentoId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> RemoverDocumento(int denunciaId, int documentoId)
        {
            try
            {
                var sucesso = await _denunciaService.RemoverDocumentoAsync(denunciaId, documentoId);

                if (sucesso)
                {
                    _logger.LogInformation($"Documento {documentoId} removido da denúncia {denunciaId}");
                    return Ok(new { success = true, message = "Documento removido com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível remover o documento" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao remover documento {documentoId} da denúncia {denunciaId}");
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

    public class AnexarDocumentoDenunciaFormDTO
    {
        public string TipoDocumento { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile Arquivo { get; set; }
    }

    #endregion
}