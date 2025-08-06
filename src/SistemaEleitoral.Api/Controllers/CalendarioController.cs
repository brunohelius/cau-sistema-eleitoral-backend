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
    /// Controller responsável pela gestão de calendários eleitorais
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CalendarioController : ControllerBase
    {
        private readonly ICalendarioService _calendarioService;
        private readonly ILogger<CalendarioController> _logger;

        public CalendarioController(
            ICalendarioService calendarioService,
            ILogger<CalendarioController> logger)
        {
            _calendarioService = calendarioService;
            _logger = logger;
        }

        #region Consultas Públicas

        /// <summary>
        /// Obtém os anos que possuem calendários eleitorais
        /// </summary>
        [HttpGet("anos")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterAnos()
        {
            try
            {
                var anos = await _calendarioService.ObterAnosComCalendariosAsync();
                return Ok(new { success = true, data = anos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter anos com calendários");
                return StatusCode(500, new { success = false, message = "Erro ao obter anos" });
            }
        }

        /// <summary>
        /// Obtém os anos com calendários concluídos
        /// </summary>
        [HttpGet("anos-concluidos")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterAnosConcluidos()
        {
            try
            {
                var anos = await _calendarioService.ObterAnosCalendariosConcluidosAsync();
                return Ok(new { success = true, data = anos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter anos concluídos");
                return StatusCode(500, new { success = false, message = "Erro ao obter anos concluídos" });
            }
        }

        /// <summary>
        /// Obtém calendário por ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPorId(int id)
        {
            try
            {
                var calendario = await _calendarioService.ObterPorIdAsync(id);
                if (calendario == null)
                    return NotFound(new { success = false, message = "Calendário não encontrado" });

                return Ok(new { success = true, data = calendario });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter calendário {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Obtém o período atual do calendário
        /// </summary>
        [HttpGet("{id}/periodo-atual")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterPeriodoAtual(int id)
        {
            try
            {
                var periodo = await _calendarioService.ObterPeriodoAtualAsync(id);
                return Ok(new { success = true, data = periodo });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter período atual do calendário {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Validações Temporais

        /// <summary>
        /// Valida se uma ação pode ser executada no período atual
        /// </summary>
        [HttpGet("{id}/validar-periodo/{tipoAtividade}")]
        public async Task<IActionResult> ValidarPeriodo(int id, string tipoAtividade)
        {
            try
            {
                if (!Enum.TryParse<TipoAtividadeCalendario>(tipoAtividade, out var tipo))
                    return BadRequest(new { success = false, message = "Tipo de atividade inválido" });

                var valido = await _calendarioService.ValidarPeriodoParaAcaoAsync(id, tipo);
                return Ok(new { 
                    success = true, 
                    periodoValido = valido,
                    tipoAtividade = tipoAtividade
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao validar período para {tipoAtividade}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Valida período para uma UF específica
        /// </summary>
        [HttpGet("{id}/validar-periodo-uf")]
        public async Task<IActionResult> ValidarPeriodoUF(
            int id, 
            [FromQuery] string uf, 
            [FromQuery] string tipoAtividade)
        {
            try
            {
                if (!Enum.TryParse<TipoAtividadeCalendario>(tipoAtividade, out var tipo))
                    return BadRequest(new { success = false, message = "Tipo de atividade inválido" });

                var valido = await _calendarioService.ValidarPeriodoUFAsync(id, uf, tipo);
                return Ok(new { 
                    success = true, 
                    periodoValido = valido,
                    uf = uf,
                    tipoAtividade = tipoAtividade
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao validar período para UF {uf}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Gestão de Calendários (Admin)

        /// <summary>
        /// Cria um novo calendário eleitoral
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,ComissaoEleitoral")]
        public async Task<IActionResult> CriarCalendario([FromBody] CriarCalendarioDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Adicionar usuário logado ao DTO
                dto.UsuarioCriacaoId = GetUsuarioLogadoId();

                var calendario = await _calendarioService.CriarCalendarioAsync(dto);
                
                _logger.LogInformation($"Calendário {calendario.Id} criado pelo usuário {dto.UsuarioCriacaoId}");
                
                return CreatedAtAction(
                    nameof(ObterPorId), 
                    new { id = calendario.Id }, 
                    new { success = true, data = calendario });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar calendário");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Duplica um calendário existente para um novo ano
        /// </summary>
        [HttpPost("{id}/duplicar")]
        [Authorize(Roles = "Admin,ComissaoEleitoral")]
        public async Task<IActionResult> DuplicarCalendario(int id, [FromBody] DuplicarCalendarioDTO dto)
        {
            try
            {
                var usuarioId = GetUsuarioLogadoId();
                var novoCalendario = await _calendarioService.DuplicarCalendarioAsync(
                    id, 
                    dto.NovoAno, 
                    usuarioId);
                
                _logger.LogInformation($"Calendário {id} duplicado para o ano {dto.NovoAno}");
                
                return Ok(new { success = true, data = novoCalendario });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao duplicar calendário {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Altera a situação do calendário
        /// </summary>
        [HttpPut("{id}/situacao")]
        [Authorize(Roles = "Admin,ComissaoEleitoral")]
        public async Task<IActionResult> AlterarSituacao(int id, [FromBody] AlterarSituacaoDTO dto)
        {
            try
            {
                if (!Enum.TryParse<SituacaoCalendario>(dto.NovaSituacao, out var situacao))
                    return BadRequest(new { success = false, message = "Situação inválida" });

                var usuarioId = GetUsuarioLogadoId();
                var sucesso = await _calendarioService.AlterarSituacaoAsync(id, situacao, usuarioId);
                
                if (sucesso)
                {
                    _logger.LogInformation($"Situação do calendário {id} alterada para {situacao}");
                    return Ok(new { success = true, message = "Situação alterada com sucesso" });
                }
                
                return BadRequest(new { success = false, message = "Não foi possível alterar a situação" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao alterar situação do calendário {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Atividades do Calendário

        /// <summary>
        /// Obtém as atividades principais do calendário
        /// </summary>
        [HttpGet("{id}/atividades-principais")]
        [AllowAnonymous]
        public async Task<IActionResult> ObterAtividadesPrincipais(int id)
        {
            try
            {
                var calendario = await _calendarioService.ObterPorIdAsync(id);
                if (calendario == null)
                    return NotFound(new { success = false, message = "Calendário não encontrado" });

                return Ok(new { 
                    success = true, 
                    data = calendario.AtividadesPrincipais 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter atividades do calendário {id}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Métodos Auxiliares

        private int GetUsuarioLogadoId()
        {
            // Implementar lógica para obter ID do usuário do token JWT
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return 0;
        }

        #endregion
    }

    #region DTOs Adicionais

    public class DuplicarCalendarioDTO
    {
        public int NovoAno { get; set; }
    }

    public class AlterarSituacaoDTO
    {
        public string NovaSituacao { get; set; }
        public string Observacao { get; set; }
    }

    #endregion
}