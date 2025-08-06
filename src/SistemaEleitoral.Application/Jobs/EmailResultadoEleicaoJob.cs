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
    /// Job para enviar emails com resultado da eleição
    /// </summary>
    public class EmailResultadoEleicaoJob
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<EmailResultadoEleicaoJob> _logger;
        
        public EmailResultadoEleicaoJob(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<EmailResultadoEleicaoJob> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Envia resultado da eleição para todos os participantes
        /// </summary>
        [Queue("emails-batch")]
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new int[] { 10, 30, 60 })]
        public async Task EnviarResultadoEleicaoAsync(int resultadoApuracaoId)
        {
            try
            {
                _logger.LogInformation($"Iniciando envio de resultado da eleição {resultadoApuracaoId}");

                var resultado = await _context.ResultadosApuracao
                    .Include(r => r.SessaoVotacao)
                        .ThenInclude(s => s.Calendario)
                            .ThenInclude(c => c.Eleicao)
                    .Include(r => r.SessaoVotacao.Uf)
                    .Include(r => r.ResultadosChapas)
                        .ThenInclude(rc => rc.Chapa)
                            .ThenInclude(c => c.Responsavel)
                    .Include(r => r.ResultadosChapas)
                        .ThenInclude(rc => rc.Chapa)
                            .ThenInclude(c => c.Membros)
                                .ThenInclude(m => m.Profissional)
                    .FirstOrDefaultAsync(r => r.Id == resultadoApuracaoId);

                if (resultado == null)
                {
                    _logger.LogWarning($"Resultado {resultadoApuracaoId} não encontrado");
                    return;
                }

                // Preparar dados do resultado
                var chapaVencedora = resultado.ChapaVencedoraId.HasValue ?
                    await _context.ChapasEleicao
                        .Include(c => c.Responsavel)
                        .FirstOrDefaultAsync(c => c.Id == resultado.ChapaVencedoraId.Value) : null;

                var resultadosOrdenados = resultado.ResultadosChapas
                    .OrderByDescending(rc => rc.TotalVotos)
                    .ToList();

                // Enviar para chapas participantes
                await NotificarChapasAsync(resultado, resultadosOrdenados, chapaVencedora);

                // Enviar para todos os eleitores
                await NotificarEleitoresAsync(resultado, resultadosOrdenados, chapaVencedora);

                // Enviar para comissão eleitoral
                await NotificarComissaoAsync(resultado, resultadosOrdenados, chapaVencedora);

                _logger.LogInformation($"Resultado enviado com sucesso para eleição {resultado.SessaoVotacao.CalendarioId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao enviar resultado da eleição {resultadoApuracaoId}");
                throw;
            }
        }

        /// <summary>
        /// Notifica chapas vencedoras
        /// </summary>
        [Queue("emails")]
        public async Task NotificarChapaVencedoraAsync(int chapaId)
        {
            try
            {
                var chapa = await _context.ChapasEleicao
                    .Include(c => c.Calendario)
                        .ThenInclude(cal => cal.Eleicao)
                    .Include(c => c.Uf)
                    .Include(c => c.Responsavel)
                    .Include(c => c.Membros)
                        .ThenInclude(m => m.Profissional)
                    .FirstOrDefaultAsync(c => c.Id == chapaId);

                if (chapa == null)
                    return;

                // Buscar resultado completo
                var resultado = await _context.ResultadosChapasApuracao
                    .Include(rc => rc.ResultadoApuracao)
                    .FirstOrDefaultAsync(rc => rc.ChapaId == chapaId);

                if (resultado == null)
                    return;

                var destinatarios = new List<string>();
                
                // Adicionar responsável e membros
                if (chapa.Responsavel != null)
                    destinatarios.Add(chapa.Responsavel.Email);

                destinatarios.AddRange(chapa.Membros
                    .Where(m => m.Profissional != null && m.Status == Domain.Enums.StatusMembroChapa.Confirmado)
                    .Select(m => m.Profissional.Email));

                var parametros = new Dictionary<string, string>
                {
                    ["NumeroChapa"] = chapa.NumeroChapa,
                    ["NomeChapa"] = chapa.Nome,
                    ["TotalVotos"] = resultado.TotalVotos.ToString(),
                    ["PercentualVotos"] = resultado.PercentualVotos.ToString("F2") + "%",
                    ["NomeEleicao"] = chapa.Calendario?.Eleicao?.Nome ?? "",
                    ["AnoEleicao"] = chapa.Calendario?.Ano.ToString() ?? "",
                    ["UF"] = chapa.Uf?.Nome ?? "",
                    ["ProximosPassos"] = GerarProximosPassosVencedor(),
                    ["MensagemParabens"] = "PARABÉNS! Sua chapa foi eleita!",
                    ["DataPosse"] = chapa.Calendario?.DataPosse?.ToString("dd/MM/yyyy") ?? "A definir"
                };

                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = destinatarios.Distinct().ToList(),
                    Assunto = $"🎉 PARABÉNS! Chapa {chapa.NumeroChapa} ELEITA!",
                    TemplateId = "ChapaEleita",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Alta
                });

                _logger.LogInformation($"Notificação de vitória enviada para chapa {chapaId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao notificar chapa vencedora {chapaId}");
                throw;
            }
        }

        private async Task NotificarChapasAsync(
            Domain.Entities.ResultadoApuracao resultado,
            List<Domain.Entities.ResultadoChapaApuracao> resultadosOrdenados,
            Domain.Entities.ChapaEleicao chapaVencedora)
        {
            foreach (var resultadoChapa in resultadosOrdenados)
            {
                var chapa = resultadoChapa.Chapa;
                if (chapa == null)
                    continue;

                var destinatarios = new List<string>();
                
                if (chapa.Responsavel != null)
                    destinatarios.Add(chapa.Responsavel.Email);

                destinatarios.AddRange(chapa.Membros
                    .Where(m => m.Profissional != null && m.Status == Domain.Enums.StatusMembroChapa.Confirmado)
                    .Select(m => m.Profissional.Email));

                if (!destinatarios.Any())
                    continue;

                var posicao = resultadosOrdenados.IndexOf(resultadoChapa) + 1;
                var venceu = chapa.Id == chapaVencedora?.Id;

                var parametros = new Dictionary<string, string>
                {
                    ["NumeroChapa"] = chapa.NumeroChapa,
                    ["NomeChapa"] = chapa.Nome,
                    ["Posicao"] = posicao.ToString() + "º lugar",
                    ["TotalVotos"] = resultadoChapa.TotalVotos.ToString(),
                    ["PercentualVotos"] = resultadoChapa.PercentualVotos.ToString("F2") + "%",
                    ["TotalVotosValidos"] = resultado.TotalVotosValidos.ToString(),
                    ["TotalVotosBrancos"] = resultado.TotalVotosBrancos.ToString(),
                    ["TotalVotosNulos"] = resultado.TotalVotosNulos.ToString(),
                    ["ChapaVencedora"] = chapaVencedora?.Nome ?? "A definir",
                    ["ResultadoChapa"] = venceu ? "ELEITA" : "NÃO ELEITA",
                    ["Mensagem"] = venceu ? 
                        "Parabéns pela vitória! Sua chapa foi eleita!" : 
                        "Agradecemos sua participação no processo democrático",
                    ["LinkResultadoCompleto"] = $"/eleicoes/resultado/{resultado.Id}"
                };

                var templateId = venceu ? "ChapaEleita" : "ResultadoChapa";

                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = destinatarios.Distinct().ToList(),
                    Assunto = $"Resultado da Eleição - Chapa {chapa.NumeroChapa}",
                    TemplateId = templateId,
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Alta
                });

                // Delay entre envios
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        private async Task NotificarEleitoresAsync(
            Domain.Entities.ResultadoApuracao resultado,
            List<Domain.Entities.ResultadoChapaApuracao> resultadosOrdenados,
            Domain.Entities.ChapaEleicao chapaVencedora)
        {
            // Buscar todos os eleitores da UF
            var eleitores = await _context.Profissionais
                .Where(p => 
                    p.UfId == resultado.SessaoVotacao.UfId &&
                    p.StatusRegistro == Domain.Enums.StatusRegistroProfissional.Ativo &&
                    !string.IsNullOrEmpty(p.Email))
                .ToListAsync();

            if (!eleitores.Any())
                return;

            // Preparar ranking das chapas
            var ranking = string.Join("\n", resultadosOrdenados.Select((rc, index) => 
                $"{index + 1}º - {rc.Chapa?.NumeroChapa} - {rc.Chapa?.Nome}: {rc.TotalVotos} votos ({rc.PercentualVotos:F2}%)"
            ));

            var parametros = new Dictionary<string, string>
            {
                ["NomeEleicao"] = resultado.SessaoVotacao?.Calendario?.Eleicao?.Nome ?? "",
                ["AnoEleicao"] = resultado.SessaoVotacao?.Calendario?.Ano.ToString() ?? "",
                ["UF"] = resultado.SessaoVotacao?.Uf?.Nome ?? "",
                ["ChapaVencedora"] = chapaVencedora != null ? 
                    $"{chapaVencedora.NumeroChapa} - {chapaVencedora.Nome}" : "A definir",
                ["TotalVotosValidos"] = resultado.TotalVotosValidos.ToString(),
                ["TotalVotosBrancos"] = resultado.TotalVotosBrancos.ToString(),
                ["TotalVotosNulos"] = resultado.TotalVotosNulos.ToString(),
                ["TotalGeralVotos"] = resultado.TotalGeralVotos.ToString(),
                ["RankingChapas"] = ranking,
                ["PrecisaSegundoTurno"] = resultado.PrecisaSegundoTurno ? "SIM" : "NÃO",
                ["LinkResultadoCompleto"] = $"/eleicoes/resultado/{resultado.Id}",
                ["LinkBoletimUrna"] = $"/eleicoes/boletim/{resultado.Id}"
            };

            // Enviar em lotes
            foreach (var batch in eleitores.Chunk(100))
            {
                var destinatarios = batch.Select(e => e.Email).ToList();

                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = destinatarios,
                    Assunto = $"Resultado Oficial - Eleição {resultado.SessaoVotacao?.Calendario?.Ano}",
                    TemplateId = "ResultadoEleicao",
                    ParametrosTemplate = parametros,
                    Prioridade = EmailPrioridade.Normal
                });

                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            _logger.LogInformation($"Resultado enviado para {eleitores.Count} eleitores");
        }

        private async Task NotificarComissaoAsync(
            Domain.Entities.ResultadoApuracao resultado,
            List<Domain.Entities.ResultadoChapaApuracao> resultadosOrdenados,
            Domain.Entities.ChapaEleicao chapaVencedora)
        {
            // Buscar membros da comissão eleitoral
            var membrosComissao = await _context.MembrosComissao
                .Include(m => m.Profissional)
                .Include(m => m.Comissao)
                .Where(m => 
                    m.Comissao.UfId == resultado.SessaoVotacao.UfId &&
                    m.Comissao.CalendarioId == resultado.SessaoVotacao.CalendarioId &&
                    m.Ativo)
                .ToListAsync();

            if (!membrosComissao.Any())
                return;

            var destinatarios = membrosComissao
                .Where(m => m.Profissional != null)
                .Select(m => m.Profissional.Email)
                .Distinct()
                .ToList();

            // Preparar estatísticas detalhadas
            var estatisticas = new List<string>
            {
                $"Total de Eleitores: {resultado.SessaoVotacao?.EstatisticasVotacao?.FirstOrDefault()?.TotalEleitores ?? 0}",
                $"Total de Votantes: {resultado.SessaoVotacao?.EstatisticasVotacao?.FirstOrDefault()?.TotalVotantes ?? 0}",
                $"Participação: {resultado.SessaoVotacao?.EstatisticasVotacao?.FirstOrDefault()?.PercentualParticipacao ?? 0:F2}%",
                $"Votos Válidos: {resultado.TotalVotosValidos}",
                $"Votos Brancos: {resultado.TotalVotosBrancos}",
                $"Votos Nulos: {resultado.TotalVotosNulos}"
            };

            var parametros = new Dictionary<string, string>
            {
                ["UF"] = resultado.SessaoVotacao?.Uf?.Nome ?? "",
                ["DataApuracao"] = resultado.DataFimApuracao?.ToString("dd/MM/yyyy HH:mm") ?? "",
                ["ChapaVencedora"] = chapaVencedora != null ? 
                    $"{chapaVencedora.NumeroChapa} - {chapaVencedora.Nome}" : "A definir",
                ["PrecisaSegundoTurno"] = resultado.PrecisaSegundoTurno ? "SIM" : "NÃO",
                ["Estatisticas"] = string.Join("\n", estatisticas),
                ["HashResultado"] = resultado.HashResultado,
                ["ProximasAcoes"] = resultado.PrecisaSegundoTurno ? 
                    "Organizar segundo turno" : "Homologar resultado e preparar posse",
                ["LinkRelatorio"] = $"/comissao/relatorio-apuracao/{resultado.Id}"
            };

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = destinatarios,
                Assunto = "[COMISSÃO] Resultado da Apuração - Ação Necessária",
                TemplateId = "ComissaoResultadoApuracao",
                ParametrosTemplate = parametros,
                Prioridade = EmailPrioridade.Alta
            });

            _logger.LogInformation($"Resultado enviado para {destinatarios.Count} membros da comissão");
        }

        private string GerarProximosPassosVencedor()
        {
            var passos = new List<string>
            {
                "1. Aguardar homologação do resultado",
                "2. Preparar documentação para posse",
                "3. Organizar equipe de transição",
                "4. Participar da cerimônia de posse",
                "5. Iniciar gestão conforme proposta apresentada"
            };

            return string.Join("\n", passos);
        }
    }
}