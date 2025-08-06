using SistemaEleitoral.Domain.Models;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Interfaces.Repositories;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Infrastructure.Templates;

public static class EmailTemplates
{
    public static readonly List<EmailTemplate> DefaultTemplates = new()
    {
        new EmailTemplate
        {
            Name = "ConviteMembroChapa",
            DisplayName = "Convite para Participação em Chapa",
            Category = "Chapa",
            Subject = "Convite para participar da chapa {{ NomeChapa }} - {{ UF }} {{ Ano }}",
            HtmlBody = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Convite para Chapa</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #2c5282; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; border: 1px solid #ddd; }
        .buttons { text-align: center; margin: 20px 0; }
        .btn { display: inline-block; padding: 12px 24px; margin: 0 10px; text-decoration: none; border-radius: 5px; }
        .btn-accept { background-color: #28a745; color: white; }
        .btn-reject { background-color: #dc3545; color: white; }
        .footer { margin-top: 20px; padding: 10px; background-color: #f8f9fa; text-align: center; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Convite para Participação em Chapa</h1>
        </div>
        
        <div class=""content"">
            <p>Prezado(a) <strong>{{ NomeMembro }}</strong>,</p>
            
            <p>Você foi convidado(a) para participar da chapa <strong>""{{ NomeChapa }}""</strong> para as eleições do CAU/{{ UF }} {{ Ano }}.</p>
            
            <p><strong>Responsável pela chapa:</strong> {{ NomeResponsavel }}<br>
            <strong>Email:</strong> {{ EmailResponsavel }}</p>
            
            <p>Para confirmar ou rejeitar sua participação, clique em um dos botões abaixo:</p>
            
            <div class=""buttons"">
                <a href=""{{ LinkConfirmacao }}"" class=""btn btn-accept"">ACEITAR CONVITE</a>
                <a href=""{{ LinkRejeicao }}"" class=""btn btn-reject"">REJEITAR CONVITE</a>
            </div>
            
            <p><strong>Prazo para confirmação:</strong> {{ PrazoConfirmacao }}</p>
            
            <p>Caso tenha dúvidas, entre em contato com o responsável pela chapa ou com a Comissão Eleitoral.</p>
        </div>
        
        <div class=""footer"">
            <p>Este é um email automático do Sistema Eleitoral CAU/{{ UF }}.<br>
            Não responda a este email.</p>
        </div>
    </div>
</body>
</html>",
            IsActive = true,
            Variables = """[""NomeMembro"", ""NomeChapa"", ""NomeResponsavel"", ""EmailResponsavel"", ""LinkConfirmacao"", ""LinkRejeicao"", ""PrazoConfirmacao"", ""UF"", ""Ano""]"""
        },

        new EmailTemplate
        {
            Name = "JulgamentoFinal",
            DisplayName = "Notificação de Julgamento Final",
            Category = "Julgamento",
            Subject = "Julgamento Final - Processo {{ NumeroProcesso }}",
            HtmlBody = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Julgamento Final</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #8b5a2b; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; border: 1px solid #ddd; }
        .decision-box { background-color: #f8f9fa; border-left: 4px solid #8b5a2b; padding: 15px; margin: 15px 0; }
        .btn { display: inline-block; padding: 12px 24px; background-color: #8b5a2b; color: white; text-decoration: none; border-radius: 5px; margin: 10px 0; }
        .footer { margin-top: 20px; padding: 10px; background-color: #f8f9fa; text-align: center; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Julgamento Final - {{ TipoJulgamento }}</h1>
        </div>
        
        <div class=""content"">
            <h2>Processo {{ NumeroProcesso }}</h2>
            
            <div class=""decision-box"">
                <h3>Decisão:</h3>
                <p><strong>{{ Decisao }}</strong></p>
            </div>
            
            <p><strong>Data do Julgamento:</strong> {{ DataJulgamento | date: ""dd/MM/yyyy"" }}</p>
            <p><strong>Relator:</strong> {{ Relator }}</p>
            <p><strong>Instância:</strong> {{ Instancia }}</p>
            
            {{ if Resumo }}
            <h3>Resumo:</h3>
            <p>{{ Resumo }}</p>
            {{ end }}
            
            <p style=""text-align: center;"">
                <a href=""{{ LinkProcesso }}"" class=""btn"">VER PROCESSO COMPLETO</a>
            </p>
        </div>
        
        <div class=""footer"">
            <p>Sistema Eleitoral CAU/{{ UF }} {{ Ano }}<br>
            Este é um email automático. Não responda a este email.</p>
        </div>
    </div>
</body>
</html>",
            IsActive = true,
            Variables = """[""NumeroProcesso"", ""Decisao"", ""DataJulgamento"", ""Relator"", ""Resumo"", ""TipoJulgamento"", ""LinkProcesso"", ""Instancia"", ""UF"", ""Ano""]"""
        },

        new EmailTemplate
        {
            Name = "ExtratoTodosProfissionais",
            DisplayName = "Extrato de Todos os Profissionais",
            Category = "Relatório",
            Subject = "Extrato de Profissionais - {{ DataGeracao | date: \"dd/MM/yyyy\" }}",
            HtmlBody = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Extrato de Profissionais</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #17a2b8; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; border: 1px solid #ddd; }
        .stats { background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin: 15px 0; }
        .attachment { background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 5px; margin: 10px 0; }
        .footer { margin-top: 20px; padding: 10px; background-color: #f8f9fa; text-align: center; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Extrato de Todos os Profissionais</h1>
            <p>{{ UF }} - {{ DataGeracao | date: ""dd/MM/yyyy HH:mm"" }}</p>
        </div>
        
        <div class=""content"">
            <p>Prezado(a) <strong>{{ NomeSolicitante }}</strong>,</p>
            
            <p>Seu relatório foi gerado com sucesso e está disponível em anexo.</p>
            
            <div class=""stats"">
                <h3>Resumo do Relatório:</h3>
                <p><strong>Total de Profissionais:</strong> {{ TotalProfissionais }}</p>
                <p><strong>UF:</strong> {{ UF }}</p>
                <p><strong>Formato:</strong> {{ Formato }}</p>
                <p><strong>Data/Hora de Geração:</strong> {{ DataGeracao | date: ""dd/MM/yyyy HH:mm:ss"" }}</p>
            </div>
            
            <div class=""attachment"">
                <h4>📎 Arquivo em Anexo:</h4>
                <p><strong>{{ NomeArquivo }}</strong></p>
                <p><em>O arquivo foi anexado a este email e pode ser baixado.</em></p>
            </div>
            
            <p><strong>Observações importantes:</strong></p>
            <ul>
                <li>Este relatório contém informações sensíveis e deve ser tratado com confidencialidade</li>
                <li>O arquivo tem validade de 30 dias</li>
                <li>Em caso de dúvidas, entre em contato com o suporte</li>
            </ul>
        </div>
        
        <div class=""footer"">
            <p>Sistema Eleitoral CAU/{{ UF }}<br>
            Relatório gerado automaticamente em {{ DataGeracao | date: ""dd/MM/yyyy HH:mm"" }}</p>
        </div>
    </div>
</body>
</html>",
            IsActive = true,
            Variables = """[""NomeSolicitante"", ""DataGeracao"", ""TotalProfissionais"", ""UF"", ""Formato"", ""NomeArquivo""]"""
        },

        new EmailTemplate
        {
            Name = "PendenciasMembroChapa",
            DisplayName = "Alerta de Pendências de Membro",
            Category = "Chapa",
            Subject = "{{ TotalPendencias }} pendência(s) na chapa {{ NomeChapa }} - Ação Necessária",
            HtmlBody = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Pendências na Chapa</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #dc3545; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; border: 1px solid #ddd; }
        .pendencia { background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 10px 0; border-radius: 3px; }
        .pendencia.critica { background-color: #f8d7da; border-left-color: #dc3545; }
        .pendencia.urgente { background-color: #ffe6cc; border-left-color: #fd7e14; }
        .summary { background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin: 15px 0; }
        .btn { display: inline-block; padding: 12px 24px; background-color: #dc3545; color: white; text-decoration: none; border-radius: 5px; margin: 10px 5px; }
        .footer { margin-top: 20px; padding: 10px; background-color: #f8f9fa; text-align: center; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>⚠️ Pendências Encontradas</h1>
        </div>
        
        <div class=""content"">
            <p>Prezado(a) <strong>{{ NomeMembro }}</strong>,</p>
            
            <p>Foram identificadas pendências em sua participação na chapa <strong>""{{ NomeChapa }}""</strong>.</p>
            
            <div class=""summary"">
                <h3>Resumo das Pendências:</h3>
                <p><strong>Total de Pendências:</strong> {{ TotalPendencias }}</p>
                {{ if PendenciasCriticas > 0 }}
                <p><strong>Pendências Críticas:</strong> {{ PendenciasCriticas }} ⚠️</p>
                {{ end }}
                {{ if PendenciasUrgentes > 0 }}
                <p><strong>Pendências Urgentes:</strong> {{ PendenciasUrgentes }} 🕐</p>
                {{ end }}
            </div>
            
            <h3>Detalhes das Pendências:</h3>
            {{ for pendencia in Pendencias }}
            <div class=""pendencia {{ if pendencia.Criticidade == 'CRITICA' }}critica{{ else if pendencia.DiasPrazo <= 3 }}urgente{{ end }}"">
                <h4>{{ pendencia.Tipo }}</h4>
                <p>{{ pendencia.Descricao }}</p>
                {{ if pendencia.Prazo }}
                <p><strong>Prazo:</strong> {{ pendencia.Prazo | date: ""dd/MM/yyyy"" }}
                {{ if pendencia.DiasPrazo }}
                    ({{ if pendencia.DiasPrazo <= 0 }}VENCIDO{{ else }}{{ pendencia.DiasPrazo }} dias restantes{{ end }})
                {{ end }}
                </p>
                {{ end }}
                <p><strong>Status:</strong> {{ pendencia.Status }}</p>
            </div>
            {{ end }}
            
            <p style=""text-align: center;"">
                <a href=""{{ LinkPendencias }}"" class=""btn"">VER TODAS AS PENDÊNCIAS</a>
                <a href=""{{ LinkChapa }}"" class=""btn"" style=""background-color: #6c757d;"">VER CHAPA</a>
            </p>
            
            <p><strong>Atenção:</strong> Pendências não resolvidas podem impedir a confirmação da chapa. Entre em contato com o responsável {{ NomeResponsavel }} ({{ EmailResponsavel }}) se precisar de auxílio.</p>
        </div>
        
        <div class=""footer"">
            <p>Sistema Eleitoral CAU/{{ UF }} {{ Ano }}<br>
            Este é um email automático. Não responda a este email.</p>
        </div>
    </div>
</body>
</html>",
            IsActive = true,
            Variables = """[""NomeMembro"", ""NomeChapa"", ""NomeResponsavel"", ""EmailResponsavel"", ""TotalPendencias"", ""Pendencias"", ""PendenciasCriticas"", ""PendenciasUrgentes"", ""LinkChapa"", ""LinkPendencias"", ""UF"", ""Ano""]"""
        },

        new EmailTemplate
        {
            Name = "ChapaConfirmada",
            DisplayName = "Confirmação de Chapa",
            Category = "Chapa",
            Subject = "Chapa {{ NomeChapa }} confirmada com sucesso!",
            HtmlBody = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Chapa Confirmada</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background-color: #28a745; color: white; padding: 20px; text-align: center; }
        .content { padding: 20px; border: 1px solid #ddd; }
        .success-box { background-color: #d4edda; border: 1px solid #c3e6cb; color: #155724; padding: 15px; border-radius: 5px; margin: 15px 0; }
        .member-list { background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0; }
        .member { padding: 5px 0; border-bottom: 1px solid #dee2e6; }
        .member:last-child { border-bottom: none; }
        .btn { display: inline-block; padding: 12px 24px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; margin: 10px 5px; }
        .footer { margin-top: 20px; padding: 10px; background-color: #f8f9fa; text-align: center; font-size: 12px; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>✅ Chapa Confirmada!</h1>
        </div>
        
        <div class=""content"">
            <div class=""success-box"">
                <h2>Parabéns! Sua chapa foi confirmada com sucesso.</h2>
            </div>
            
            <p><strong>Nome da Chapa:</strong> {{ NomeChapa }}</p>
            <p><strong>Número:</strong> {{ NumeroChapa }}</p>
            <p><strong>Data de Confirmação:</strong> {{ DataConfirmacao | date: ""dd/MM/yyyy HH:mm"" }}</p>
            <p><strong>Status Anterior:</strong> {{ StatusAnterior }}</p>
            <p><strong>Status Atual:</strong> {{ StatusAtual }}</p>
            
            <div class=""member-list"">
                <h3>Membros da Chapa ({{ TotalMembros }}):</h3>
                {{ for membro in Membros }}
                {{ if membro.StatusParticipacao == 'CONFIRMADO' }}
                <div class=""member"">
                    <strong>{{ membro.Nome }}</strong> - {{ membro.Cargo }}<br>
                    <small>{{ membro.Email }} | CPF: {{ membro.CPF }}</small>
                </div>
                {{ end }}
                {{ end }}
            </div>
            
            {{ if Observacoes }}
            <div style=""background-color: #fff3cd; padding: 10px; border-radius: 5px; margin: 10px 0;"">
                <h4>Observações:</h4>
                <p>{{ Observacoes }}</p>
            </div>
            {{ end }}
            
            <p style=""text-align: center;"">
                <a href=""{{ LinkChapa }}"" class=""btn"">VER CHAPA</a>
                <a href=""{{ LinkCertificado }}"" class=""btn"" style=""background-color: #17a2b8;"">BAIXAR CERTIFICADO</a>
            </p>
            
            <p><strong>Próximos passos:</strong></p>
            <ul>
                <li>Aguardar o período de impugnações</li>
                <li>Acompanhar o calendário eleitoral</li>
                <li>Manter os dados atualizados</li>
            </ul>
        </div>
        
        <div class=""footer"">
            <p>Sistema Eleitoral CAU/{{ UF }} {{ Ano }}<br>
            Chapa confirmada em {{ DataConfirmacao | date: ""dd/MM/yyyy HH:mm"" }}</p>
        </div>
    </div>
</body>
</html>",
            IsActive = true,
            Variables = """[""NomeChapa"", ""NumeroChapa"", ""DataConfirmacao"", ""StatusAnterior"", ""StatusAtual"", ""TotalMembros"", ""Membros"", ""LinkChapa"", ""LinkCertificado"", ""UF"", ""Ano"", ""Observacoes""]"""
        }
    };
}

/// <summary>
/// Classe para seedar templates no banco de dados
/// </summary>
public class EmailTemplateSeeder
{
    private readonly IEmailTemplateRepository _repository;
    private readonly ILogger<EmailTemplateSeeder> _logger;

    public EmailTemplateSeeder(IEmailTemplateRepository repository, ILogger<EmailTemplateSeeder> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task SeedDefaultTemplatesAsync()
    {
        try
        {
            foreach (var template in EmailTemplates.DefaultTemplates)
            {
                var exists = await _repository.ExistsAsync(template.Name);
                if (!exists)
                {
                    await _repository.AddAsync(template);
                    _logger.LogInformation("Template '{TemplateName}' adicionado com sucesso", template.Name);
                }
                else
                {
                    _logger.LogInformation("Template '{TemplateName}' já existe, pulando", template.Name);
                }
            }

            _logger.LogInformation("Seed de templates concluído. {Count} templates processados", 
                EmailTemplates.DefaultTemplates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer seed dos templates de email");
            throw;
        }
    }
}