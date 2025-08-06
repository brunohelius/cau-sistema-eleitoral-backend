using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hangfire;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace SistemaEleitoral.Application.Jobs
{
    /// <summary>
    /// Job para enviar emails relacionados a denúncias
    /// </summary>
    public class EmailDenunciaRegistradaJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailDenunciaRegistradaJob> _logger;
        
        public EmailDenunciaRegistradaJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<EmailDenunciaRegistradaJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Envia email quando uma denúncia é registrada
        /// </summary>
        [Queue("emails")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10, 30, 60 })]
        public async Task EnviarEmailDenunciaRegistradaAsync(int denunciaId)
        {
            try
            {
                _logger.LogInformation($"Iniciando envio de email para denúncia {denunciaId}");

                var denuncia = await _context.Denuncias
                    .Include(d => d.Calendario)
                    .Include(d => d.Uf)
                    .Include(d => d.Denunciante)
                    .Include(d => d.DenunciaChapa)
                        .ThenInclude(dc => dc.Chapa)
                            .ThenInclude(c => c.Responsavel)
                    .Include(d => d.DenunciaMembroChapa)
                        .ThenInclude(dmc => dmc.MembroChapa)
                            .ThenInclude(mc => mc.Profissional)
                    .Include(d => d.DenunciaMembroComissao)
                        .ThenInclude(dmc => dmc.MembroComissao)
                            .ThenInclude(mc => mc.Profissional)
                    .FirstOrDefaultAsync(d => d.Id == denunciaId);

                if (denuncia == null)
                {
                    _logger.LogWarning($"Denúncia {denunciaId} não encontrada");
                    return;
                }

                // Enviar confirmação para o denunciante (se não for anônimo)
                if (!denuncia.DenuncianteAnonimo && denuncia.Denunciante != null)
                {
                    await EnviarConfirmacaoDenuncianteAsync(denuncia);
                }

                // Notificar denunciados
                await NotificarDenunciadosAsync(denuncia);

                // Notificar comissão eleitoral
                await NotificarComissaoEleitoralAsync(denuncia);

                _logger.LogInformation($"Emails enviados com sucesso para denúncia {denunciaId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar emails para denúncia {denunciaId}");
                throw;
            }
        }

        /// <summary>
        /// Envia email quando denúncia é admitida
        /// </summary>
        [Queue("emails")]
        public async Task EnviarEmailDenunciaAdmitidaAsync(int denunciaId)
        {
            try
            {
                var denuncia = await _context.Denuncias
                    .Include(d => d.Denunciante)
                    .Include(d => d.DenunciaAdmitida)
                    .Include(d => d.DenunciaChapa)
                        .ThenInclude(dc => dc.Chapa)
                    .FirstOrDefaultAsync(d => d.Id == denunciaId);

                if (denuncia?.DenunciaAdmitida == null)
                    return;

                var destinatarios = new List<string>();
                
                // Denunciante
                if (!denuncia.DenuncianteAnonimo && denuncia.Denunciante != null)
                {
                    destinatarios.Add(denuncia.Denunciante.Email);
                }

                // Denunciados
                if (denuncia.DenunciaChapa?.Chapa?.ResponsavelEmail != null)
                {
                    destinatarios.Add(denuncia.DenunciaChapa.Chapa.ResponsavelEmail);
                }

                if (destinatarios.Count == 0)
                    return;

                var parametros = new Dictionary<string, string>
                {
                    ["ProtocoloDenuncia"] = denuncia.ProtocoloNumero,
                    ["DataAdmissao"] = denuncia.DenunciaAdmitida.DataAdmissao.ToString("dd/MM/yyyy"),
                    ["PrazoDefesa"] = denuncia.DenunciaAdmitida.PrazoDefesa.ToString("dd/MM/yyyy"),
                    ["Parecer"] = denuncia.DenunciaAdmitida.Parecer,
                    ["LinkDefesa"] = $"/denuncias/{denuncia.Id}/defesa"
                };

                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = destinatarios,
                    Assunto = $"Denúncia {denuncia.ProtocoloNumero} - Admitida",
                    TemplateId = "DenunciaAdmitida",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Alta
                });

                _logger.LogInformation($"Email de admissão enviado para denúncia {denunciaId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de admissão para denúncia {denunciaId}");
                throw;
            }
        }

        /// <summary>
        /// Envia email quando defesa é apresentada
        /// </summary>
        [Queue("emails")]
        public async Task EnviarEmailDefesaApresentadaAsync(int denunciaId)
        {
            try
            {
                var denuncia = await _context.Denuncias
                    .Include(d => d.Denunciante)
                    .Include(d => d.Defesas)
                    .FirstOrDefaultAsync(d => d.Id == denunciaId);

                if (denuncia == null || !denuncia.Defesas.Any())
                    return;

                // Notificar denunciante sobre defesa apresentada
                if (!denuncia.DenuncianteAnonimo && denuncia.Denunciante != null)
                {
                    var defesa = denuncia.Defesas.OrderByDescending(d => d.DataApresentacao).First();

                    var parametros = new Dictionary<string, string>
                    {
                        ["ProtocoloDenuncia"] = denuncia.ProtocoloNumero,
                        ["DataDefesa"] = defesa.DataApresentacao.ToString("dd/MM/yyyy HH:mm"),
                        ["ProximaEtapa"] = "Análise e julgamento pela comissão eleitoral"
                    };

                    await _notificationService.EnviarEmailAsync(new EmailModel
                    {
                        Para = new List<string> { denuncia.Denunciante.Email },
                        Assunto = $"Defesa Apresentada - Denúncia {denuncia.ProtocoloNumero}",
                        TemplateId = "DefesaApresentada",
                        ParametrosTemplate = parametros,
                        Prioridade = EmailPrioridade.Normal
                    });
                }

                _logger.LogInformation($"Email de defesa enviado para denúncia {denunciaId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de defesa para denúncia {denunciaId}");
                throw;
            }
        }

        private async Task EnviarConfirmacaoDenuncianteAsync(Domain.Entities.Denuncia denuncia)
        {
            var parametros = new Dictionary<string, string>
            {
                ["NomeDenunciante"] = denuncia.Denunciante.Nome,
                ["ProtocoloDenuncia"] = denuncia.ProtocoloNumero,
                ["DataRegistro"] = denuncia.DataRegistro.ToString("dd/MM/yyyy HH:mm"),
                ["TipoDenuncia"] = denuncia.TipoDenuncia.ToString(),
                ["Descricao"] = denuncia.Descricao.Length > 200 ? 
                    denuncia.Descricao.Substring(0, 200) + "..." : denuncia.Descricao,
                ["LinkAcompanhamento"] = $"/denuncias/acompanhar/{denuncia.ProtocoloNumero}",
                ["ProximosPassos"] = "Sua denúncia será analisada pela comissão eleitoral"
            };

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = new List<string> { denuncia.Denunciante.Email },
                Assunto = $"Confirmação de Denúncia - Protocolo {denuncia.ProtocoloNumero}",
                TemplateId = "DenunciaRegistrada",
                ParametrosTemplate = parametros,
                Prioridade = EmailPrioridade.Alta
            });
        }

        private async Task NotificarDenunciadosAsync(Domain.Entities.Denuncia denuncia)
        {
            var destinatarios = new List<string>();
            string denunciadoNome = "";
            string denunciadoTipo = "";

            // Identificar denunciados
            if (denuncia.DenunciaChapa != null)
            {
                destinatarios.Add(denuncia.DenunciaChapa.Chapa.ResponsavelEmail);
                denunciadoNome = denuncia.DenunciaChapa.Chapa.Nome;
                denunciadoTipo = "Chapa";
            }
            else if (denuncia.DenunciaMembroChapa != null)
            {
                destinatarios.Add(denuncia.DenunciaMembroChapa.MembroChapa.Profissional.Email);
                denunciadoNome = denuncia.DenunciaMembroChapa.MembroChapa.Profissional.Nome;
                denunciadoTipo = "Membro de Chapa";
            }
            else if (denuncia.DenunciaMembroComissao != null)
            {
                destinatarios.Add(denuncia.DenunciaMembroComissao.MembroComissao.Profissional.Email);
                denunciadoNome = denuncia.DenunciaMembroComissao.MembroComissao.Profissional.Nome;
                denunciadoTipo = "Membro de Comissão";
            }

            if (destinatarios.Count == 0)
                return;

            var parametros = new Dictionary<string, string>
            {
                ["DenunciadoNome"] = denunciadoNome,
                ["DenunciadoTipo"] = denunciadoTipo,
                ["ProtocoloDenuncia"] = denuncia.ProtocoloNumero,
                ["DataRegistro"] = denuncia.DataRegistro.ToString("dd/MM/yyyy"),
                ["TipoDenuncia"] = denuncia.TipoDenuncia.ToString(),
                ["ProximaEtapa"] = "Aguardar análise da comissão eleitoral para admissibilidade",
                ["LinkAcompanhamento"] = $"/denuncias/{denuncia.Id}/acompanhar"
            };

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = destinatarios,
                Assunto = $"Notificação de Denúncia - Protocolo {denuncia.ProtocoloNumero}",
                TemplateId = "NotificacaoDenunciado",
                ParametrosTemplate = parametros,
                Prioridade = EmailPrioridade.Alta
            });
        }

        private async Task NotificarComissaoEleitoralAsync(Domain.Entities.Denuncia denuncia)
        {
            // Buscar membros da comissão eleitoral da UF
            var membrosComissao = await _context.MembrosComissao
                .Include(m => m.Profissional)
                .Include(m => m.Comissao)
                .Where(m => 
                    m.Comissao.UfId == denuncia.UfId &&
                    m.Comissao.CalendarioId == denuncia.CalendarioId &&
                    m.Ativo)
                .ToListAsync();

            if (!membrosComissao.Any())
                return;

            var destinatarios = membrosComissao
                .Where(m => m.Profissional != null)
                .Select(m => m.Profissional.Email)
                .Distinct()
                .ToList();

            var parametros = new Dictionary<string, string>
            {
                ["ProtocoloDenuncia"] = denuncia.ProtocoloNumero,
                ["DataRegistro"] = denuncia.DataRegistro.ToString("dd/MM/yyyy HH:mm"),
                ["TipoDenuncia"] = denuncia.TipoDenuncia.ToString(),
                ["UF"] = denuncia.Uf?.Nome ?? "",
                ["Urgente"] = denuncia.Urgente ? "SIM" : "NÃO",
                ["LinkAnalise"] = $"/comissao/denuncias/{denuncia.Id}/analisar",
                ["AcaoNecessaria"] = "Analisar admissibilidade da denúncia"
            };

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = destinatarios,
                Assunto = $"[COMISSÃO] Nova Denúncia para Análise - {denuncia.ProtocoloNumero}",
                TemplateId = "ComissaoDenunciaRecebida",
                ParametrosTemplate = parametros,
                Prioridade = denuncia.Urgente ? EmailPrioridade.Urgente : EmailPrioridade.Alta
            });
        }
    }
}