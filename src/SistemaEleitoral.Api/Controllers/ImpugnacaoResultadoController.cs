using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Application.DTOs.ImpugnacaoResultado;
using SistemaEleitoral.Domain.Interfaces.Services;

namespace SistemaEleitoral.API.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de impugnações de resultado eleitoral
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ImpugnacaoResultadoController : ControllerBase
    {
        private readonly IImpugnacaoResultadoService _service;
        private readonly ILogger<ImpugnacaoResultadoController> _logger;

        public ImpugnacaoResultadoController(
            IImpugnacaoResultadoService service,
            ILogger<ImpugnacaoResultadoController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// Registra uma nova impugnação de resultado
        /// </summary>
        /// <param name="dto">Dados da impugnação</param>
        /// <returns>Impugnação registrada</returns>
        [HttpPost]
        [ProducesResponseType(typeof(ImpugnacaoResultadoDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ImpugnacaoResultadoDTO>> RegistrarImpugnacao(
            [FromBody] RegistrarImpugnacaoResultadoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _service.RegistrarImpugnacaoAsync(dto);
                return CreatedAtAction(nameof(ObterPorId), new { id = resultado.Id }, resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao registrar impugnação");
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar impugnação de resultado");
                return StatusCode(500, new { erro = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Adiciona alegação à impugnação
        /// </summary>
        /// <param name="dto">Dados da alegação</param>
        /// <returns>Alegação adicionada</returns>
        [HttpPost("alegacao")]
        [ProducesResponseType(typeof(AlegacaoImpugnacaoResultadoDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<AlegacaoImpugnacaoResultadoDTO>> AdicionarAlegacao(
            [FromBody] AdicionarAlegacaoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _service.AdicionarAlegacaoAsync(dto);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao adicionar alegação");
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar alegação");
                return StatusCode(500, new { erro = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Adiciona recurso à impugnação
        /// </summary>
        /// <param name="dto">Dados do recurso</param>
        /// <returns>Recurso adicionado</returns>
        [HttpPost("recurso")]
        [ProducesResponseType(typeof(RecursoImpugnacaoResultadoDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<RecursoImpugnacaoResultadoDTO>> AdicionarRecurso(
            [FromBody] AdicionarRecursoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _service.AdicionarRecursoAsync(dto);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao adicionar recurso");
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar recurso");
                return StatusCode(500, new { erro = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Adiciona contrarrazão ao recurso
        /// </summary>
        /// <param name="dto">Dados da contrarrazão</param>
        /// <returns>Contrarrazão adicionada</returns>
        [HttpPost("contrarrazao")]
        [ProducesResponseType(typeof(ContrarrazaoImpugnacaoResultadoDTO), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ContrarrazaoImpugnacaoResultadoDTO>> AdicionarContrarrazao(
            [FromBody] AdicionarContrarrazaoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _service.AdicionarContrarrazaoAsync(dto);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao adicionar contrarrazão");
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar contrarrazão");
                return StatusCode(500, new { erro = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Julga alegação de impugnação
        /// </summary>
        /// <param name="dto">Dados do julgamento</param>
        /// <returns>Julgamento registrado</returns>
        [HttpPost("julgar-alegacao")]
        [Authorize(Roles = "ComissaoEleitoral,Administrador")]
        [ProducesResponseType(typeof(JulgamentoAlegacaoImpugResultadoDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<JulgamentoAlegacaoImpugResultadoDTO>> JulgarAlegacao(
            [FromBody] JulgarAlegacaoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _service.JulgarAlegacaoAsync(dto);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao julgar alegação");
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao julgar alegação");
                return StatusCode(500, new { erro = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Julga recurso de impugnação
        /// </summary>
        /// <param name="dto">Dados do julgamento</param>
        /// <returns>Julgamento registrado</returns>
        [HttpPost("julgar-recurso")]
        [Authorize(Roles = "ComissaoEleitoral,Administrador")]
        [ProducesResponseType(typeof(JulgamentoRecursoImpugResultadoDTO), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<JulgamentoRecursoImpugResultadoDTO>> JulgarRecurso(
            [FromBody] JulgarRecursoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _service.JulgarRecursoAsync(dto);
                return Ok(resultado);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao julgar recurso");
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao julgar recurso");
                return StatusCode(500, new { erro = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Obtém impugnação por ID
        /// </summary>
        /// <param name="id">ID da impugnação</param>
        /// <returns>Detalhes da impugnação</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ImpugnacaoResultadoDetalheDTO), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<ImpugnacaoResultadoDetalheDTO>> ObterPorId(int id)
        {
            try
            {
                var impugnacao = await _service.ObterPorIdAsync(id);
                if (impugnacao == null)
                    return NotFound(new { erro = "Impugnação não encontrada" });

                return Ok(impugnacao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter impugnação {Id}", id);
                return StatusCode(500, new { erro = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Lista impugnações por calendário
        /// </summary>
        /// <param name="calendarioId">ID do calendário</param>
        /// <returns>Lista de impugnações</returns>
        [HttpGet("calendario/{calendarioId}")]
        [ProducesResponseType(typeof(IEnumerable<ImpugnacaoResultadoDTO>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<ImpugnacaoResultadoDTO>>> ListarPorCalendario(int calendarioId)
        {
            try
            {
                var impugnacoes = await _service.ListarPorCalendarioAsync(calendarioId);
                return Ok(impugnacoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar impugnações do calendário {CalendarioId}", calendarioId);
                return StatusCode(500, new { erro = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Lista impugnações por profissional
        /// </summary>
        /// <param name="profissionalId">ID do profissional</param>
        /// <returns>Lista de impugnações</returns>
        [HttpGet("profissional/{profissionalId}")]
        [ProducesResponseType(typeof(IEnumerable<ImpugnacaoResultadoDTO>), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<IEnumerable<ImpugnacaoResultadoDTO>>> ListarPorProfissional(int profissionalId)
        {
            try
            {
                var impugnacoes = await _service.ListarPorProfissionalAsync(profissionalId);
                return Ok(impugnacoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar impugnações do profissional {ProfissionalId}", profissionalId);
                return StatusCode(500, new { erro = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Lista impugnações pendentes de julgamento
        /// </summary>
        /// <returns>Lista de impugnações pendentes</returns>
        [HttpGet("pendentes-julgamento")]
        [Authorize(Roles = "ComissaoEleitoral,Administrador")]
        [ProducesResponseType(typeof(IEnumerable<ImpugnacaoResultadoDTO>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult<IEnumerable<ImpugnacaoResultadoDTO>>> ListarPendentesJulgamento()
        {
            try
            {
                var impugnacoes = await _service.ListarPendentesJulgamentoAsync();
                return Ok(impugnacoes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar impugnações pendentes de julgamento");
                return StatusCode(500, new { erro = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Faz upload de arquivo para impugnação
        /// </summary>
        /// <param name="arquivo">Arquivo a ser enviado</param>
        /// <returns>Informações do arquivo enviado</returns>
        [HttpPost("upload")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> UploadArquivo([FromForm] IFormFile arquivo)
        {
            try
            {
                if (arquivo == null || arquivo.Length == 0)
                    return BadRequest(new { erro = "Arquivo inválido" });

                // Validar tipo de arquivo
                var extensoesPermitidas = new[] { ".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png" };
                var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
                
                if (!extensoesPermitidas.Contains(extensao))
                    return BadRequest(new { erro = "Tipo de arquivo não permitido" });

                // Validar tamanho (máximo 10MB)
                if (arquivo.Length > 10 * 1024 * 1024)
                    return BadRequest(new { erro = "Arquivo muito grande (máximo 10MB)" });

                // Gerar nome único para o arquivo
                var nomeArquivoFisico = $"{Guid.NewGuid()}{extensao}";
                var caminhoDestino = Path.Combine("uploads", "impugnacoes", nomeArquivoFisico);

                // Criar diretório se não existir
                var diretorio = Path.GetDirectoryName(caminhoDestino);
                if (!string.IsNullOrEmpty(diretorio))
                    Directory.CreateDirectory(diretorio);

                // Salvar arquivo
                using (var stream = new FileStream(caminhoDestino, FileMode.Create))
                {
                    await arquivo.CopyToAsync(stream);
                }

                return Ok(new
                {
                    nomeArquivo = arquivo.FileName,
                    nomeArquivoFisico = nomeArquivoFisico,
                    tamanho = arquivo.Length,
                    tipo = arquivo.ContentType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload de arquivo");
                return StatusCode(500, new { erro = "Erro ao processar arquivo" });
            }
        }

        /// <summary>
        /// Faz download de arquivo de impugnação
        /// </summary>
        /// <param name="nomeArquivoFisico">Nome físico do arquivo</param>
        /// <returns>Arquivo para download</returns>
        [HttpGet("download/{nomeArquivoFisico}")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> DownloadArquivo(string nomeArquivoFisico)
        {
            try
            {
                var caminhoArquivo = Path.Combine("uploads", "impugnacoes", nomeArquivoFisico);
                
                if (!System.IO.File.Exists(caminhoArquivo))
                    return NotFound(new { erro = "Arquivo não encontrado" });

                var bytes = await System.IO.File.ReadAllBytesAsync(caminhoArquivo);
                var contentType = "application/octet-stream";
                
                return File(bytes, contentType, nomeArquivoFisico);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer download de arquivo");
                return StatusCode(500, new { erro = "Erro ao processar arquivo" });
            }
        }
    }
}