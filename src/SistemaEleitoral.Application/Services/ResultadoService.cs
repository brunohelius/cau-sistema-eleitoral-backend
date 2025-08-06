using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces.Repositories;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Domain.Enums;
using System.Text;
using System.Security.Cryptography;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Service responsável pela apuração de resultados eleitorais
    /// </summary>
    public class ResultadoService : IResultadoService
    {
        private readonly IResultadoRepository _resultadoRepository;
        private readonly IVotoRepository _votoRepository;
        private readonly IChapaRepository _chapaRepository;
        private readonly ICalendarioRepository _calendarioRepository;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ResultadoService> _logger;

        public ResultadoService(
            IResultadoRepository resultadoRepository,
            IVotoRepository votoRepository,
            IChapaRepository chapaRepository,
            ICalendarioRepository calendarioRepository,
            INotificationService notificationService,
            ILogger<ResultadoService> logger)
        {
            _resultadoRepository = resultadoRepository;
            _votoRepository = votoRepository;
            _chapaRepository = chapaRepository;
            _calendarioRepository = calendarioRepository;
            _notificationService = notificationService;
            _logger = logger;
        }

        #region Apuração de Votos

        /// <summary>
        /// Inicia processo de apuração
        /// </summary>
        public async Task<ResultadoApuracao> IniciarApuracaoAsync(ApuracaoDTO dto)
        {
            try
            {
                _logger.LogInformation($"Iniciando apuração para calendário {dto.CalendarioId}, UF {dto.UfId}");

                // Validar se votação foi encerrada
                var votacaoEncerrada = await ValidarVotacaoEncerrada(dto.CalendarioId, dto.UfId);
                if (!votacaoEncerrada)
                    throw new InvalidOperationException("Votação ainda não foi encerrada");

                // Criar resultado de apuração
                var resultado = new ResultadoApuracao
                {
                    CalendarioId = dto.CalendarioId,
                    UfId = dto.UfId,
                    DataApuracao = DateTime.Now,
                    StatusApuracao = StatusApuracao.EmAndamento,
                    ResponsavelApuracaoId = dto.ResponsavelId
                };

                await _resultadoRepository.AddAsync(resultado);

                // Processar votos
                await ProcessarVotos(resultado);

                // Calcular resultados
                await CalcularResultados(resultado);

                // Verificar necessidade de segundo turno
                await VerificarSegundoTurno(resultado);

                // Finalizar apuração
                resultado.StatusApuracao = StatusApuracao.Concluida;
                resultado.DataFinalizacao = DateTime.Now;
                await _resultadoRepository.UpdateAsync(resultado);

                // Notificar interessados
                await _notificationService.NotificarResultadoApuracao(resultado.Id);

                _logger.LogInformation($"Apuração concluída: {resultado.Id}");
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar apuração");
                throw;
            }
        }

        /// <summary>
        /// Processa votos computados
        /// </summary>
        private async Task ProcessarVotos(ResultadoApuracao resultado)
        {
            var votos = await _votoRepository.ObterVotosPorSessaoAsync(resultado.CalendarioId, resultado.UfId);
            
            resultado.TotalEleitores = await ObterTotalEleitores(resultado.CalendarioId, resultado.UfId);
            resultado.TotalVotantes = votos.Count;
            resultado.TotalAbstencoes = resultado.TotalEleitores - resultado.TotalVotantes;

            // Contabilizar votos por chapa
            var votosChapa = votos.GroupBy(v => v.ChapaId)
                .Select(g => new VotoChapa
                {
                    ChapaId = g.Key.Value,
                    QuantidadeVotos = g.Count(),
                    ResultadoApuracaoId = resultado.Id
                }).ToList();

            // Contabilizar votos brancos e nulos
            resultado.TotalVotosBrancos = votos.Count(v => v.VotoEmBranco);
            resultado.TotalVotosNulos = votos.Count(v => v.VotoNulo);
            resultado.TotalVotosValidos = resultado.TotalVotantes - resultado.TotalVotosBrancos - resultado.TotalVotosNulos;

            // Calcular percentuais
            if (resultado.TotalEleitores > 0)
            {
                resultado.PercentualParticipacao = (decimal)resultado.TotalVotantes / resultado.TotalEleitores * 100;
                resultado.PercentualAbstencao = (decimal)resultado.TotalAbstencoes / resultado.TotalEleitores * 100;
            }

            _logger.LogInformation($"Votos processados: {resultado.TotalVotantes} votantes de {resultado.TotalEleitores} eleitores");
        }

        /// <summary>
        /// Calcula resultados finais
        /// </summary>
        private async Task CalcularResultados(ResultadoApuracao resultado)
        {
            var votosChapas = await _resultadoRepository.ObterVotosPorChapaAsync(resultado.Id);
            
            // Ordenar por quantidade de votos
            var chapasOrdenadas = votosChapas.OrderByDescending(v => v.QuantidadeVotos).ToList();

            int posicao = 1;
            foreach (var votoChapa in chapasOrdenadas)
            {
                votoChapa.Posicao = posicao++;
                
                // Calcular percentual
                if (resultado.TotalVotosValidos > 0)
                {
                    votoChapa.PercentualVotos = (decimal)votoChapa.QuantidadeVotos / resultado.TotalVotosValidos * 100;
                }

                // Verificar se foi eleita (maioria simples)
                if (votoChapa.Posicao == 1 && votoChapa.PercentualVotos > 50)
                {
                    votoChapa.Eleita = true;
                    resultado.ChapaVencedoraId = votoChapa.ChapaId;
                }

                await _resultadoRepository.UpdateVotoChapaAsync(votoChapa);
            }

            // Gerar hash de integridade
            resultado.HashIntegridade = GerarHashIntegridade(resultado);
            
            _logger.LogInformation($"Resultados calculados. Chapa vencedora: {resultado.ChapaVencedoraId}");
        }

        /// <summary>
        /// Verifica necessidade de segundo turno
        /// </summary>
        private async Task VerificarSegundoTurno(ResultadoApuracao resultado)
        {
            var votosChapas = await _resultadoRepository.ObterVotosPorChapaAsync(resultado.Id);
            var primeiraColocada = votosChapas.OrderByDescending(v => v.QuantidadeVotos).FirstOrDefault();

            if (primeiraColocada == null)
                return;

            // Verificar se precisa de segundo turno (não alcançou maioria absoluta)
            if (primeiraColocada.PercentualVotos <= 50)
            {
                resultado.NecessitaSegundoTurno = true;
                resultado.ChapaVencedoraId = null; // Sem vencedor no primeiro turno

                // Identificar chapas para segundo turno (duas mais votadas)
                var duasPrimeiras = votosChapas
                    .OrderByDescending(v => v.QuantidadeVotos)
                    .Take(2)
                    .Select(v => v.ChapaId)
                    .ToList();

                resultado.ChapasSegundoTurno = string.Join(",", duasPrimeiras);
                
                _logger.LogInformation($"Segundo turno necessário entre chapas: {resultado.ChapasSegundoTurno}");
            }
        }

        #endregion

        #region Recontagem

        /// <summary>
        /// Solicita recontagem de votos
        /// </summary>
        public async Task<bool> SolicitarRecontagemAsync(SolicitarRecontagemDTO dto)
        {
            try
            {
                var resultado = await _resultadoRepository.GetByIdAsync(dto.ResultadoId);
                if (resultado == null)
                    throw new ArgumentException("Resultado não encontrado");

                // Verificar prazo para recontagem (48 horas)
                var prazoLimite = resultado.DataFinalizacao.Value.AddHours(48);
                if (DateTime.Now > prazoLimite)
                    throw new InvalidOperationException("Prazo para solicitação de recontagem expirado");

                // Criar solicitação
                var solicitacao = new SolicitacaoRecontagem
                {
                    ResultadoApuracaoId = dto.ResultadoId,
                    SolicitanteId = dto.SolicitanteId,
                    Motivo = dto.Motivo,
                    DataSolicitacao = DateTime.Now,
                    Status = StatusRecontagem.Pendente
                };

                await _resultadoRepository.AddSolicitacaoRecontagemAsync(solicitacao);

                // Notificar comissão
                await _notificationService.NotificarSolicitacaoRecontagem(solicitacao.Id);

                _logger.LogInformation($"Recontagem solicitada para resultado {dto.ResultadoId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao solicitar recontagem");
                throw;
            }
        }

        /// <summary>
        /// Executa recontagem de votos
        /// </summary>
        public async Task<ResultadoRecontagem> ExecutarRecontagemAsync(ExecutarRecontagemDTO dto)
        {
            try
            {
                var solicitacao = await _resultadoRepository.ObterSolicitacaoRecontagemAsync(dto.SolicitacaoId);
                if (solicitacao == null)
                    throw new ArgumentException("Solicitação não encontrada");

                _logger.LogInformation($"Iniciando recontagem para solicitação {dto.SolicitacaoId}");

                // Criar resultado de recontagem
                var recontagem = new ResultadoRecontagem
                {
                    SolicitacaoRecontagemId = dto.SolicitacaoId,
                    DataInicio = DateTime.Now,
                    ResponsavelId = dto.ResponsavelId
                };

                // Recontar votos
                var resultadoOriginal = await _resultadoRepository.GetByIdAsync(solicitacao.ResultadoApuracaoId);
                var votos = await _votoRepository.ObterVotosPorSessaoAsync(resultadoOriginal.CalendarioId, resultadoOriginal.UfId);

                // Processar recontagem
                recontagem.TotalVotosRecontados = votos.Count;
                recontagem.TotalVotosBrancosRecontados = votos.Count(v => v.VotoEmBranco);
                recontagem.TotalVotosNulosRecontados = votos.Count(v => v.VotoNulo);
                recontagem.TotalVotosValidosRecontados = recontagem.TotalVotosRecontados - 
                    recontagem.TotalVotosBrancosRecontados - recontagem.TotalVotosNulosRecontados;

                // Verificar divergências
                recontagem.HouveDivergencia = 
                    recontagem.TotalVotosRecontados != resultadoOriginal.TotalVotantes ||
                    recontagem.TotalVotosBrancosRecontados != resultadoOriginal.TotalVotosBrancos ||
                    recontagem.TotalVotosNulosRecontados != resultadoOriginal.TotalVotosNulos;

                if (recontagem.HouveDivergencia)
                {
                    recontagem.DescricaoDivergencias = GerarDescricaoDivergencias(resultadoOriginal, recontagem);
                    _logger.LogWarning($"Divergências encontradas na recontagem: {recontagem.DescricaoDivergencias}");
                }

                recontagem.DataFinalizacao = DateTime.Now;
                await _resultadoRepository.AddResultadoRecontagemAsync(recontagem);

                // Atualizar status da solicitação
                solicitacao.Status = StatusRecontagem.Concluida;
                solicitacao.DataConclusao = DateTime.Now;
                await _resultadoRepository.UpdateSolicitacaoRecontagemAsync(solicitacao);

                // Notificar resultado
                await _notificationService.NotificarResultadoRecontagem(recontagem.Id);

                _logger.LogInformation($"Recontagem concluída. Divergências: {recontagem.HouveDivergencia}");
                return recontagem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar recontagem");
                throw;
            }
        }

        #endregion

        #region Homologação

        /// <summary>
        /// Homologa resultado da eleição
        /// </summary>
        public async Task<bool> HomologarResultadoAsync(HomologarResultadoDTO dto)
        {
            try
            {
                var resultado = await _resultadoRepository.GetByIdAsync(dto.ResultadoId);
                if (resultado == null)
                    throw new ArgumentException("Resultado não encontrado");

                if (resultado.StatusApuracao != StatusApuracao.Concluida)
                    throw new InvalidOperationException("Apuração ainda não foi concluída");

                // Verificar se há recontagens pendentes
                var recontagensPendentes = await _resultadoRepository.VerificarRecontagensPendentesAsync(dto.ResultadoId);
                if (recontagensPendentes)
                    throw new InvalidOperationException("Existem solicitações de recontagem pendentes");

                // Homologar
                resultado.Homologado = true;
                resultado.DataHomologacao = DateTime.Now;
                resultado.ResponsavelHomologacaoId = dto.ResponsavelId;
                resultado.ObservacoesHomologacao = dto.Observacoes;

                // Gerar documento de homologação
                resultado.DocumentoHomologacao = await GerarDocumentoHomologacao(resultado);

                await _resultadoRepository.UpdateAsync(resultado);

                // Atualizar status das chapas
                if (resultado.ChapaVencedoraId.HasValue)
                {
                    await AtualizarStatusChapaVencedora(resultado.ChapaVencedoraId.Value);
                }

                // Notificar homologação
                await _notificationService.NotificarHomologacaoResultado(resultado.Id);

                _logger.LogInformation($"Resultado {dto.ResultadoId} homologado");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao homologar resultado");
                throw;
            }
        }

        /// <summary>
        /// Impugna resultado da eleição
        /// </summary>
        public async Task<bool> ImpugnarResultadoAsync(ImpugnarResultadoDTO dto)
        {
            try
            {
                var resultado = await _resultadoRepository.GetByIdAsync(dto.ResultadoId);
                if (resultado == null)
                    throw new ArgumentException("Resultado não encontrado");

                // Criar impugnação
                var impugnacao = new ImpugnacaoResultado
                {
                    ResultadoApuracaoId = dto.ResultadoId,
                    ImpugnanteId = dto.ImpugnanteId,
                    Motivo = dto.Motivo,
                    Fundamentacao = dto.Fundamentacao,
                    DataImpugnacao = DateTime.Now,
                    Status = StatusImpugnacao.EmAnalise
                };

                await _resultadoRepository.AddImpugnacaoResultadoAsync(impugnacao);

                // Suspender homologação
                resultado.Homologado = false;
                resultado.StatusApuracao = StatusApuracao.Impugnada;
                await _resultadoRepository.UpdateAsync(resultado);

                // Notificar impugnação
                await _notificationService.NotificarImpugnacaoResultado(impugnacao.Id);

                _logger.LogInformation($"Resultado {dto.ResultadoId} impugnado");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao impugnar resultado");
                throw;
            }
        }

        #endregion

        #region Publicação e Divulgação

        /// <summary>
        /// Publica resultado oficial
        /// </summary>
        public async Task<bool> PublicarResultadoAsync(PublicarResultadoDTO dto)
        {
            try
            {
                var resultado = await _resultadoRepository.GetByIdAsync(dto.ResultadoId);
                if (resultado == null)
                    throw new ArgumentException("Resultado não encontrado");

                if (!resultado.Homologado)
                    throw new InvalidOperationException("Resultado não foi homologado");

                // Publicar
                resultado.Publicado = true;
                resultado.DataPublicacao = DateTime.Now;
                resultado.ResponsavelPublicacaoId = dto.ResponsavelId;

                // Gerar documentos oficiais
                resultado.AtaApuracao = await GerarAtaApuracao(resultado);
                resultado.BoletimUrna = await GerarBoletimUrna(resultado);
                resultado.RelatorioFinal = await GerarRelatorioFinal(resultado);

                await _resultadoRepository.UpdateAsync(resultado);

                // Divulgar em massa
                await _notificationService.DivulgarResultadoOficial(resultado.Id);

                _logger.LogInformation($"Resultado {dto.ResultadoId} publicado oficialmente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao publicar resultado");
                throw;
            }
        }

        /// <summary>
        /// Obtém resultado para divulgação pública
        /// </summary>
        public async Task<ResultadoDivulgacao> ObterResultadoPublicoAsync(int calendarioId, int? ufId = null)
        {
            try
            {
                var resultado = await _resultadoRepository.ObterResultadoPorCalendarioAsync(calendarioId, ufId);
                if (resultado == null || !resultado.Publicado)
                    return null;

                var divulgacao = new ResultadoDivulgacao
                {
                    CalendarioId = calendarioId,
                    UfId = ufId,
                    DataApuracao = resultado.DataApuracao,
                    TotalEleitores = resultado.TotalEleitores,
                    TotalVotantes = resultado.TotalVotantes,
                    PercentualParticipacao = resultado.PercentualParticipacao,
                    ChapaVencedora = await ObterDadosChapaVencedora(resultado.ChapaVencedoraId),
                    ResultadosPorChapa = await ObterResultadosChapas(resultado.Id),
                    NecessitaSegundoTurno = resultado.NecessitaSegundoTurno
                };

                return divulgacao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter resultado público");
                throw;
            }
        }

        #endregion

        #region Relatórios e Estatísticas

        /// <summary>
        /// Gera relatório estatístico da apuração
        /// </summary>
        public async Task<RelatorioEstatistico> GerarRelatorioEstatisticoAsync(int resultadoId)
        {
            try
            {
                var resultado = await _resultadoRepository.GetByIdAsync(resultadoId);
                if (resultado == null)
                    throw new ArgumentException("Resultado não encontrado");

                var relatorio = new RelatorioEstatistico
                {
                    ResultadoId = resultadoId,
                    DataGeracao = DateTime.Now,
                    
                    // Estatísticas gerais
                    TotalEleitores = resultado.TotalEleitores,
                    TotalVotantes = resultado.TotalVotantes,
                    TotalAbstencoes = resultado.TotalAbstencoes,
                    
                    // Percentuais
                    PercentualParticipacao = resultado.PercentualParticipacao,
                    PercentualAbstencao = resultado.PercentualAbstencao,
                    
                    // Votos
                    TotalVotosValidos = resultado.TotalVotosValidos,
                    TotalVotosBrancos = resultado.TotalVotosBrancos,
                    TotalVotosNulos = resultado.TotalVotosNulos,
                    
                    // Distribuição por hora
                    DistribuicaoPorHora = await ObterDistribuicaoVotosPorHora(resultado),
                    
                    // Distribuição geográfica
                    DistribuicaoGeografica = await ObterDistribuicaoGeografica(resultado)
                };

                _logger.LogInformation($"Relatório estatístico gerado para resultado {resultadoId}");
                return relatorio;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar relatório estatístico");
                throw;
            }
        }

        /// <summary>
        /// Obtém histórico de resultados
        /// </summary>
        public async Task<List<HistoricoResultado>> ObterHistoricoResultadosAsync(int? ufId = null, int quantidade = 10)
        {
            try
            {
                var resultados = await _resultadoRepository.ObterUltimosResultadosAsync(ufId, quantidade);
                
                var historico = resultados.Select(r => new HistoricoResultado
                {
                    CalendarioId = r.CalendarioId,
                    Ano = r.DataApuracao.Year,
                    UfId = r.UfId,
                    TotalVotantes = r.TotalVotantes,
                    PercentualParticipacao = r.PercentualParticipacao,
                    ChapaVencedoraId = r.ChapaVencedoraId,
                    SegundoTurno = r.NecessitaSegundoTurno
                }).ToList();

                return historico;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter histórico de resultados");
                throw;
            }
        }

        #endregion

        #region Métodos Auxiliares

        private async Task<bool> ValidarVotacaoEncerrada(int calendarioId, int ufId)
        {
            // Implementar verificação se votação foi encerrada
            return await Task.FromResult(true);
        }

        private async Task<int> ObterTotalEleitores(int calendarioId, int ufId)
        {
            // Implementar obtenção do total de eleitores aptos
            return await Task.FromResult(1000); // Placeholder
        }

        private string GerarHashIntegridade(ResultadoApuracao resultado)
        {
            var dados = $"{resultado.Id}|{resultado.TotalVotantes}|{resultado.TotalVotosValidos}|" +
                       $"{resultado.TotalVotosBrancos}|{resultado.TotalVotosNulos}|{resultado.ChapaVencedoraId}";
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private string GerarDescricaoDivergencias(ResultadoApuracao original, ResultadoRecontagem recontagem)
        {
            var divergencias = new List<string>();

            if (original.TotalVotantes != recontagem.TotalVotosRecontados)
                divergencias.Add($"Total de votos: Original={original.TotalVotantes}, Recontagem={recontagem.TotalVotosRecontados}");

            if (original.TotalVotosBrancos != recontagem.TotalVotosBrancosRecontados)
                divergencias.Add($"Votos brancos: Original={original.TotalVotosBrancos}, Recontagem={recontagem.TotalVotosBrancosRecontados}");

            if (original.TotalVotosNulos != recontagem.TotalVotosNulosRecontados)
                divergencias.Add($"Votos nulos: Original={original.TotalVotosNulos}, Recontagem={recontagem.TotalVotosNulosRecontados}");

            return string.Join("; ", divergencias);
        }

        private async Task<string> GerarDocumentoHomologacao(ResultadoApuracao resultado)
        {
            // Implementar geração de documento PDF
            return await Task.FromResult($"HOMOLOGACAO_{resultado.Id}_{DateTime.Now:yyyyMMddHHmmss}");
        }

        private async Task AtualizarStatusChapaVencedora(int chapaId)
        {
            var chapa = await _chapaRepository.GetByIdAsync(chapaId);
            if (chapa != null)
            {
                chapa.StatusChapa = StatusChapa.Eleita;
                await _chapaRepository.UpdateAsync(chapa);
            }
        }

        private async Task<string> GerarAtaApuracao(ResultadoApuracao resultado)
        {
            // Implementar geração de ata
            return await Task.FromResult($"ATA_{resultado.Id}");
        }

        private async Task<string> GerarBoletimUrna(ResultadoApuracao resultado)
        {
            // Implementar geração de boletim
            return await Task.FromResult($"BOLETIM_{resultado.Id}");
        }

        private async Task<string> GerarRelatorioFinal(ResultadoApuracao resultado)
        {
            // Implementar geração de relatório
            return await Task.FromResult($"RELATORIO_{resultado.Id}");
        }

        private async Task<object> ObterDadosChapaVencedora(int? chapaId)
        {
            if (!chapaId.HasValue)
                return null;

            var chapa = await _chapaRepository.GetByIdAsync(chapaId.Value);
            return new
            {
                Id = chapa.Id,
                Nome = chapa.NomeChapa,
                Numero = chapa.NumeroChapa
            };
        }

        private async Task<List<object>> ObterResultadosChapas(int resultadoId)
        {
            var votosChapas = await _resultadoRepository.ObterVotosPorChapaAsync(resultadoId);
            
            return votosChapas.Select(v => new
            {
                ChapaId = v.ChapaId,
                QuantidadeVotos = v.QuantidadeVotos,
                PercentualVotos = v.PercentualVotos,
                Posicao = v.Posicao
            }).Cast<object>().ToList();
        }

        private async Task<object> ObterDistribuicaoVotosPorHora(ResultadoApuracao resultado)
        {
            // Implementar análise de distribuição temporal
            return await Task.FromResult(new { });
        }

        private async Task<object> ObterDistribuicaoGeografica(ResultadoApuracao resultado)
        {
            // Implementar análise de distribuição geográfica
            return await Task.FromResult(new { });
        }

        #endregion
    }
}