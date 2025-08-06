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
    /// Job para enviar email quando uma chapa é confirmada
    /// </summary>
    public class EmailChapaConfirmadaJob : IEmailJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailChapaConfirmadaJob> _logger;
        
        public EmailChapaConfirmadaJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<EmailChapaConfirmadaJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Envia email de confirmação de chapa
        /// </summary>
        [Queue("emails")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10, 30, 60 })]
        public async Task ExecuteAsync(int chapaId)
        {
            try
            {
                _logger.LogInformation($"Iniciando envio de email para chapa confirmada {chapaId}");

                // Buscar dados da chapa
                var chapa = await _context.ChapasEleicao
                    .Include(c => c.Calendario)
                        .ThenInclude(cal => cal.Eleicao)
                    .Include(c => c.Uf)
                    .Include(c => c.Responsavel)
                    .Include(c => c.Membros)
                        .ThenInclude(m => m.Profissional)
                    .FirstOrDefaultAsync(c => c.Id == chapaId);

                if (chapa == null)
                {
                    _logger.LogWarning($"Chapa {chapaId} não encontrada");
                    return;
                }

                // Preparar lista de destinatários
                var destinatarios = new List<string>();
                
                // Adicionar responsável
                if (chapa.Responsavel != null)
                {
                    destinatarios.Add(chapa.Responsavel.Email);
                }

                // Adicionar membros confirmados
                foreach (var membro in chapa.Membros)
                {
                    if (membro.Status == Domain.Enums.StatusMembroChapa.Confirmado && 
                        membro.Profissional != null)
                    {
                        destinatarios.Add(membro.Profissional.Email);
                    }
                }

                if (destinatarios.Count == 0)
                {
                    _logger.LogWarning($"Nenhum destinatário encontrado para chapa {chapaId}");
                    return;
                }

                // Preparar parâmetros do template
                var parametros = new Dictionary<string, string>
                {
                    ["NumeroChapa"] = chapa.NumeroChapa,
                    ["NomeChapa"] = chapa.Nome,
                    ["Slogan"] = chapa.Slogan ?? "",
                    ["UF"] = chapa.Uf?.Nome ?? "",
                    ["NomeEleicao"] = chapa.Calendario?.Eleicao?.Nome ?? "",
                    ["AnoEleicao"] = chapa.Calendario?.Ano.ToString() ?? "",
                    ["DataConfirmacao"] = DateTime.Now.ToString("dd/MM/yyyy"),
                    ["QuantidadeMembros"] = chapa.Membros.Count.ToString(),
                    ["LinkAreaChapa"] = $"/chapas/{chapa.Id}/area-restrita",
                    ["ProximosPassos"] = GerarProximosPassos(chapa),
                    ["PrazosImportantes"] = await GerarPrazosImportantesAsync(chapa.CalendarioId)
                };

                // Enviar email
                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = destinatarios,
                    Assunto = $"Chapa {chapa.NumeroChapa} - {chapa.Nome} Confirmada com Sucesso",
                    TemplateId = "ChapaConfirmada",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Alta
                });

                // Registrar log de sucesso
                await RegistrarLogEmailAsync(chapaId, "ChapaConfirmada", true, destinatarios.Count);

                _logger.LogInformation($"Email enviado com sucesso para {destinatarios.Count} destinatários da chapa {chapaId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar email para chapa confirmada {chapaId}");
                
                // Registrar log de erro
                await RegistrarLogEmailAsync(chapaId, "ChapaConfirmada", false, 0, ex.Message);
                
                throw; // Re-throw para que o Hangfire possa fazer retry
            }
        }

        /// <summary>
        /// Envia email em lote para múltiplas chapas confirmadas
        /// </summary>
        [Queue("emails-batch")]
        public async Task ExecuteBatchAsync(List<int> chapaIds)
        {
            foreach (var chapaId in chapaIds)
            {
                try
                {
                    await ExecuteAsync(chapaId);
                    
                    // Delay entre envios para evitar sobrecarga
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao processar email para chapa {chapaId} no batch");
                    // Continua processando as outras chapas
                }
            }
        }

        /// <summary>
        /// Agenda reenvio de email
        /// </summary>
        public void ScheduleRetry(int chapaId, TimeSpan delay)
        {
            BackgroundJob.Schedule(() => ExecuteAsync(chapaId), delay);
            _logger.LogInformation($"Reenvio agendado para chapa {chapaId} em {delay.TotalMinutes} minutos");
        }

        private string GerarProximosPassos(Domain.Entities.ChapaEleicao chapa)
        {
            var passos = new List<string>
            {
                "1. Completar documentação pendente",
                "2. Aguardar período de impugnação",
                "3. Preparar material de campanha",
                "4. Participar dos debates agendados",
                "5. Mobilizar apoiadores"
            };

            return string.Join("\n", passos);
        }

        private async Task<string> GerarPrazosImportantesAsync(int calendarioId)
        {
            var atividades = await _context.AtividadesSecundariasCalendario
                .Where(a => 
                    a.CalendarioId == calendarioId &&
                    a.DataInicio > DateTime.Now)
                .OrderBy(a => a.DataInicio)
                .Take(5)
                .Select(a => $"{a.Nome}: {a.DataInicio:dd/MM/yyyy}")
                .ToListAsync();

            return string.Join("\n", atividades);
        }

        private async Task RegistrarLogEmailAsync(
            int chapaId, 
            string tipoEmail, 
            bool sucesso, 
            int quantidadeDestinatarios,
            string erro = null)
        {
            var log = new EmailLog
            {
                ChapaId = chapaId,
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

    /// <summary>
    /// Interface base para jobs de email
    /// </summary>
    public interface IEmailJob
    {
        Task ExecuteAsync(int entityId);
        void ScheduleRetry(int entityId, TimeSpan delay);
    }
}