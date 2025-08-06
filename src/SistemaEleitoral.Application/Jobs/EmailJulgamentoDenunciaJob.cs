using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hangfire;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using SistemaEleitoral.Application.DTOs;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Application.Jobs
{
    /// <summary>
    /// Job para enviar email quando uma denúncia é julgada
    /// </summary>
    public class EmailJulgamentoDenunciaJob : IEmailJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailJulgamentoDenunciaJob> _logger;
        
        public EmailJulgamentoDenunciaJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<EmailJulgamentoDenunciaJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Envia email de julgamento de denúncia
        /// </summary>
        [Queue("emails")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10, 30, 60 })]
        public async Task ExecuteAsync(int denunciaId)
        {
            try
            {
                _logger.LogInformation($"Iniciando envio de email para julgamento de denúncia {denunciaId}");

                // Buscar denúncia com dados relacionados
                var denuncia = await _context.Denuncias
                    .Include(d => d.Denunciante)
                    .Include(d => d.Denunciado)
                    .Include(d => d.Chapa)
                        .ThenInclude(c => c.Responsavel)
                    .Include(d => d.Chapa)
                        .ThenInclude(c => c.Membros)
                            .ThenInclude(m => m.Profissional)
                    .Include(d => d.Relator)
                    .Include(d => d.Calendario)
                        .ThenInclude(c => c.Eleicao)
                    .FirstOrDefaultAsync(d => d.Id == denunciaId);

                if (denuncia == null)
                {
                    _logger.LogWarning($"Denúncia {denunciaId} não encontrada");
                    return;
                }

                var destinatarios = new List<string>();

                // Adicionar denunciante
                if (denuncia.Denunciante != null && !string.IsNullOrEmpty(denuncia.Denunciante.Email))
                {
                    destinatarios.Add(denuncia.Denunciante.Email);
                }

                // Adicionar denunciado
                if (denuncia.Denunciado != null && !string.IsNullOrEmpty(denuncia.Denunciado.Email))
                {
                    destinatarios.Add(denuncia.Denunciado.Email);
                }

                // Adicionar responsável pela chapa se houver
                if (denuncia.Chapa?.Responsavel != null && !string.IsNullOrEmpty(denuncia.Chapa.Responsavel.Email))
                {
                    destinatarios.Add(denuncia.Chapa.Responsavel.Email);
                }

                // Adicionar membros da chapa se houver
                if (denuncia.Chapa?.Membros != null)
                {
                    foreach (var membro in denuncia.Chapa.Membros.Where(m => m.Status == Domain.Enums.StatusMembroChapa.Confirmado))
                    {
                        if (membro.Profissional != null && !string.IsNullOrEmpty(membro.Profissional.Email))
                        {
                            destinatarios.Add(membro.Profissional.Email);
                        }
                    }
                }

                // Adicionar relator
                if (denuncia.Relator != null && !string.IsNullOrEmpty(denuncia.Relator.Email))
                {
                    destinatarios.Add(denuncia.Relator.Email);
                }

                // Adicionar membros da comissão eleitoral
                var comissaoEmails = await _context.MembrosComissao
                    .Include(m => m.Profissional)
                    .Where(m => m.Comissao.CalendarioId == denuncia.CalendarioId &&
                               m.Comissao.Ativo && 
                               m.Ativo)
                    .Select(m => m.Profissional.Email)
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToListAsync();

                destinatarios.AddRange(comissaoEmails);

                // Remover duplicatas
                destinatarios = destinatarios.Distinct().ToList();

                if (destinatarios.Count == 0)
                {
                    _logger.LogWarning($"Nenhum destinatário encontrado para denúncia {denunciaId}");
                    return;
                }

                var decisaoTexto = denuncia.DecisaoJulgamento switch
                {
                    Domain.Enums.DecisaoJulgamento.Procedente => "PROCEDENTE",
                    Domain.Enums.DecisaoJulgamento.Improcedente => "IMPROCEDENTE",
                    Domain.Enums.DecisaoJulgamento.ImprocedenteParcial => "PARCIALMENTE PROCEDENTE",
                    _ => "EM ANÁLISE"
                };

                var parametros = new Dictionary<string, string>
                {
                    ["ProtocoloDenuncia"] = denuncia.Protocolo ?? $"DEN-{denunciaId:D6}",
                    ["Denunciante"] = denuncia.Denunciante?.NomeCompleto ?? "",
                    ["Denunciado"] = denuncia.Denunciado?.NomeCompleto ?? "",
                    ["ChapaRelacionada"] = denuncia.Chapa != null ? $"{denuncia.Chapa.NumeroChapa} - {denuncia.Chapa.Nome}" : "N/A",
                    ["DataJulgamento"] = denuncia.DataJulgamento?.ToString("dd/MM/yyyy HH:mm") ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    ["Decisao"] = decisaoTexto,
                    ["Relator"] = denuncia.Relator?.NomeCompleto ?? "",
                    ["FundamentacaoResumo"] = (denuncia.FundamentacaoJulgamento ?? "").Length > 200 
                        ? denuncia.FundamentacaoJulgamento.Substring(0, 200) + "..." 
                        : denuncia.FundamentacaoJulgamento ?? "",
                    ["NomeEleicao"] = denuncia.Calendario?.Eleicao?.Nome ?? "",
                    ["AnoEleicao"] = denuncia.Calendario?.Ano.ToString() ?? "",
                    ["PrazoRecurso"] = DateTime.Now.AddDays(10).ToString("dd/MM/yyyy"),
                    ["LinkDetalhes"] = $"/denuncias/{denunciaId}/julgamento",
                    ["LinkRecurso"] = $"/denuncias/{denunciaId}/recurso"
                };

                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = destinatarios,
                    Assunto = $"Julgamento de Denúncia - Protocolo {denuncia.Protocolo} - Decisão: {decisaoTexto}",
                    TemplateId = "JulgamentoDenuncia",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Urgente
                });

                await RegistrarLogEmailAsync(denunciaId, "JulgamentoDenuncia", true, destinatarios.Count);

                _logger.LogInformation($"Email de julgamento de denúncia enviado para {destinatarios.Count} destinatários");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de julgamento de denúncia {denunciaId}");
                await RegistrarLogEmailAsync(denunciaId, "JulgamentoDenuncia", false, 0, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Agenda reenvio de email
        /// </summary>
        public void ScheduleRetry(int denunciaId, TimeSpan delay)
        {
            BackgroundJob.Schedule(() => ExecuteAsync(denunciaId), delay);
            _logger.LogInformation($"Reenvio agendado para denúncia {denunciaId} em {delay.TotalMinutes} minutos");
        }

        private async Task RegistrarLogEmailAsync(
            int denunciaId, 
            string tipoEmail, 
            bool sucesso, 
            int quantidadeDestinatarios,
            string erro = null)
        {
            var log = new EmailLog
            {
                TipoEmail = tipoEmail,
                DataEnvio = DateTime.Now,
                Sucesso = sucesso,
                QuantidadeDestinatarios = quantidadeDestinatarios,
                MensagemErro = erro
            };

            _context.EmailLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}