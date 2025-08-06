using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hangfire;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using SistemaEleitoral.Application.DTOs;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Application.Jobs
{
    /// <summary>
    /// Job para enviar email quando uma alegação final é cadastrada
    /// </summary>
    public class EmailAlegacaoFinalCadastradaJob : IEmailJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailAlegacaoFinalCadastradaJob> _logger;
        
        public EmailAlegacaoFinalCadastradaJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<EmailAlegacaoFinalCadastradaJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Envia email de alegação final cadastrada
        /// </summary>
        [Queue("emails")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10, 30, 60 })]
        public async Task ExecuteAsync(int alegacaoFinalId)
        {
            try
            {
                _logger.LogInformation($"Iniciando envio de email para alegação final cadastrada {alegacaoFinalId}");

                // Lógica para buscar dados da alegação final e destinatários
                // Exemplo: Buscar a alegação final e o usuário responsável
                var alegacaoFinal = await _context.AlegacaoFinal
                                                .Include(a => a.Responsavel)
                                                .FirstOrDefaultAsync(a => a.Id == alegacaoFinalId);

                if (alegacaoFinal == null)
                {
                    _logger.LogWarning($"Alegação Final {alegacaoFinalId} não encontrada");
                    return;
                }

                var destinatarios = new List<string>();
                if (alegacaoFinal.Responsavel != null && !string.IsNullOrEmpty(alegacaoFinal.Responsavel.Email))
                {
                    destinatarios.Add(alegacaoFinal.Responsavel.Email);
                }

                if (destinatarios.Count == 0)
                {
                    _logger.LogWarning($"Nenhum destinatário encontrado para alegação final {alegacaoFinalId}");
                    return;
                }

                var parametros = new Dictionary<string, string>
                {
                    ["ProtocoloAlegacao"] = alegacaoFinal.Protocolo,
                    ["DataCadastro"] = alegacaoFinal.DataCadastro.ToString("dd/MM/yyyy HH:mm"),
                    ["ResponsavelNome"] = alegacaoFinal.Responsavel?.Nome ?? "",
                    ["LinkAlegacao"] = $"/alegacoes-finais/{alegacaoFinal.Id}"
                };

                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = destinatarios,
                    Assunto = $"Alegação Final Cadastrada - Protocolo: {alegacaoFinal.Protocolo}",
                    TemplateId = "AlegacaoFinalCadastrada",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Normal
                });

                await RegistrarLogEmailAsync(alegacaoFinalId, "AlegacaoFinalCadastrada", true, destinatarios.Count);

                _logger.LogInformation($"Email de alegação final cadastrada enviado com sucesso para {destinatarios.Count} destinatários da alegação {alegacaoFinalId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email para alegação final cadastrada {alegacaoFinalId}");
                await RegistrarLogEmailAsync(alegacaoFinalId, "AlegacaoFinalCadastrada", false, 0, ex.Message);
                throw; 
            }
        }

        /// <summary>
        /// Agenda reenvio de email
        /// </summary>
        public void ScheduleRetry(int alegacaoFinalId, TimeSpan delay)
        {
            BackgroundJob.Schedule(() => ExecuteAsync(alegacaoFinalId), delay);
            _logger.LogInformation($"Reenvio agendado para alegação final {alegacaoFinalId} em {delay.TotalMinutes} minutos");
        }

        private async Task RegistrarLogEmailAsync(
            int alegacaoFinalId, 
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
                MensagemErro = erro,
                EntityId = alegacaoFinalId
            };

            _context.EmailLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}