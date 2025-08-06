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
    /// Controller responsável pela gestão de membros das chapas eleitorais
    /// </summary>
    [ApiController]
    [Route("api/membros-chapa")]
    [Authorize]
    public class MembroChapaController : ControllerBase
    {
        private readonly IMembroChapaService _membroChapaService;
        private readonly IValidacaoElegibilidadeService _validacaoService;
        private readonly ILogger<MembroChapaController> _logger;

        public MembroChapaController(
            IMembroChapaService membroChapaService,
            IValidacaoElegibilidadeService validacaoService,
            ILogger<MembroChapaController> logger)
        {
            _membroChapaService = membroChapaService;
            _validacaoService = validacaoService;
            _logger = logger;
        }

        #region Gestão de Membros

        /// <summary>
        /// Adiciona membro à chapa
        /// </summary>
        [HttpPost("adicionar")]
        [Authorize]
        public async Task<IActionResult> AdicionarMembro([FromBody] AdicionarMembroChapaDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.UsuarioRegistroId = GetUsuarioLogadoId();

                // Validar elegibilidade primeiro
                var elegibilidade = await _validacaoService.ValidarElegibilidadeAsync(dto.ProfissionalId, dto.CalendarioId);
                if (!elegibilidade.EhElegivel)
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "Profissional não é elegível",
                        motivos = elegibilidade.MotivosPendencias
                    });
                }

                var membro = await _membroChapaService.AdicionarMembroAsync(dto);

                _logger.LogInformation($"Membro {dto.ProfissionalId} adicionado à chapa {dto.ChapaId}");

                // Enviar convite
                BackgroundJob.Enqueue(() => EnviarConviteMembro(membro.Id));

                return CreatedAtAction(
                    nameof(ObterPorId),
                    new { id = membro.Id },
                    new 
                    { 
                        success = true, 
                        data = membro,
                        message = "Membro adicionado com sucesso"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar membro à chapa");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Remove membro da chapa
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> RemoverMembro(int id, [FromBody] RemoverMembroChapaDTO dto)
        {
            try
            {
                dto.UsuarioRemocaoId = GetUsuarioLogadoId();

                var sucesso = await _membroChapaService.RemoverMembroAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Membro {id} removido da chapa");
                    
                    // Notificar membro removido
                    BackgroundJob.Enqueue(() => NotificarMembroRemovido(id, dto.Motivo));
                    
                    return Ok(new { success = true, message = "Membro removido com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível remover o membro" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao remover membro {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Substitui membro da chapa
        /// </summary>
        [HttpPost("{id}/substituir")]
        [Authorize]
        public async Task<IActionResult> SubstituirMembro(int id, [FromBody] SubstituirMembroChapaDTO dto)
        {
            try
            {
                dto.UsuarioSubstituicaoId = GetUsuarioLogadoId();

                // Validar elegibilidade do substituto
                var elegibilidade = await _validacaoService.ValidarElegibilidadeAsync(dto.NovoProfissionalId, dto.CalendarioId);
                if (!elegibilidade.EhElegivel)
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "Substituto não é elegível",
                        motivos = elegibilidade.MotivosPendencias
                    });
                }

                var novoMembro = await _membroChapaService.SubstituirMembroAsync(id, dto);

                _logger.LogInformation($"Membro {id} substituído por {dto.NovoProfissionalId}");

                // Notificar ambos
                BackgroundJob.Enqueue(() => NotificarSubstituicao(id, novoMembro.Id));

                return Ok(new 
                { 
                    success = true, 
                    data = novoMembro,
                    message = "Membro substituído com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao substituir membro {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Aceite de Convite

        /// <summary>
        /// Aceita convite para participar da chapa
        /// </summary>
        [HttpPost("{id}/aceitar")]
        [Authorize]
        public async Task<IActionResult> AceitarConvite(int id, [FromBody] AceitarConviteDTO dto)
        {
            try
            {
                var profissionalId = GetProfissionalLogadoId();
                dto.ProfissionalId = profissionalId;

                var sucesso = await _membroChapaService.AceitarConviteAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Convite {id} aceito pelo profissional {profissionalId}");
                    
                    // Notificar responsável da chapa
                    BackgroundJob.Enqueue(() => NotificarConviteAceito(id));
                    
                    return Ok(new { success = true, message = "Convite aceito com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível aceitar o convite" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao aceitar convite {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Recusa convite para participar da chapa
        /// </summary>
        [HttpPost("{id}/recusar")]
        [Authorize]
        public async Task<IActionResult> RecusarConvite(int id, [FromBody] RecusarConviteDTO dto)
        {
            try
            {
                var profissionalId = GetProfissionalLogadoId();
                dto.ProfissionalId = profissionalId;

                var sucesso = await _membroChapaService.RecusarConviteAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Convite {id} recusado pelo profissional {profissionalId}");
                    
                    // Notificar responsável da chapa
                    BackgroundJob.Enqueue(() => NotificarConviteRecusado(id, dto.Motivo));
                    
                    return Ok(new { success = true, message = "Convite recusado" });
                }

                return BadRequest(new { success = false, message = "Não foi possível recusar o convite" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao recusar convite {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Valida token de convite
        /// </summary>
        [HttpGet("convite/validar/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidarTokenConvite(string token)
        {
            try
            {
                var convite = await _membroChapaService.ValidarTokenConviteAsync(token);
                
                if (convite == null)
                    return BadRequest(new { success = false, message = "Token inválido ou expirado" });

                return Ok(new 
                { 
                    success = true, 
                    data = convite,
                    message = "Token válido"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao validar token {token}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Ordenação e Cargos

        /// <summary>
        /// Atualiza ordem dos membros na chapa
        /// </summary>
        [HttpPut("chapa/{chapaId}/ordenar")]
        [Authorize]
        public async Task<IActionResult> OrdenarMembros(int chapaId, [FromBody] OrdenarMembrosDTO dto)
        {
            try
            {
                dto.UsuarioOrdenacaoId = GetUsuarioLogadoId();

                var sucesso = await _membroChapaService.OrdenarMembrosAsync(chapaId, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Ordem dos membros atualizada para chapa {chapaId}");
                    return Ok(new { success = true, message = "Ordem atualizada com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível atualizar a ordem" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao ordenar membros da chapa {chapaId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza cargo do membro na chapa
        /// </summary>
        [HttpPatch("{id}/cargo")]
        [Authorize]
        public async Task<IActionResult> AtualizarCargo(int id, [FromBody] AtualizarCargoMembroChapaDTO dto)
        {
            try
            {
                dto.UsuarioAtualizacaoId = GetUsuarioLogadoId();

                var sucesso = await _membroChapaService.AtualizarCargoAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Cargo do membro {id} atualizado para {dto.NovoCargo}");
                    return Ok(new { success = true, message = "Cargo atualizado com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível atualizar o cargo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao atualizar cargo do membro {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Elegibilidade

        /// <summary>
        /// Verifica elegibilidade do membro
        /// </summary>
        [HttpGet("{id}/elegibilidade")]
        [Authorize]
        public async Task<IActionResult> VerificarElegibilidade(int id)
        {
            try
            {
                var elegibilidade = await _membroChapaService.VerificarElegibilidadeMembroAsync(id);
                
                return Ok(new 
                { 
                    success = true, 
                    data = elegibilidade
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar elegibilidade do membro {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Valida elegibilidade de profissional
        /// </summary>
        [HttpPost("validar-elegibilidade")]
        [Authorize]
        public async Task<IActionResult> ValidarElegibilidade([FromBody] ValidarElegibilidadeDTO dto)
        {
            try
            {
                var resultado = await _validacaoService.ValidarElegibilidadeAsync(dto.ProfissionalId, dto.CalendarioId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = resultado,
                    elegivel = resultado.EhElegivel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar elegibilidade");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza status de elegibilidade
        /// </summary>
        [HttpPatch("{id}/elegibilidade/atualizar")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> AtualizarStatusElegibilidade(int id)
        {
            try
            {
                var resultado = await _membroChapaService.AtualizarStatusElegibilidadeAsync(id);
                
                if (resultado.StatusAlterado)
                {
                    _logger.LogInformation($"Status de elegibilidade do membro {id} atualizado");
                    
                    // Notificar alteração
                    if (!resultado.EhElegivel)
                    {
                        BackgroundJob.Enqueue(() => NotificarInelegibilidade(id, resultado.Motivos));
                    }
                }

                return Ok(new 
                { 
                    success = true, 
                    data = resultado,
                    message = resultado.StatusAlterado ? 
                        "Status atualizado" : 
                        "Sem alterações necessárias"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao atualizar status de elegibilidade do membro {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Consultas

        /// <summary>
        /// Obtém membro por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                var membro = await _membroChapaService.ObterPorIdAsync(id);
                
                if (membro == null)
                    return NotFound(new { success = false, message = "Membro não encontrado" });

                return Ok(new { success = true, data = membro });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter membro {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista membros da chapa
        /// </summary>
        [HttpGet("chapa/{chapaId}")]
        [Authorize]
        public async Task<IActionResult> ObterPorChapa(int chapaId)
        {
            try
            {
                var membros = await _membroChapaService.ObterPorChapaAsync(chapaId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = membros,
                    total = membros.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter membros da chapa {chapaId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista convites pendentes do profissional
        /// </summary>
        [HttpGet("meus-convites")]
        [Authorize]
        public async Task<IActionResult> ObterMeusConvites()
        {
            try
            {
                var profissionalId = GetProfissionalLogadoId();
                var convites = await _membroChapaService.ObterConvitesPendentesAsync(profissionalId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = convites,
                    total = convites.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter convites pendentes");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Verifica se profissional já está em alguma chapa
        /// </summary>
        [HttpGet("verificar-participacao/{profissionalId}/{calendarioId}")]
        [Authorize]
        public async Task<IActionResult> VerificarParticipacao(int profissionalId, int calendarioId)
        {
            try
            {
                var participacao = await _membroChapaService.VerificarParticipacaoAsync(profissionalId, calendarioId);
                
                return Ok(new 
                { 
                    success = true, 
                    participaDeChapa = participacao.ParticipaNaChapa,
                    chapaId = participacao.ChapaId,
                    nomeCargo = participacao.NomeCargo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar participação de {profissionalId} no calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Documentação

        /// <summary>
        /// Anexa documento do membro
        /// </summary>
        [HttpPost("{id}/documentos")]
        [Authorize]
        public async Task<IActionResult> AnexarDocumento(int id, [FromForm] AnexarDocumentoMembroFormDTO dto)
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

                var anexarDto = new AnexarDocumentoMembroDTO
                {
                    TipoDocumento = dto.TipoDocumento,
                    NomeArquivo = dto.Arquivo?.FileName,
                    ConteudoArquivo = conteudo,
                    UsuarioUploadId = GetUsuarioLogadoId()
                };

                var sucesso = await _membroChapaService.AnexarDocumentoAsync(id, anexarDto);

                if (sucesso)
                {
                    _logger.LogInformation($"Documento anexado ao membro {id}");
                    return Ok(new { success = true, message = "Documento anexado com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível anexar o documento" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao anexar documento ao membro {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista documentos do membro
        /// </summary>
        [HttpGet("{id}/documentos")]
        [Authorize]
        public async Task<IActionResult> ObterDocumentos(int id)
        {
            try
            {
                var documentos = await _membroChapaService.ObterDocumentosAsync(id);
                
                return Ok(new 
                { 
                    success = true, 
                    data = documentos,
                    total = documentos.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter documentos do membro {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Estatísticas

        /// <summary>
        /// Obtém estatísticas de membros por calendário
        /// </summary>
        [HttpGet("estatisticas/calendario/{calendarioId}")]
        [Authorize(Roles = "ComissaoEleitoral,Admin")]
        public async Task<IActionResult> ObterEstatisticas(int calendarioId)
        {
            try
            {
                var estatisticas = await _membroChapaService.ObterEstatisticasAsync(calendarioId);
                
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
        public async Task EnviarConviteMembro(int membroId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarMembroRemovido(int membroId, string motivo)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarSubstituicao(int membroAntigoId, int membroNovoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarConviteAceito(int membroId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarConviteRecusado(int membroId, string motivo)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarInelegibilidade(int membroId, List<string> motivos)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        #endregion
    }

    /// <summary>
    /// DTO para upload de documento
    /// </summary>
    public class AnexarDocumentoMembroFormDTO
    {
        public string TipoDocumento { get; set; }
        public Microsoft.AspNetCore.Http.IFormFile Arquivo { get; set; }
    }
}