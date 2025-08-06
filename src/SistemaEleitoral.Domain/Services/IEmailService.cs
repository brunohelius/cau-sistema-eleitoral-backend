using SistemaEleitoral.Domain.Models;

namespace SistemaEleitoral.Domain.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, string? cc = null, string? bcc = null);
    Task<bool> SendTemplateEmailAsync(string templateName, object model, string to, string? cc = null, string? bcc = null);
    Task<bool> QueueEmailAsync(EmailMessage message);
    Task<bool> QueueEmailsAsync(List<EmailMessage> messages);
    Task<bool> SendBulkEmailAsync(List<string> recipients, string templateName, object model);
    Task<EmailTemplate?> GetTemplateAsync(string name);
    Task<List<EmailLog>> GetEmailLogsAsync(int? userId = null, DateTime? from = null, DateTime? to = null);
}

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public string? TemplateName { get; set; }
    public object? TemplateModel { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public List<EmailAttachment>? Attachments { get; set; }
    public int Priority { get; set; } = 1; // 1=Normal, 2=High, 0=Low
    public DateTime? ScheduledFor { get; set; }
    public int? UserId { get; set; }
    public string? JobType { get; set; }
    public string? ReferenceId { get; set; }
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}