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
    /// Job para enviar emails relacionados ao processo de votação
    /// </summary>
    public class EmailVotacaoAbertaJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailVotacaoAbertaJob> _logger;
        
        public EmailVotacaoAbertaJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<EmailVotacaoAbertaJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Envia email notificando abertura da votação
        /// </summary>
        [Queue("emails-batch")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10, 30, 60 })]
        public async Task NotificarAberturaVotacaoAsync(int sessaoVotacaoId)
        {
            try
            {
                _logger.LogInformation($"Iniciando notificação de abertura de votação para sessão {sessaoVotacaoId}");

                var sessao = await _context.SessoesVotacao
                    .Include(s => s.Calendario)
                        .ThenInclude(c => c.Eleicao)
                    .Include(s => s.Uf)
                    .FirstOrDefaultAsync(s => s.Id == sessaoVotacaoId);

                if (sessao == null)
                {
                    _logger.LogWarning($"Sessão de votação {sessaoVotacaoId} não encontrada");
                    return;
                }

                // Buscar eleitores aptos
                var eleitores = await ObterEleitoresAptosAsync(sessao.UfId);
                
                if (!eleitores.Any())
                {
                    _logger.LogWarning($"Nenhum eleitor apto encontrado para UF {sessao.UfId}");
                    return;
                }

                // Buscar chapas concorrentes
                var chapas = await ObterChapasConcorrentesAsync(sessao.CalendarioId, sessao.UfId);

                // Preparar template base
                var parametrosBase = new Dictionary<string, string>
                {
                    ["NomeEleicao"] = sessao.Calendario?.Eleicao?.Nome ?? "",
                    ["AnoEleicao"] = sessao.Calendario?.Ano.ToString() ?? "",
                    ["UF"] = sessao.Uf?.Nome ?? "",
                    ["DataAbertura"] = sessao.DataAbertura.ToString("dd/MM/yyyy HH:mm"),
                    ["DataFechamentoPrevisto"] = sessao.DataAbertura.AddHours(8).ToString("dd/MM/yyyy HH:mm"),
                    ["Turno"] = sessao.Turno == 1 ? "Primeiro Turno" : "Segundo Turno",
                    ["QuantidadeChapas"] = chapas.Count.ToString(),
                    ["LinkVotacao"] = $"/votacao/votar/{sessaoVotacaoId}",
                    ["InstrucoesVoto"] = GerarInstrucoesVoto(),
                    ["ChapasDisponiveis"] = GerarListaChapas(chapas)
                };

                // Enviar em lotes para evitar sobrecarga
                const int tamanhoBatch = 100;
                for (int i = 0; i < eleitores.Count; i += tamanhoBatch)
                {
                    var batch = eleitores.Skip(i).Take(tamanhoBatch).ToList();
                    await EnviarBatchEmailsAsync(batch, parametrosBase, sessao);
                    
                    // Delay entre batches
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                _logger.LogInformation($"Notificações enviadas para {eleitores.Count} eleitores");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao notificar abertura de votação para sessão {sessaoVotacaoId}");
                throw;
            }
        }

        /// <summary>
        /// Envia lembrete de votação para quem ainda não votou
        /// </summary>
        [Queue("emails-batch")]
        public async Task EnviarLembreteVotacaoAsync(int sessaoVotacaoId)
        {
            try
            {
                var sessao = await _context.SessoesVotacao
                    .Include(s => s.Calendario)
                    .Include(s => s.Uf)
                    .FirstOrDefaultAsync(s => s.Id == sessaoVotacaoId);

                if (sessao == null || sessao.Status != Domain.Enums.StatusSessaoVotacao.Aberta)
                    return;

                // Buscar eleitores que ainda não votaram
                var eleitoresQueVotaram = await _context.Votos
                    .Where(v => v.SessaoVotacaoId == sessaoVotacaoId)
                    .Select(v => v.EleitorId)
                    .Distinct()
                    .ToListAsync();

                var eleitoresAptos = await ObterEleitoresAptosAsync(sessao.UfId);
                var eleitoresPendentes = eleitoresAptos
                    .Where(e => !eleitoresQueVotaram.Contains(e.Id))
                    .ToList();

                if (!eleitoresPendentes.Any())
                    return;

                var horasRestantes = (sessao.DataAbertura.AddHours(8) - DateTime.Now).TotalHours;
                
                var parametros = new Dictionary<string, string>
                {
                    ["UF"] = sessao.Uf?.Nome ?? "",
                    ["HorasRestantes"] = Math.Round(horasRestantes, 1).ToString(),
                    ["LinkVotacao"] = $"/votacao/votar/{sessaoVotacaoId}",
                    ["Urgencia"] = horasRestantes < 2 ? "URGENTE: " : ""
                };

                foreach (var eleitor in eleitoresPendentes)
                {
                    parametros["NomeEleitor"] = eleitor.Nome;

                    await _notificationService.EnviarEmailAsync(new EmailModel
                    {
                        Para = new List<string> { eleitor.Email },
                        Assunto = $"{parametros["Urgencia"]}Lembrete - Votação se encerra em {parametros["HorasRestantes"]} horas",
                        TemplateId = "LembreteVotacao",
                        ParametrosTemplate = parametros,
                        Prioridade = horasRestantes < 2 ? EmailPrioridade.Urgente : EmailPrioridade.Alta
                    });
                }

                _logger.LogInformation($"Lembretes enviados para {eleitoresPendentes.Count} eleitores");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar lembretes de votação para sessão {sessaoVotacaoId}");
            }
        }

        /// <summary>
        /// Envia comprovante de votação
        /// </summary>
        [Queue("emails")]
        public async Task EnviarComprovanteVotoAsync(int votoId)
        {
            try
            {
                var voto = await _context.Votos
                    .Include(v => v.SessaoVotacao)
                        .ThenInclude(s => s.Calendario)
                            .ThenInclude(c => c.Eleicao)
                    .Include(v => v.SessaoVotacao.Uf)
                    .Include(v => v.Eleitor)
                    .FirstOrDefaultAsync(v => v.Id == votoId);

                if (voto == null || voto.Eleitor == null)
                    return;

                var parametros = new Dictionary<string, string>
                {
                    ["NomeEleitor"] = voto.Eleitor.Nome,
                    ["ProtocoloVoto"] = voto.ProtocoloComprovante,
                    ["DataHoraVoto"] = voto.DataHoraVoto.ToString("dd/MM/yyyy HH:mm:ss"),
                    ["NomeEleicao"] = voto.SessaoVotacao?.Calendario?.Eleicao?.Nome ?? "",
                    ["AnoEleicao"] = voto.SessaoVotacao?.Calendario?.Ano.ToString() ?? "",
                    ["UF"] = voto.SessaoVotacao?.Uf?.Nome ?? "",
                    ["HashComprovante"] = voto.HashVoto,
                    ["CodigoVerificacao"] = GerarCodigoVerificacao(voto.ProtocoloComprovante),
                    ["LinkVerificacao"] = $"/votacao/verificar/{voto.ProtocoloComprovante}",
                    ["InformacoesLegais"] = "Este comprovante é o seu documento oficial de participação na eleição"
                };

                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = new List<string> { voto.Eleitor.Email },
                    Assunto = $"Comprovante de Votação - Protocolo {voto.ProtocoloComprovante}",
                    TemplateId = "ComprovanteVoto",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Alta
                });

                _logger.LogInformation($"Comprovante enviado para eleitor {voto.EleitorId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar comprovante de voto {votoId}");
                throw;
            }
        }

        /// <summary>
        /// Notifica encerramento da votação
        /// </summary>
        [Queue("emails-batch")]
        public async Task NotificarEncerramentoVotacaoAsync(int sessaoVotacaoId)
        {
            try
            {
                var sessao = await _context.SessoesVotacao
                    .Include(s => s.Calendario)
                    .Include(s => s.Uf)
                    .Include(s => s.EstatisticasVotacao)
                    .FirstOrDefaultAsync(s => s.Id == sessaoVotacaoId);

                if (sessao == null)
                    return;

                var estatistica = sessao.EstatisticasVotacao.FirstOrDefault();
                
                // Buscar todos os eleitores da UF
                var eleitores = await ObterEleitoresAptosAsync(sessao.UfId);

                var parametros = new Dictionary<string, string>
                {
                    ["UF"] = sessao.Uf?.Nome ?? "",
                    ["DataEncerramento"] = sessao.DataFechamento?.ToString("dd/MM/yyyy HH:mm") ?? "",
                    ["TotalEleitores"] = estatistica?.TotalEleitores.ToString() ?? "0",
                    ["TotalVotantes"] = estatistica?.TotalVotantes.ToString() ?? "0",
                    ["PercentualParticipacao"] = estatistica?.PercentualParticipacao.ToString("F2") ?? "0",
                    ["ProximaEtapa"] = "Aguarde a apuração dos resultados",
                    ["PrevisaoResultado"] = DateTime.Now.AddHours(2).ToString("dd/MM/yyyy HH:mm")
                };

                // Enviar para todos os eleitores
                foreach (var batch in eleitores.Chunk(100))
                {
                    var destinatarios = batch.Select(e => e.Email).ToList();

                    await _notificationService.EnviarEmailAsync(new EmailModel
                    {
                        Para = destinatarios,
                        Assunto = "Votação Encerrada - Aguarde os Resultados",
                        TemplateId = "VotacaoEncerrada",
                        ParametrosTemplate = parametros,
                        Prioridade = EmailPrioridade.Normal
                    });

                    await Task.Delay(TimeSpan.FromSeconds(3));
                }

                _logger.LogInformation($"Notificação de encerramento enviada para {eleitores.Count} eleitores");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao notificar encerramento de votação para sessão {sessaoVotacaoId}");
            }
        }

        private async Task<List<Domain.Entities.Profissional>> ObterEleitoresAptosAsync(int ufId)
        {
            return await _context.Profissionais
                .Where(p => 
                    p.UfId == ufId &&
                    p.StatusRegistro == Domain.Enums.StatusRegistroProfissional.Ativo &&
                    !string.IsNullOrEmpty(p.Email))
                .ToListAsync();
        }

        private async Task<List<Domain.Entities.ChapaEleicao>> ObterChapasConcorrentesAsync(int calendarioId, int ufId)
        {
            return await _context.ChapasEleicao
                .Include(c => c.Responsavel)
                .Where(c => 
                    c.CalendarioId == calendarioId &&
                    c.UfId == ufId &&
                    (c.Status == Domain.Enums.StatusChapa.Deferida || 
                     c.Status == Domain.Enums.StatusChapa.Apta))
                .OrderBy(c => c.NumeroChapa)
                .ToListAsync();
        }

        private async Task EnviarBatchEmailsAsync(
            List<Domain.Entities.Profissional> eleitores,
            Dictionary<string, string> parametrosBase,
            Domain.Entities.SessaoVotacao sessao)
        {
            var destinatarios = eleitores.Select(e => e.Email).ToList();

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = destinatarios,
                Assunto = $"Votação Aberta - Eleição {sessao.Calendario?.Ano} - {sessao.Uf?.Nome}",
                TemplateId = "VotacaoAberta",
                ParametrosTemplate = parametrosBase,
                Prioridade = EmailPrioridade.Alta
            });
        }

        private string GerarInstrucoesVoto()
        {
            var instrucoes = new List<string>
            {
                "1. Acesse o sistema com seu login e senha",
                "2. Clique no link de votação",
                "3. Escolha sua chapa ou vote em branco",
                "4. Confirme seu voto",
                "5. Guarde o comprovante"
            };

            return string.Join("\n", instrucoes);
        }

        private string GerarListaChapas(List<Domain.Entities.ChapaEleicao> chapas)
        {
            var lista = chapas.Select(c => $"• {c.NumeroChapa} - {c.Nome}");
            return string.Join("\n", lista);
        }

        private string GerarCodigoVerificacao(string protocolo)
        {
            return protocolo.GetHashCode().ToString("X8");
        }
    }
}