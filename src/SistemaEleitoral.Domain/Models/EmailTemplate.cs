using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Domain.Models;

public class EmailTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public string? Variables { get; set; } // JSON com variáveis disponíveis
    public string Category { get; set; } = "General";
    public int UfId { get; set; }
}

public class EmailLog : BaseEntity
{
    public string To { get; set; } = string.Empty;
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Sent, Failed, Queued
    public string? ErrorMessage { get; set; }
    public DateTime? SentAt { get; set; }
    public int? Attempts { get; set; } = 0;
    public string? JobType { get; set; }
    public string? ReferenceId { get; set; }
    public int? UserId { get; set; }
    public string? EmailProvider { get; set; }
    public string? MessageId { get; set; }
}

public class EmailConfiguration : BaseEntity
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSSL { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int UfId { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxAttemptsPerDay { get; set; } = 1000;
    public int MaxAttemptsPerHour { get; set; } = 100;
}