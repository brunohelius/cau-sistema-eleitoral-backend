using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Models;
using SistemaEleitoral.Domain.Services;
using System.Text.Json;
using System.Text.RegularExpressions;
using Scriban;

namespace SistemaEleitoral.Infrastructure.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IEmailTemplateRepository _repository;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(
        IEmailTemplateRepository repository,
        ILogger<EmailTemplateService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<EmailTemplate?> GetTemplateAsync(string name)
    {
        try
        {
            return await _repository.GetByNameAsync(name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar template {TemplateName}", name);
            return null;
        }
    }

    public async Task<List<EmailTemplate>> GetAllTemplatesAsync()
    {
        try
        {
            return await _repository.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar todos os templates");
            return new List<EmailTemplate>();
        }
    }

    public async Task<List<EmailTemplate>> GetTemplatesByCategoryAsync(string category)
    {
        try
        {
            return await _repository.GetByCategoryAsync(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar templates da categoria {Category}", category);
            return new List<EmailTemplate>();
        }
    }

    public async Task<EmailTemplate> CreateTemplateAsync(EmailTemplate template)
    {
        try
        {
            // Validar se já existe
            if (await _repository.ExistsAsync(template.Name))
            {
                throw new InvalidOperationException($"Template com nome '{template.Name}' já existe");
            }

            // Validar sintaxe do template
            if (!await ValidateTemplateAsync(template.HtmlBody))
            {
                throw new InvalidOperationException("Template possui sintaxe inválida");
            }

            // Extrair e salvar variáveis do template
            template.Variables = JsonSerializer.Serialize(await ExtractVariablesAsync(template.HtmlBody));

            template.CreatedAt = DateTime.Now;
            template.UpdatedAt = DateTime.Now;

            return await _repository.AddAsync(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar template {TemplateName}", template.Name);
            throw;
        }
    }

    public async Task<EmailTemplate> UpdateTemplateAsync(EmailTemplate template)
    {
        try
        {
            // Validar sintaxe do template
            if (!await ValidateTemplateAsync(template.HtmlBody))
            {
                throw new InvalidOperationException("Template possui sintaxe inválida");
            }

            // Atualizar variáveis do template
            template.Variables = JsonSerializer.Serialize(await ExtractVariablesAsync(template.HtmlBody));
            template.UpdatedAt = DateTime.Now;

            return await _repository.UpdateAsync(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar template {TemplateName}", template.Name);
            throw;
        }
    }

    public async Task<bool> DeleteTemplateAsync(int templateId)
    {
        try
        {
            return await _repository.DeleteAsync(templateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao deletar template {TemplateId}", templateId);
            return false;
        }
    }

    public async Task<string> RenderTemplateAsync(string templateContent, object model)
    {
        try
        {
            var template = Template.Parse(templateContent);
            var result = await template.RenderAsync(model);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao renderizar template");
            throw new InvalidOperationException($"Erro ao renderizar template: {ex.Message}", ex);
        }
    }

    public async Task<bool> ValidateTemplateAsync(string templateContent)
    {
        try
        {
            var template = Template.Parse(templateContent);
            
            // Verificar se há erros de parsing
            if (template.HasErrors)
            {
                var errors = string.Join(", ", template.Messages.Select(m => m.Message));
                _logger.LogWarning("Template possui erros de sintaxe: {Errors}", errors);
                return false;
            }

            // Testar renderização com modelo vazio
            var testModel = new Dictionary<string, object>();
            await template.RenderAsync(testModel);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Template falhou na validação");
            return false;
        }
    }

    public async Task<List<string>> GetTemplateVariablesAsync(string templateName)
    {
        try
        {
            var template = await GetTemplateAsync(templateName);
            if (template == null || string.IsNullOrEmpty(template.Variables))
            {
                return new List<string>();
            }

            return JsonSerializer.Deserialize<List<string>>(template.Variables) ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter variáveis do template {TemplateName}", templateName);
            return new List<string>();
        }
    }

    private async Task<List<string>> ExtractVariablesAsync(string templateContent)
    {
        try
        {
            var variables = new HashSet<string>();
            
            // Regex para encontrar variáveis do tipo {{variavel}} ou {{ variavel }}
            var regex = new Regex(@"{{\s*([a-zA-Z_][a-zA-Z0-9_\.]*)\s*}}", RegexOptions.IgnoreCase);
            var matches = regex.Matches(templateContent);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    variables.Add(match.Groups[1].Value.Trim());
                }
            }

            return variables.OrderBy(v => v).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao extrair variáveis do template");
            return new List<string>();
        }
    }
    
    // Método adicional para compatibilidade com a interface
    public async Task<string> ProcessTemplateAsync(string templateName, object data)
    {
        var template = await GetTemplateAsync(templateName);
        if (template == null)
        {
            _logger.LogWarning("Template {TemplateName} não encontrado", templateName);
            return string.Empty;
        }
        
        var renderedContent = await RenderTemplateAsync(template.Content, data);
        return renderedContent ?? string.Empty;
    }
}