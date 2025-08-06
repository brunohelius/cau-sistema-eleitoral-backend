using SistemaEleitoral.Domain.Models;

namespace SistemaEleitoral.Domain.Services;

public interface IEmailTemplateService
{
    Task<EmailTemplate?> GetTemplateAsync(string name);
    Task<List<EmailTemplate>> GetAllTemplatesAsync();
    Task<List<EmailTemplate>> GetTemplatesByCategoryAsync(string category);
    Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template);
    Task<EmailTemplate> UpdateTemplateAsync(EmailTemplate template);
    Task<bool> DeleteTemplateAsync(int templateId);
    Task<string> RenderTemplateAsync(string templateContent, object model);
    Task<bool> ValidateTemplateAsync(string templateContent);
    Task<List<string>> GetTemplateVariablesAsync(string templateName);
}

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetByNameAsync(string name);
    Task<List<EmailTemplate>> GetAllAsync();
    Task<List<EmailTemplate>> GetByCategoryAsync(string category);
    Task<List<EmailTemplate>> GetByUfIdAsync(int ufId);
    Task<EmailTemplate> AddAsync(EmailTemplate template);
    Task<EmailTemplate> UpdateAsync(EmailTemplate template);
    Task<bool> DeleteAsync(int templateId);
    Task<bool> ExistsAsync(string name);
}

public interface IEmailLogRepository
{
    Task<EmailLog> AddAsync(EmailLog emailLog);
    Task<EmailLog> UpdateAsync(EmailLog emailLog);
    Task<List<EmailLog>> GetLogsAsync(int? userId = null, DateTime? from = null, DateTime? to = null);
    Task<List<EmailLog>> GetFailedEmailsAsync();
    Task<EmailLogStats> GetStatsAsync(DateTime from, DateTime to);
}

public class EmailLogStats
{
    public int TotalEmails { get; set; }
    public int EmailsEnviados { get; set; }
    public int EmailsFalharam { get; set; }
    public int EmailsPendentes { get; set; }
    public double TaxaSucesso => TotalEmails > 0 ? (double)EmailsEnviados / TotalEmails * 100 : 0;
    public Dictionary<string, int> EmailsPorTemplate { get; set; } = new();
    public Dictionary<DateTime, int> EmailsPorDia { get; set; } = new();
}