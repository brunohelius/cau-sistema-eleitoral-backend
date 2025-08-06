using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Interfaces.Services;
using System.Threading.Tasks;
using System;

namespace SistemaEleitoral.Api.Controllers
{
    /// <summary>
    /// Controller para gestão de eleições
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EleicaoController : ControllerBase
    {
        private readonly IEleicaoService _eleicaoService;
        private readonly ILogger<EleicaoController> _logger;

        public EleicaoController(
            IEleicaoService eleicaoService,
            ILogger<EleicaoController> logger)
        {
            _eleicaoService = eleicaoService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todas as eleições
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ListarEleicoes([FromQuery] FiltroEleicoesDTO filtro)
        {
            try
            {
                var eleicoes = await _eleicaoService.ListarEleicoesAsync(filtro);
                return Ok(eleicoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar eleições");
                return StatusCode(500, new { message = "Erro ao listar eleições" });
            }
        }

        /// <summary>
        /// Obtém eleição por ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterEleicao(int id)
        {
            try
            {
                var eleicao = await _eleicaoService.ObterEleicaoPorIdAsync(id);
                return Ok(eleicao);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter eleição {Id}", id);
                return StatusCode(500, new { message = "Erro ao obter eleição" });
            }
        }

        /// <summary>
        /// Cria uma nova eleição
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ComissaoEleitoral")]
        public async Task<IActionResult> CriarEleicao([FromBody] CriarEleicaoDTO dto)
        {
            try
            {
                var eleicao = await _eleicaoService.CriarEleicaoAsync(dto);
                return CreatedAtAction(nameof(ObterEleicao), new { id = eleicao.Id }, eleicao);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar eleição");
                return StatusCode(500, new { message = "Erro ao criar eleição" });
            }
        }

        /// <summary>
        /// Atualiza uma eleição
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,ComissaoEleitoral")]
        public async Task<IActionResult> AtualizarEleicao(int id, [FromBody] AtualizarEleicaoDTO dto)
        {
            try
            {
                var eleicao = await _eleicaoService.AtualizarEleicaoAsync(id, dto);
                return Ok(eleicao);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar eleição {Id}", id);
                return StatusCode(500, new { message = "Erro ao atualizar eleição" });
            }
        }

        /// <summary>
        /// Exclui uma eleição
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExcluirEleicao(int id)
        {
            try
            {
                // TODO: Obter usuário autenticado
                var usuarioId = 1;
                
                await _eleicaoService.ExcluirEleicaoAsync(id, usuarioId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir eleição {Id}", id);
                return StatusCode(500, new { message = "Erro ao excluir eleição" });
            }
        }

        /// <summary>
        /// Altera situação da eleição
        /// </summary>
        [HttpPut("{id}/situacao")]
        [Authorize(Roles = "Admin,ComissaoEleitoral")]
        public async Task<IActionResult> AlterarSituacao(int id, [FromBody] AlterarSituacaoEleicaoDTO dto)
        {
            try
            {
                var eleicao = await _eleicaoService.AlterarSituacaoEleicaoAsync(id, dto);
                return Ok(eleicao);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar situação da eleição {Id}", id);
                return StatusCode(500, new { message = "Erro ao alterar situação" });
            }
        }

        /// <summary>
        /// Obtém estatísticas da eleição
        /// </summary>
        [HttpGet("{id}/estatisticas")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterEstatisticas(int id)
        {
            try
            {
                var estatisticas = await _eleicaoService.ObterEstatisticasEleicaoAsync(id);
                return Ok(estatisticas);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas da eleição {Id}", id);
                return StatusCode(500, new { message = "Erro ao obter estatísticas" });
            }
        }

        /// <summary>
        /// Obtém anos com eleições
        /// </summary>
        [HttpGet("anos")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterAnosComEleicoes()
        {
            try
            {
                var anos = await _eleicaoService.ObterAnosComEleicoesAsync();
                return Ok(anos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter anos com eleições");
                return StatusCode(500, new { message = "Erro ao obter anos" });
            }
        }

        /// <summary>
        /// Obtém tipos de processo
        /// </summary>
        [HttpGet("tipos-processo")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterTiposProcesso()
        {
            try
            {
                var tipos = await _eleicaoService.ObterTiposProcessoAsync();
                return Ok(tipos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter tipos de processo");
                return StatusCode(500, new { message = "Erro ao obter tipos de processo" });
            }
        }
    }
}