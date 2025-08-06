using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Hangfire;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace SistemaEleitoral.Application.Jobs
{
    /// <summary>
    /// Job para enviar email de convite para membro de chapa
    /// </summary>
    public class EmailMembroConvidadoJob : IEmailJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailMembroConvidadoJob> _logger;
        
        public EmailMembroConvidadoJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<EmailMembroConvidadoJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Envia email de convite para membro
        /// </summary>
        [Queue("emails")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10, 30, 60 })]
        public async Task ExecuteAsync(int membroChapaId)
        {
            try
            {
                _logger.LogInformation($"Iniciando envio de convite para membro {membroChapaId}");

                // Buscar dados do membro e chapa
                var membro = await _context.MembrosChapa
                    .Include(m => m.Chapa)
                        .ThenInclude(c => c.Calendario)
                            .ThenInclude(cal => cal.Eleicao)
                    .Include(m => m.Chapa.Uf)
                    .Include(m => m.Chapa.Responsavel)
                    .Include(m => m.Profissional)
                    .FirstOrDefaultAsync(m => m.Id == membroChapaId);

                if (membro == null)
                {
                    _logger.LogWarning($"Membro {membroChapaId} não encontrado");
                    return;
                }

                if (membro.Profissional == null)
                {
                    _logger.LogWarning($"Profissional do membro {membroChapaId} não encontrado");
                    return;
                }

                // Verificar se o convite ainda está pendente
                if (membro.Status != Domain.Enums.StatusMembroChapa.ConvitePendente)
                {
                    _logger.LogInformation($"Membro {membroChapaId} não está com convite pendente. Status: {membro.Status}");
                    return;
                }

                // Gerar link de confirmação
                var linkConfirmacao = GerarLinkConfirmacao(membro);
                var linkRecusa = GerarLinkRecusa(membro);

                // Preparar parâmetros do template
                var parametros = new Dictionary<string, string>
                {
                    ["NomeProfissional"] = membro.Profissional.Nome,
                    ["NumeroChapa"] = membro.Chapa.NumeroChapa,
                    ["NomeChapa"] = membro.Chapa.Nome,
                    ["SloganChapa"] = membro.Chapa.Slogan ?? "",
                    ["NomeResponsavel"] = membro.Chapa.Responsavel?.Nome ?? "",
                    ["Cargo"] = membro.Cargo ?? "Membro",
                    ["TipoParticipacao"] = membro.TipoParticipacao.ToString(),
                    ["UF"] = membro.Chapa.Uf?.Nome ?? "",
                    ["NomeEleicao"] = membro.Chapa.Calendario?.Eleicao?.Nome ?? "",
                    ["AnoEleicao"] = membro.Chapa.Calendario?.Ano.ToString() ?? "",
                    ["DataExpiracao"] = membro.DataExpiracaoToken?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    ["LinkConfirmacao"] = linkConfirmacao,
                    ["LinkRecusa"] = linkRecusa,
                    ["MensagemPersonalizada"] = await ObterMensagemPersonalizadaAsync(membro.ChapaId),
                    ["InformacoesChapa"] = await GerarInformacoesChapaAsync(membro.Chapa)
                };

                // Enviar email
                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = new List<string> { membro.Profissional.Email },
                    Cc = new List<string> { membro.Chapa.Responsavel?.Email },
                    Assunto = $"Convite para participar da Chapa {membro.Chapa.NumeroChapa} - {membro.Chapa.Nome}",
                    TemplateId = "ConviteMembro",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Alta
                });

                // Registrar log de sucesso
                await RegistrarLogConviteAsync(membro, true);

                _logger.LogInformation($"Convite enviado com sucesso para {membro.Profissional.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar convite para membro {membroChapaId}");
                
                // Registrar log de erro
                await RegistrarLogConviteErroAsync(membroChapaId, ex.Message);
                
                throw; // Re-throw para que o Hangfire possa fazer retry
            }
        }

        /// <summary>
        /// Reenvia convite expirado
        /// </summary>
        [Queue("emails")]
        public async Task ReenviarConviteAsync(int membroChapaId, string novoToken)
        {
            try
            {
                // Atualizar token e data de expiração
                var membro = await _context.MembrosChapa
                    .FirstOrDefaultAsync(m => m.Id == membroChapaId);

                if (membro != null)
                {
                    membro.TokenConfirmacao = novoToken;
                    membro.DataExpiracaoToken = DateTime.Now.AddDays(7);
                    await _context.SaveChangesAsync();

                    // Enviar novo convite
                    await ExecuteAsync(membroChapaId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao reenviar convite para membro {membroChapaId}");
                throw;
            }
        }

        /// <summary>
        /// Envia lembrete de convite pendente
        /// </summary>
        [Queue("emails")]
        public async Task EnviarLembreteAsync(int membroChapaId)
        {
            try
            {
                var membro = await _context.MembrosChapa
                    .Include(m => m.Profissional)
                    .Include(m => m.Chapa)
                    .FirstOrDefaultAsync(m => m.Id == membroChapaId);

                if (membro == null || membro.Status != Domain.Enums.StatusMembroChapa.ConvitePendente)
                    return;

                // Verificar se o convite ainda é válido
                if (membro.DataExpiracaoToken < DateTime.Now)
                {
                    _logger.LogInformation($"Convite expirado para membro {membroChapaId}");
                    return;
                }

                var diasRestantes = (membro.DataExpiracaoToken.Value - DateTime.Now).Days;

                var parametros = new Dictionary<string, string>
                {
                    ["NomeProfissional"] = membro.Profissional.Nome,
                    ["NumeroChapa"] = membro.Chapa.NumeroChapa,
                    ["NomeChapa"] = membro.Chapa.Nome,
                    ["DiasRestantes"] = diasRestantes.ToString(),
                    ["LinkConfirmacao"] = GerarLinkConfirmacao(membro)
                };

                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = new List<string> { membro.Profissional.Email },
                    Assunto = $"Lembrete: Convite expira em {diasRestantes} dias - Chapa {membro.Chapa.NumeroChapa}",
                    TemplateId = "LembreteConvite",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Normal
                });

                _logger.LogInformation($"Lembrete enviado para membro {membroChapaId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar lembrete para membro {membroChapaId}");
            }
        }

        public void ScheduleRetry(int entityId, TimeSpan delay)
        {
            BackgroundJob.Schedule(() => ExecuteAsync(entityId), delay);
            _logger.LogInformation($"Reenvio de convite agendado para membro {entityId} em {delay.TotalMinutes} minutos");
        }

        private string GerarLinkConfirmacao(Domain.Entities.MembroChapa membro)
        {
            var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "https://eleicoes.caubr.gov.br";
            return $"{baseUrl}/convite/confirmar/{membro.Id}?token={membro.TokenConfirmacao}";
        }

        private string GerarLinkRecusa(Domain.Entities.MembroChapa membro)
        {
            var baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "https://eleicoes.caubr.gov.br";
            return $"{baseUrl}/convite/recusar/{membro.Id}?token={membro.TokenConfirmacao}";
        }

        private async Task<string> ObterMensagemPersonalizadaAsync(int chapaId)
        {
            var mensagem = await _context.MensagensPersonalizadas
                .Where(m => m.ChapaId == chapaId && m.Tipo == "ConviteMembro")
                .Select(m => m.Conteudo)
                .FirstOrDefaultAsync();

            return mensagem ?? "Contamos com sua participação para construirmos juntos uma gestão de excelência!";
        }

        private async Task<string> GerarInformacoesChapaAsync(Domain.Entities.ChapaEleicao chapa)
        {
            var membrosConfirmados = await _context.MembrosChapa
                .Where(m => 
                    m.ChapaId == chapa.Id && 
                    m.Status == Domain.Enums.StatusMembroChapa.Confirmado)
                .CountAsync();

            var info = new List<string>
            {
                $"Número da Chapa: {chapa.NumeroChapa}",
                $"Nome: {chapa.Nome}",
                $"Slogan: {chapa.Slogan}",
                $"Membros confirmados: {membrosConfirmados}",
                $"UF: {chapa.Uf?.Nome}"
            };

            return string.Join("\n", info);
        }

        private async Task RegistrarLogConviteAsync(Domain.Entities.MembroChapa membro, bool sucesso)
        {
            var log = new LogConviteEnviado
            {
                MembroChapaId = membro.Id,
                ProfissionalId = membro.ProfissionalId,
                ChapaId = membro.ChapaId,
                DataEnvio = DateTime.Now,
                Sucesso = sucesso,
                TokenUtilizado = membro.TokenConfirmacao
            };

            _context.LogsConviteEnviado.Add(log);
            await _context.SaveChangesAsync();
        }

        private async Task RegistrarLogConviteErroAsync(int membroChapaId, string erro)
        {
            var log = new LogConviteEnviado
            {
                MembroChapaId = membroChapaId,
                DataEnvio = DateTime.Now,
                Sucesso = false,
                MensagemErro = erro
            };

            _context.LogsConviteEnviado.Add(log);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Entidade de mensagem personalizada
    /// </summary>
    public class MensagemPersonalizada
    {
        public int Id { get; set; }
        public int ChapaId { get; set; }
        public string Tipo { get; set; }
        public string Conteudo { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    /// <summary>
    /// Entidade de log de convite
    /// </summary>
    public class LogConviteEnviado
    {
        public int Id { get; set; }
        public int MembroChapaId { get; set; }
        public int ProfissionalId { get; set; }
        public int ChapaId { get; set; }
        public DateTime DataEnvio { get; set; }
        public bool Sucesso { get; set; }
        public string TokenUtilizado { get; set; }
        public string MensagemErro { get; set; }
    }
}