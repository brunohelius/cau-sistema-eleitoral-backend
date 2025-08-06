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
    /// Controller responsável pela gestão das Comissões Eleitorais
    /// </summary>
    [ApiController]
    [Route("api/comissao-eleitoral")]
    [Authorize]
    public class ComissaoEleitoralController : ControllerBase
    {
        private readonly IComissaoEleitoralService _comissaoService;
        private readonly ILogger<ComissaoEleitoralController> _logger;

        public ComissaoEleitoralController(
            IComissaoEleitoralService comissaoService,
            ILogger<ComissaoEleitoralController> logger)
        {
            _comissaoService = comissaoService;
            _logger = logger;
        }

        #region Criação e Gestão de Comissões

        /// <summary>
        /// Cria uma nova comissão eleitoral
        /// </summary>
        [HttpPost("criar")]
        [Authorize(Roles = "Admin,GestorEleitoral")]
        public async Task<IActionResult> CriarComissao([FromBody] CriarComissaoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.UsuarioCriacaoId = GetUsuarioLogadoId();

                var comissao = await _comissaoService.CriarComissaoAsync(dto);

                _logger.LogInformation($"Comissão eleitoral criada: {comissao.Nome} para calendário {dto.CalendarioId}");

                // Notificar membros nomeados
                BackgroundJob.Enqueue(() => NotificarMembroNomeado(comissao.Id));

                return CreatedAtAction(
                    nameof(ObterPorId),
                    new { id = comissao.Id },
                    new 
                    { 
                        success = true, 
                        data = comissao,
                        message = "Comissão eleitoral criada com sucesso"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar comissão eleitoral");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza dados da comissão
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,GestorEleitoral,PresidenteComissao")]
        public async Task<IActionResult> AtualizarComissao(int id, [FromBody] AtualizarComissaoDTO dto)
        {
            try
            {
                dto.UsuarioAtualizacaoId = GetUsuarioLogadoId();

                var sucesso = await _comissaoService.AtualizarComissaoAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Comissão {id} atualizada");
                    return Ok(new { success = true, message = "Comissão atualizada com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível atualizar a comissão" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao atualizar comissão {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Ativa ou desativa comissão
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,GestorEleitoral")]
        public async Task<IActionResult> AlterarStatusComissao(int id, [FromBody] AlterarStatusDTO dto)
        {
            try
            {
                var sucesso = await _comissaoService.AlterarStatusAsync(id, dto.Ativo, GetUsuarioLogadoId());

                if (sucesso)
                {
                    var status = dto.Ativo ? "ativada" : "desativada";
                    _logger.LogInformation($"Comissão {id} {status}");
                    return Ok(new { success = true, message = $"Comissão {status} com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível alterar o status" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao alterar status da comissão {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Gestão de Membros

        /// <summary>
        /// Adiciona membro à comissão
        /// </summary>
        [HttpPost("{id}/membros")]
        [Authorize(Roles = "Admin,GestorEleitoral,PresidenteComissao")]
        public async Task<IActionResult> AdicionarMembro(int id, [FromBody] AdicionarMembroComissaoDTO dto)
        {
            try
            {
                dto.UsuarioNomeacaoId = GetUsuarioLogadoId();

                var sucesso = await _comissaoService.AdicionarMembroAsync(id, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Membro {dto.ProfissionalId} adicionado à comissão {id}");
                    
                    // Notificar novo membro
                    BackgroundJob.Enqueue(() => NotificarNovoMembro(id, dto.ProfissionalId));
                    
                    return Ok(new { success = true, message = "Membro adicionado com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível adicionar o membro" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao adicionar membro à comissão {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Remove membro da comissão
        /// </summary>
        [HttpDelete("{comissaoId}/membros/{membroId}")]
        [Authorize(Roles = "Admin,GestorEleitoral,PresidenteComissao")]
        public async Task<IActionResult> RemoverMembro(int comissaoId, int membroId, [FromBody] RemoverMembroDTO dto)
        {
            try
            {
                dto.UsuarioRemocaoId = GetUsuarioLogadoId();

                var sucesso = await _comissaoService.RemoverMembroAsync(comissaoId, membroId, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Membro {membroId} removido da comissão {comissaoId}");
                    
                    // Notificar membro removido
                    BackgroundJob.Enqueue(() => NotificarMembroRemovido(membroId, dto.Motivo));
                    
                    return Ok(new { success = true, message = "Membro removido com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível remover o membro" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao remover membro {membroId} da comissão {comissaoId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Atualiza cargo do membro na comissão
        /// </summary>
        [HttpPatch("{comissaoId}/membros/{membroId}/cargo")]
        [Authorize(Roles = "Admin,GestorEleitoral,PresidenteComissao")]
        public async Task<IActionResult> AtualizarCargoMembro(int comissaoId, int membroId, [FromBody] AtualizarCargoMembroDTO dto)
        {
            try
            {
                dto.UsuarioAtualizacaoId = GetUsuarioLogadoId();

                var sucesso = await _comissaoService.AtualizarCargoMembroAsync(comissaoId, membroId, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Cargo do membro {membroId} atualizado na comissão {comissaoId}");
                    return Ok(new { success = true, message = "Cargo atualizado com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível atualizar o cargo" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao atualizar cargo do membro {membroId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista membros da comissão
        /// </summary>
        [HttpGet("{id}/membros")]
        [Authorize]
        public async Task<IActionResult> ObterMembros(int id)
        {
            try
            {
                var membros = await _comissaoService.ObterMembrosAsync(id);
                
                return Ok(new 
                { 
                    success = true, 
                    data = membros,
                    total = membros.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter membros da comissão {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Atividades e Deliberações

        /// <summary>
        /// Registra atividade da comissão
        /// </summary>
        [HttpPost("{id}/atividades")]
        [Authorize(Roles = "MembroComissao,Admin")]
        public async Task<IActionResult> RegistrarAtividade(int id, [FromBody] RegistrarAtividadeComissaoDTO dto)
        {
            try
            {
                dto.RegistradoPorId = GetProfissionalLogadoId();

                var atividade = await _comissaoService.RegistrarAtividadeAsync(id, dto);

                _logger.LogInformation($"Atividade registrada para comissão {id}: {dto.TipoAtividade}");

                return Ok(new 
                { 
                    success = true, 
                    data = atividade,
                    message = "Atividade registrada com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar atividade para comissão {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Registra deliberação da comissão
        /// </summary>
        [HttpPost("{id}/deliberacoes")]
        [Authorize(Roles = "MembroComissao,Admin")]
        public async Task<IActionResult> RegistrarDeliberacao(int id, [FromBody] RegistrarDeliberacaoDTO dto)
        {
            try
            {
                dto.RelatorId = GetProfissionalLogadoId();

                var deliberacao = await _comissaoService.RegistrarDeliberacaoAsync(id, dto);

                _logger.LogInformation($"Deliberação registrada para comissão {id}");

                // Notificar interessados
                BackgroundJob.Enqueue(() => NotificarDeliberacao(deliberacao.Id));

                return Ok(new 
                { 
                    success = true, 
                    data = deliberacao,
                    message = "Deliberação registrada com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar deliberação para comissão {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista atividades da comissão
        /// </summary>
        [HttpGet("{id}/atividades")]
        [Authorize]
        public async Task<IActionResult> ObterAtividades(int id, [FromQuery] int? pagina = 1, [FromQuery] int? tamanhoPagina = 20)
        {
            try
            {
                var atividades = await _comissaoService.ObterAtividadesAsync(id, pagina.Value, tamanhoPagina.Value);
                
                return Ok(new 
                { 
                    success = true, 
                    data = atividades
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter atividades da comissão {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Reuniões

        /// <summary>
        /// Agenda reunião da comissão
        /// </summary>
        [HttpPost("{id}/reunioes")]
        [Authorize(Roles = "PresidenteComissao,SecretarioComissao,Admin")]
        public async Task<IActionResult> AgendarReuniao(int id, [FromBody] AgendarReuniaoDTO dto)
        {
            try
            {
                dto.ConvocadaPorId = GetProfissionalLogadoId();

                var reuniao = await _comissaoService.AgendarReuniaoAsync(id, dto);

                _logger.LogInformation($"Reunião agendada para comissão {id}: {dto.DataReuniao}");

                // Enviar convocações
                BackgroundJob.Enqueue(() => EnviarConvocacaoReuniao(reuniao.Id));

                return Ok(new 
                { 
                    success = true, 
                    data = reuniao,
                    message = "Reunião agendada com sucesso"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao agendar reunião para comissão {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Registra ata de reunião
        /// </summary>
        [HttpPost("reunioes/{reuniaoId}/ata")]
        [Authorize(Roles = "SecretarioComissao,PresidenteComissao,Admin")]
        public async Task<IActionResult> RegistrarAta(int reuniaoId, [FromBody] RegistrarAtaReuniaoDTO dto)
        {
            try
            {
                dto.SecretarioId = GetProfissionalLogadoId();

                var sucesso = await _comissaoService.RegistrarAtaReuniaoAsync(reuniaoId, dto);

                if (sucesso)
                {
                    _logger.LogInformation($"Ata registrada para reunião {reuniaoId}");
                    
                    // Disponibilizar ata
                    BackgroundJob.Enqueue(() => DisponibilizarAta(reuniaoId));
                    
                    return Ok(new { success = true, message = "Ata registrada com sucesso" });
                }

                return BadRequest(new { success = false, message = "Não foi possível registrar a ata" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar ata da reunião {reuniaoId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Consultas

        /// <summary>
        /// Obtém comissão por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                var comissao = await _comissaoService.ObterPorIdAsync(id);
                
                if (comissao == null)
                    return NotFound(new { success = false, message = "Comissão não encontrada" });

                return Ok(new { success = true, data = comissao });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter comissão {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista comissões por calendário
        /// </summary>
        [HttpGet("calendario/{calendarioId}")]
        [Authorize]
        public async Task<IActionResult> ObterPorCalendario(int calendarioId)
        {
            try
            {
                var comissoes = await _comissaoService.ObterPorCalendarioAsync(calendarioId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = comissoes,
                    total = comissoes.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter comissões do calendário {calendarioId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Lista comissões por UF
        /// </summary>
        [HttpGet("uf/{ufId}")]
        [Authorize]
        public async Task<IActionResult> ObterPorUf(int ufId)
        {
            try
            {
                var comissoes = await _comissaoService.ObterPorUfAsync(ufId);
                
                return Ok(new 
                { 
                    success = true, 
                    data = comissoes,
                    total = comissoes.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter comissões da UF {ufId}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Verifica se profissional é membro de comissão
        /// </summary>
        [HttpGet("verificar-membro/{profissionalId}")]
        [Authorize]
        public async Task<IActionResult> VerificarSeMembro(int profissionalId, [FromQuery] int? calendarioId = null)
        {
            try
            {
                var ehMembro = await _comissaoService.VerificarSeMembroAsync(profissionalId, calendarioId);
                
                return Ok(new 
                { 
                    success = true, 
                    ehMembro = ehMembro,
                    message = ehMembro ? "É membro de comissão" : "Não é membro de comissão"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar se {profissionalId} é membro de comissão");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Relatórios

        /// <summary>
        /// Gera relatório de atividades
        /// </summary>
        [HttpGet("{id}/relatorio")]
        [Authorize(Roles = "MembroComissao,Admin")]
        public async Task<IActionResult> GerarRelatorio(int id, [FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
        {
            try
            {
                var relatorio = await _comissaoService.GerarRelatorioAtividadesAsync(id, dataInicio, dataFim);
                
                _logger.LogInformation($"Relatório gerado para comissão {id}");

                return File(
                    relatorio.ConteudoPDF, 
                    "application/pdf", 
                    $"relatorio_comissao_{id}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório para comissão {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Exporta lista de membros
        /// </summary>
        [HttpGet("{id}/membros/exportar")]
        [Authorize(Roles = "MembroComissao,Admin")]
        public async Task<IActionResult> ExportarMembros(int id, [FromQuery] string formato = "pdf")
        {
            try
            {
                var arquivo = await _comissaoService.ExportarMembrosAsync(id, formato);
                
                var contentType = formato.ToLower() == "excel" ? 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : 
                    "application/pdf";
                
                var extensao = formato.ToLower() == "excel" ? "xlsx" : "pdf";

                return File(
                    arquivo.Conteudo, 
                    contentType, 
                    $"membros_comissao_{id}_{DateTime.Now:yyyyMMddHHmmss}.{extensao}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao exportar membros da comissão {id}");
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
        public async Task NotificarMembroNomeado(int comissaoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task NotificarNovoMembro(int comissaoId, int profissionalId)
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
        public async Task NotificarDeliberacao(int deliberacaoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task EnviarConvocacaoReuniao(int reuniaoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task DisponibilizarAta(int reuniaoId)
        {
            // Implementação do job
            await Task.CompletedTask;
        }

        #endregion
    }
}