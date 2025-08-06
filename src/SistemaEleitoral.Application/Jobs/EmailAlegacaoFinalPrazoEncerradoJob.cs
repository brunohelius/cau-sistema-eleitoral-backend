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
    /// Job para enviar email quando o prazo de alegações finais é encerrado
    /// </summary>
    public class EmailAlegacaoFinalPrazoEncerradoJob : IEmailJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailAlegacaoFinalPrazoEncerradoJob> _logger;
        
        public EmailAlegacaoFinalPrazoEncerradoJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<EmailAlegacaoFinalPrazoEncerradoJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Envia email de prazo de alegações finais encerrado
        /// </summary>
        [Queue("emails")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10, 30, 60 })]
        public async Task ExecuteAsync(int calendarioId)
        {
            try
            {
                _logger.LogInformation($"Iniciando envio de email para prazo de alegações finais encerrado - Calendário {calendarioId}");

                // Buscar calendário e dados relacionados
                var calendario = await _context.Calendarios
                    .Include(c => c.Eleicao)
                    .FirstOrDefaultAsync(c => c.Id == calendarioId);

                if (calendario == null)
                {
                    _logger.LogWarning($"Calendário {calendarioId} não encontrado");
                    return;
                }

                // Buscar todas as comissões eleitorais ativas para este calendário
                var comissoes = await _context.ComissoesEleitorais
                    .Include(c => c.Membros)
                        .ThenInclude(m => m.Profissional)
                    .Where(c => c.CalendarioId == calendarioId && c.Ativo)
                    .ToListAsync();

                // Buscar alegações finais pendentes
                var alegacoesPendentes = await _context.AlegacaoFinal
                    .Where(a => a.CalendarioId == calendarioId && 
                                a.Status == "Pendente")
                    .CountAsync();

                var destinatarios = new List<string>();

                // Adicionar membros das comissões
                foreach (var comissao in comissoes)
                {
                    foreach (var membro in comissao.Membros.Where(m => m.Ativo))
                    {
                        if (membro.Profissional != null && !string.IsNullOrEmpty(membro.Profissional.Email))
                        {
                            destinatarios.Add(membro.Profissional.Email);
                        }
                    }
                }

                // Adicionar coordenadores e relatores
                var coordenadores = await _context.MembrosComissao
                    .Include(m => m.Profissional)
                    .Where(m => m.Comissao.CalendarioId == calendarioId &&
                               m.Cargo == "Coordenador" && 
                               m.Ativo)
                    .Select(m => m.Profissional.Email)
                    .ToListAsync();
                
                destinatarios.AddRange(coordenadores.Where(e => !string.IsNullOrEmpty(e)));

                // Remover duplicatas
                destinatarios = destinatarios.Distinct().ToList();

                if (destinatarios.Count == 0)
                {
                    _logger.LogWarning($"Nenhum destinatário encontrado para calendário {calendarioId}");
                    return;
                }

                var parametros = new Dictionary<string, string>
                {
                    ["NomeEleicao"] = calendario.Eleicao?.Nome ?? "",
                    ["AnoEleicao"] = calendario.Ano.ToString(),
                    ["DataEncerramento"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    ["AlegacoesPendentes"] = alegacoesPendentes.ToString(),
                    ["ProximaFase"] = "Análise e Julgamento das Alegações",
                    ["LinkAcompanhamento"] = $"/calendario/{calendarioId}/alegacoes-finais"
                };

                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = destinatarios,
                    Assunto = $"Prazo de Alegações Finais Encerrado - {calendario.Eleicao?.Nome} {calendario.Ano}",
                    TemplateId = "AlegacaoFinalPrazoEncerrado",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Alta
                });

                await RegistrarLogEmailAsync(calendarioId, "AlegacaoFinalPrazoEncerrado", true, destinatarios.Count);

                _logger.LogInformation($"Email de prazo encerrado enviado para {destinatarios.Count} destinatários");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de prazo encerrado para calendário {calendarioId}");
                await RegistrarLogEmailAsync(calendarioId, "AlegacaoFinalPrazoEncerrado", false, 0, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Agenda reenvio de email
        /// </summary>
        public void ScheduleRetry(int calendarioId, TimeSpan delay)
        {
            BackgroundJob.Schedule(() => ExecuteAsync(calendarioId), delay);
            _logger.LogInformation($"Reenvio agendado para calendário {calendarioId} em {delay.TotalMinutes} minutos");
        }

        private async Task RegistrarLogEmailAsync(
            int calendarioId, 
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