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
    /// Job para enviar email quando um pedido de impugnação é registrado
    /// </summary>
    public class EmailPedidoImpugnacaoJob : IEmailJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailPedidoImpugnacaoJob> _logger;
        
        public EmailPedidoImpugnacaoJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<EmailPedidoImpugnacaoJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Envia email de pedido de impugnação registrado
        /// </summary>
        [Queue("emails")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10, 30, 60 })]
        public async Task ExecuteAsync(int pedidoImpugnacaoId)
        {
            try
            {
                _logger.LogInformation($"Iniciando envio de email para pedido de impugnação {pedidoImpugnacaoId}");

                // Buscar pedido de impugnação com dados relacionados
                var pedido = await _context.PedidosImpugnacao
                    .Include(p => p.Chapa)
                        .ThenInclude(c => c.Responsavel)
                    .Include(p => p.Chapa)
                        .ThenInclude(c => c.Membros)
                            .ThenInclude(m => m.Profissional)
                    .Include(p => p.Solicitante)
                    .Include(p => p.Calendario)
                        .ThenInclude(c => c.Eleicao)
                    .FirstOrDefaultAsync(p => p.Id == pedidoImpugnacaoId);

                if (pedido == null)
                {
                    _logger.LogWarning($"Pedido de impugnação {pedidoImpugnacaoId} não encontrado");
                    return;
                }

                var destinatarios = new List<string>();

                // Adicionar responsável pela chapa
                if (pedido.Chapa?.Responsavel != null && !string.IsNullOrEmpty(pedido.Chapa.Responsavel.Email))
                {
                    destinatarios.Add(pedido.Chapa.Responsavel.Email);
                }

                // Adicionar membros da chapa
                if (pedido.Chapa?.Membros != null)
                {
                    foreach (var membro in pedido.Chapa.Membros.Where(m => m.Status == Domain.Enums.StatusMembroChapa.Confirmado))
                    {
                        if (membro.Profissional != null && !string.IsNullOrEmpty(membro.Profissional.Email))
                        {
                            destinatarios.Add(membro.Profissional.Email);
                        }
                    }
                }

                // Adicionar solicitante
                if (pedido.Solicitante != null && !string.IsNullOrEmpty(pedido.Solicitante.Email))
                {
                    destinatarios.Add(pedido.Solicitante.Email);
                }

                // Adicionar comissão eleitoral
                var comissaoEmails = await _context.MembrosComissao
                    .Include(m => m.Profissional)
                    .Where(m => m.Comissao.CalendarioId == pedido.CalendarioId &&
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
                    _logger.LogWarning($"Nenhum destinatário encontrado para pedido de impugnação {pedidoImpugnacaoId}");
                    return;
                }

                var parametros = new Dictionary<string, string>
                {
                    ["ProtocoloImpugnacao"] = pedido.Protocolo ?? $"IMP-{pedidoImpugnacaoId:D6}",
                    ["NomeChapa"] = pedido.Chapa?.Nome ?? "",
                    ["NumeroChapa"] = pedido.Chapa?.NumeroChapa ?? "",
                    ["Solicitante"] = pedido.Solicitante?.NomeCompleto ?? "",
                    ["DataSolicitacao"] = pedido.DataSolicitacao.ToString("dd/MM/yyyy HH:mm"),
                    ["MotivoImpugnacao"] = pedido.Motivo ?? "",
                    ["NomeEleicao"] = pedido.Calendario?.Eleicao?.Nome ?? "",
                    ["AnoEleicao"] = pedido.Calendario?.Ano.ToString() ?? "",
                    ["PrazoDefesa"] = DateTime.Now.AddDays(5).ToString("dd/MM/yyyy"),
                    ["LinkDetalhes"] = $"/impugnacoes/{pedidoImpugnacaoId}",
                    ["LinkDefesa"] = $"/impugnacoes/{pedidoImpugnacaoId}/defesa"
                };

                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = destinatarios,
                    Assunto = $"Pedido de Impugnação Registrado - Chapa {pedido.Chapa?.NumeroChapa} - {pedido.Chapa?.Nome}",
                    TemplateId = "PedidoImpugnacao",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Urgente
                });

                await RegistrarLogEmailAsync(pedidoImpugnacaoId, "PedidoImpugnacao", true, destinatarios.Count);

                _logger.LogInformation($"Email de pedido de impugnação enviado para {destinatarios.Count} destinatários");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email de pedido de impugnação {pedidoImpugnacaoId}");
                await RegistrarLogEmailAsync(pedidoImpugnacaoId, "PedidoImpugnacao", false, 0, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Agenda reenvio de email
        /// </summary>
        public void ScheduleRetry(int pedidoImpugnacaoId, TimeSpan delay)
        {
            BackgroundJob.Schedule(() => ExecuteAsync(pedidoImpugnacaoId), delay);
            _logger.LogInformation($"Reenvio agendado para pedido de impugnação {pedidoImpugnacaoId} em {delay.TotalMinutes} minutos");
        }

        private async Task RegistrarLogEmailAsync(
            int pedidoImpugnacaoId, 
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