using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaEleitoral.Domain.Models;
using SistemaEleitoral.Domain.Services;
using SistemaEleitoral.Domain.Jobs.EmailJobs;

namespace SistemaEleitoral.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommunicationController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<CommunicationController> _logger;

    public CommunicationController(
        IEmailService emailService,
        INotificationService notificationService,
        IEmailTemplateService emailTemplateService,
        ILogger<CommunicationController> logger)
    {
        _emailService = emailService;
        _notificationService = notificationService;
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    #region Email Management

    [HttpPost("email/send")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest request)
    {
        try
        {
            var success = await _emailService.SendEmailAsync(
                request.To, 
                request.Subject, 
                request.Body, 
                request.Cc, 
                request.Bcc);

            return Ok(new { Success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email para {To}", request.To);
            return BadRequest(new { Error = "Erro ao enviar email", Details = ex.Message });
        }
    }

    [HttpPost("email/send-template")]
    public async Task<IActionResult> SendTemplateEmail([FromBody] SendTemplateEmailRequest request)
    {
        try
        {
            var success = await _emailService.SendTemplateEmailAsync(
                request.TemplateName,
                request.Model,
                request.To,
                request.Cc,
                request.Bcc);

            return Ok(new { Success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email com template {TemplateName} para {To}", 
                request.TemplateName, request.To);
            return BadRequest(new { Error = "Erro ao enviar email", Details = ex.Message });
        }
    }

    [HttpPost("email/queue")]
    public async Task<IActionResult> QueueEmail([FromBody] EmailMessage message)
    {
        try
        {
            var success = await _emailService.QueueEmailAsync(message);
            return Ok(new { Success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enfileirar email para {To}", message.To);
            return BadRequest(new { Error = "Erro ao enfileirar email", Details = ex.Message });
        }
    }

    [HttpPost("email/bulk")]
    public async Task<IActionResult> SendBulkEmail([FromBody] BulkEmailRequest request)
    {
        try
        {
            var success = await _emailService.SendBulkEmailAsync(
                request.Recipients,
                request.TemplateName,
                request.Model);

            return Ok(new { Success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar emails em lote");
            return BadRequest(new { Error = "Erro ao enviar emails em lote", Details = ex.Message });
        }
    }

    [HttpGet("email/logs")]
    public async Task<IActionResult> GetEmailLogs([FromQuery] EmailLogsFilter filter)
    {
        try
        {
            var logs = await _emailService.GetEmailLogsAsync(
                filter.UserId,
                filter.From,
                filter.To);

            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar logs de email");
            return BadRequest(new { Error = "Erro ao buscar logs", Details = ex.Message });
        }
    }

    #endregion

    #region Email Templates

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates([FromQuery] string? category = null)
    {
        try
        {
            var templates = string.IsNullOrEmpty(category) 
                ? await _emailTemplateService.GetAllTemplatesAsync()
                : await _emailTemplateService.GetTemplatesByCategoryAsync(category);

            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar templates");
            return BadRequest(new { Error = "Erro ao buscar templates", Details = ex.Message });
        }
    }

    [HttpGet("templates/{name}")]
    public async Task<IActionResult> GetTemplate(string name)
    {
        try
        {
            var template = await _emailTemplateService.GetTemplateAsync(name);
            if (template == null)
                return NotFound();

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar template {TemplateName}", name);
            return BadRequest(new { Error = "Erro ao buscar template", Details = ex.Message });
        }
    }

    [HttpPost("templates")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateTemplate([FromBody] EmailTemplate template)
    {
        try
        {
            var created = await _emailTemplateService.CreateTemplateAsync(template);
            return CreatedAtAction(nameof(GetTemplate), new { name = created.Name }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar template");
            return BadRequest(new { Error = "Erro ao criar template", Details = ex.Message });
        }
    }

    [HttpPut("templates/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTemplate(int id, [FromBody] EmailTemplate template)
    {
        try
        {
            template.Id = id;
            var updated = await _emailTemplateService.UpdateTemplateAsync(template);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar template {TemplateId}", id);
            return BadRequest(new { Error = "Erro ao atualizar template", Details = ex.Message });
        }
    }

    [HttpDelete("templates/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTemplate(int id)
    {
        try
        {
            var success = await _emailTemplateService.DeleteTemplateAsync(id);
            return Ok(new { Success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar template {TemplateId}", id);
            return BadRequest(new { Error = "Erro ao deletar template", Details = ex.Message });
        }
    }

    [HttpPost("templates/{name}/preview")]
    public async Task<IActionResult> PreviewTemplate(string name, [FromBody] object model)
    {
        try
        {
            var template = await _emailTemplateService.GetTemplateAsync(name);
            if (template == null)
                return NotFound();

            var renderedSubject = await _emailTemplateService.RenderTemplateAsync(template.Subject, model);
            var renderedBody = await _emailTemplateService.RenderTemplateAsync(template.HtmlBody, model);

            return Ok(new 
            { 
                Subject = renderedSubject, 
                HtmlBody = renderedBody,
                Template = template.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao visualizar template {TemplateName}", name);
            return BadRequest(new { Error = "Erro ao visualizar template", Details = ex.Message });
        }
    }

    #endregion

    #region Notifications

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications([FromQuery] bool unreadOnly = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, unreadOnly);
            return Ok(notifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar notificações");
            return BadRequest(new { Error = "Erro ao buscar notificações", Details = ex.Message });
        }
    }

    [HttpGet("notifications/unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { Count = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar quantidade de notificações não lidas");
            return BadRequest(new { Error = "Erro ao buscar contador", Details = ex.Message });
        }
    }

    [HttpPost("notifications/{id}/read")]
    public async Task<IActionResult> MarkNotificationAsRead(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _notificationService.MarkAsReadAsync(id, userId);
            return Ok(new { Success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar notificação como lida");
            return BadRequest(new { Error = "Erro ao marcar notificação", Details = ex.Message });
        }
    }

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsAsRead()
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { Success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao marcar todas as notificações como lidas");
            return BadRequest(new { Error = "Erro ao marcar notificações", Details = ex.Message });
        }
    }

    [HttpPost("notifications/send")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        try
        {
            if (request.UserId.HasValue)
            {
                await _notificationService.SendToUserAsync(request.UserId.Value, request.Message, request.Type);
            }
            else if (request.UserIds?.Any() == true)
            {
                await _notificationService.SendToUsersAsync(request.UserIds, request.Message, request.Type);
            }
            else if (!string.IsNullOrEmpty(request.GroupName))
            {
                await _notificationService.SendToGroupAsync(request.GroupName, request.Message, request.Type);
            }
            else
            {
                await _notificationService.SendToAllAsync(request.Message, request.Type);
            }

            return Ok(new { Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar notificação");
            return BadRequest(new { Error = "Erro ao enviar notificação", Details = ex.Message });
        }
    }

    #endregion

    #region Electoral Jobs

    [HttpPost("jobs/convite-membro-chapa")]
    [Authorize(Roles = "Admin,Responsavel")]
    public async Task<IActionResult> EnviarConviteMembroChapa([FromBody] ConviteMembroChapaRequest request)
    {
        try
        {
            var parameter = new Domain.Jobs.EmailJobParameter
            {
                Data = new Dictionary<string, object>
                {
                    ["chapaId"] = request.ChapaId,
                    ["membroId"] = request.MembroId,
                    ["conviteToken"] = request.ConviteToken,
                    ["baseUrl"] = request.BaseUrl,
                    ["prazoConfirmacao"] = request.PrazoConfirmacao
                },
                UserId = GetCurrentUserId()
            };

            BackgroundJob.Enqueue<EnviarEmailConviteMembroChapaJob>(job => job.ExecuteAsync(parameter));
            
            return Ok(new { Success = true, Message = "Job de convite enfileirado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enfileirar job de convite");
            return BadRequest(new { Error = "Erro ao enviar convite", Details = ex.Message });
        }
    }

    [HttpPost("jobs/julgamento-final")]
    [Authorize(Roles = "Admin,Julgador")]
    public async Task<IActionResult> EnviarJulgamentoFinal([FromBody] JulgamentoFinalRequest request)
    {
        try
        {
            var parameter = new Domain.Jobs.EmailJobParameter
            {
                Data = new Dictionary<string, object>
                {
                    ["julgamentoId"] = request.JulgamentoId,
                    ["tipo"] = request.Tipo,
                    ["baseUrl"] = request.BaseUrl
                },
                UserId = GetCurrentUserId()
            };

            BackgroundJob.Enqueue<EnviarEmailJulgamentoFinalJob>(job => job.ExecuteAsync(parameter));
            
            return Ok(new { Success = true, Message = "Job de julgamento enfileirado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enfileirar job de julgamento");
            return BadRequest(new { Error = "Erro ao enviar julgamento", Details = ex.Message });
        }
    }

    [HttpPost("jobs/extrato-profissionais")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GerarExtratoProfissionais([FromBody] ExtratoProfissionaisRequest request)
    {
        try
        {
            var parameter = new Domain.Jobs.EmailJobParameter
            {
                Data = new Dictionary<string, object>
                {
                    ["ufId"] = request.UfId,
                    ["solicitanteId"] = GetCurrentUserId(),
                    ["formato"] = request.Formato
                },
                UserId = GetCurrentUserId()
            };

            BackgroundJob.Enqueue<GerarExtratoTodosProfissionaisJob>(job => job.ExecuteAsync(parameter));
            
            return Ok(new { Success = true, Message = "Job de extrato enfileirado com sucesso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enfileirar job de extrato");
            return BadRequest(new { Error = "Erro ao gerar extrato", Details = ex.Message });
        }
    }

    #endregion

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("id")?.Value ?? User.FindFirst("sub")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}

#region Request DTOs

public class SendEmailRequest
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
}

public class SendTemplateEmailRequest
{
    public string To { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public object Model { get; set; } = new();
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
}

public class BulkEmailRequest
{
    public List<string> Recipients { get; set; } = new();
    public string TemplateName { get; set; } = string.Empty;
    public object Model { get; set; } = new();
}

public class EmailLogsFilter
{
    public int? UserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public class SendNotificationRequest
{
    public int? UserId { get; set; }
    public List<int>? UserIds { get; set; }
    public string? GroupName { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; } = NotificationType.Info;
}

public class ConviteMembroChapaRequest
{
    public int ChapaId { get; set; }
    public int MembroId { get; set; }
    public string ConviteToken { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string PrazoConfirmacao { get; set; } = string.Empty;
}

public class JulgamentoFinalRequest
{
    public int JulgamentoId { get; set; }
    public string Tipo { get; set; } = "FINAL";
    public string BaseUrl { get; set; } = string.Empty;
}

public class ExtratoProfissionaisRequest
{
    public int UfId { get; set; }
    public string Formato { get; set; } = "PDF";
}

#endregion