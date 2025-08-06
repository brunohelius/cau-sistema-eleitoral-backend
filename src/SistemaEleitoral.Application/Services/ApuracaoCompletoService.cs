using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Application.Interfaces;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Entities.Votacao;
using SistemaEleitoral.Domain.Interfaces;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Service completo para apuração de votos e cálculo de resultados
    /// </summary>
    public class ApuracaoCompletoService : IApuracaoService
    {
        private readonly ILogger<ApuracaoCompletoService> _logger;
        private readonly IVotoRepository _votoRepository;
        private readonly IChapaEleicaoRepository _chapaRepository;
        private readonly IEleicaoRepository _eleicaoRepository;
        private readonly IResultadoApuracaoRepository _resultadoRepository;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;

        public ApuracaoCompletoService(
            ILogger<ApuracaoCompletoService> logger,
            IVotoRepository votoRepository,
            IChapaEleicaoRepository chapaRepository,
            IEleicaoRepository eleicaoRepository,
            IResultadoApuracaoRepository resultadoRepository,
            IEmailService emailService,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _votoRepository = votoRepository;
            _chapaRepository = chapaRepository;
            _eleicaoRepository = eleicaoRepository;
            _resultadoRepository = resultadoRepository;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ResultadoApuracao> IniciarApuracaoAsync(int eleicaoId)
        {
            try
            {
                _logger.LogInformation($"Iniciando apuração da eleição {eleicaoId}");

                var eleicao = await _eleicaoRepository.GetByIdComChapasAsync(eleicaoId);
                if (eleicao == null)
                    throw new ArgumentException("Eleição não encontrada");

                if (eleicao.Status != StatusEleicao.VotacaoFechada)
                    throw new InvalidOperationException("Votação deve estar fechada para iniciar apuração");

                // Criar resultado de apuração
                var resultado = new ResultadoApuracao
                {
                    EleicaoId = eleicaoId,
                    DataInicioApuracao = DateTime.Now,
                    Status = StatusApuracao.EmAndamento
                };

                // Processar votos
                await ProcessarVotosAsync(resultado, eleicao);

                // Calcular resultados
                await CalcularResultadosAsync(resultado, eleicao);

                // Determinar eleitos
                await DeterminarEleitosAsync(resultado, eleicao);

                // Finalizar apuração
                resultado.DataFimApuracao = DateTime.Now;
                resultado.Status = StatusApuracao.Concluida;

                await _resultadoRepository.AddAsync(resultado);
                await _unitOfWork.CommitAsync();

                // Atualizar status da eleição
                eleicao.Status = StatusEleicao.EmApuracao;
                await _eleicaoRepository.UpdateAsync(eleicao);
                await _unitOfWork.CommitAsync();

                // Notificar conclusão
                await NotificarConclusaoApuracao(resultado);

                _logger.LogInformation($"Apuração concluída para eleição {eleicaoId}");
                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro na apuração da eleição {eleicaoId}");
                throw;
            }
        }

        private async Task ProcessarVotosAsync(ResultadoApuracao resultado, Eleicao eleicao)
        {
            _logger.LogInformation("Processando votos...");

            // Obter todos os votos da eleição
            var votos = await _votoRepository.GetVotosPorEleicaoAsync(eleicao.Id);
            
            resultado.TotalEleitoresAptos = await _votoRepository.GetTotalEleitoresAptosAsync(eleicao.Id);
            resultado.TotalVotos = votos.Count();
            resultado.VotosBrancos = votos.Count(v => v.VotoBranco);
            resultado.VotosNulos = votos.Count(v => v.VotoNulo);
            resultado.VotosValidos = resultado.TotalVotos - resultado.VotosBrancos - resultado.VotosNulos;

            // Calcular participação
            if (resultado.TotalEleitoresAptos > 0)
            {
                resultado.PercentualParticipacao = 
                    (decimal)resultado.TotalVotos / resultado.TotalEleitoresAptos * 100;
                resultado.PercentualAbstencao = 100 - resultado.PercentualParticipacao;
            }

            // Verificar quorum
            resultado.QuorumAtingido = resultado.PercentualParticipacao >= eleicao.QuorumMinimo;

            _logger.LogInformation($"Total de votos processados: {resultado.TotalVotos}");
        }

        private async Task CalcularResultadosAsync(ResultadoApuracao resultado, Eleicao eleicao)
        {
            _logger.LogInformation("Calculando resultados por chapa...");

            var chapas = eleicao.Chapas.Where(c => c.Status == StatusChapa.Homologada).ToList();
            var resultadosChapas = new List<ResultadoChapa>();

            foreach (var chapa in chapas)
            {
                var votosChapa = await _votoRepository.GetVotosPorChapaAsync(chapa.Id);
                var totalVotosChapa = votosChapa.Count();

                var resultadoChapa = new ResultadoChapa
                {
                    ResultadoApuracaoId = resultado.Id,
                    ChapaEleicaoId = chapa.Id,
                    TotalVotos = totalVotosChapa,
                    PercentualVotos = resultado.VotosValidos > 0 
                        ? (decimal)totalVotosChapa / resultado.VotosValidos * 100 
                        : 0,
                    PercentualVotosTotal = resultado.TotalVotos > 0
                        ? (decimal)totalVotosChapa / resultado.TotalVotos * 100
                        : 0
                };

                // Análise regional (se aplicável)
                await CalcularVotosPorRegiaoAsync(resultadoChapa, chapa.Id);

                resultadosChapas.Add(resultadoChapa);
            }

            // Ordenar por votos e definir classificação
            var chapasOrdenadas = resultadosChapas.OrderByDescending(r => r.TotalVotos).ToList();
            for (int i = 0; i < chapasOrdenadas.Count; i++)
            {
                chapasOrdenadas[i].Classificacao = i + 1;
            }

            resultado.ResultadosChapas = resultadosChapas;

            _logger.LogInformation($"Resultados calculados para {chapas.Count} chapas");
        }

        private async Task DeterminarEleitosAsync(ResultadoApuracao resultado, Eleicao eleicao)
        {
            _logger.LogInformation("Determinando eleitos...");

            if (!resultado.QuorumAtingido)
            {
                _logger.LogWarning("Quorum não atingido - nenhuma chapa eleita");
                resultado.Observacoes = "Eleição não atingiu o quorum mínimo necessário";
                return;
            }

            var chapasOrdenadas = resultado.ResultadosChapas
                .OrderByDescending(r => r.TotalVotos)
                .ToList();

            // Verificar empate
            if (chapasOrdenadas.Count >= 2 && 
                chapasOrdenadas[0].TotalVotos == chapasOrdenadas[1].TotalVotos)
            {
                resultado.HouveEmpate = true;
                resultado.Observacoes = "Houve empate entre as chapas mais votadas";
                // Aplicar critério de desempate
                await AplicarCriterioDesempateAsync(chapasOrdenadas, eleicao);
            }

            // Marcar eleitos
            var vagas = eleicao.NumeroVagas;
            for (int i = 0; i < Math.Min(vagas, chapasOrdenadas.Count); i++)
            {
                chapasOrdenadas[i].Eleita = true;
                chapasOrdenadas[i].TipoEleicao = i == 0 ? "Titular" : $"Suplente {i}";
                
                // Atualizar status da chapa
                var chapa = await _chapaRepository.GetByIdAsync(chapasOrdenadas[i].ChapaEleicaoId);
                if (chapa != null)
                {
                    chapa.Status = StatusChapa.Eleita;
                    chapa.Eleita = true;
                    chapa.Classificacao = i + 1;
                    await _chapaRepository.UpdateAsync(chapa);
                }
            }

            // Marcar suplentes
            var suplentes = eleicao.NumeroSuplentes;
            for (int i = vagas; i < Math.Min(vagas + suplentes, chapasOrdenadas.Count); i++)
            {
                chapasOrdenadas[i].Suplente = true;
                chapasOrdenadas[i].OrdemSuplencia = i - vagas + 1;
            }

            _logger.LogInformation($"Eleitos determinados: {vagas} titulares, {suplentes} suplentes");
        }

        private async Task CalcularVotosPorRegiaoAsync(ResultadoChapa resultado, int chapaId)
        {
            // Implementar cálculo de votos por região/filial
            var votosPorRegiao = await _votoRepository.GetVotosPorRegiaoAsync(chapaId);
            
            resultado.VotosPorRegiao = votosPorRegiao.Select(v => new VotoRegional
            {
                RegiaoId = v.Key,
                NomeRegiao = v.Value.Nome,
                TotalVotos = v.Value.Total,
                PercentualRegional = v.Value.Percentual
            }).ToList();
        }

        private async Task AplicarCriterioDesempateAsync(List<ResultadoChapa> chapas, Eleicao eleicao)
        {
            // Implementar critérios de desempate
            // Ex: idade do candidato principal, tempo de registro profissional, etc.
            await Task.CompletedTask;
        }

        public async Task<ResultadoApuracao> RecalcularResultadosAsync(int eleicaoId)
        {
            try
            {
                _logger.LogInformation($"Recalculando resultados da eleição {eleicaoId}");

                // Invalidar resultado anterior
                var resultadoAnterior = await _resultadoRepository.GetByEleicaoIdAsync(eleicaoId);
                if (resultadoAnterior != null)
                {
                    resultadoAnterior.Status = StatusApuracao.Invalidada;
                    await _resultadoRepository.UpdateAsync(resultadoAnterior);
                }

                // Iniciar nova apuração
                return await IniciarApuracaoAsync(eleicaoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao recalcular resultados da eleição {eleicaoId}");
                throw;
            }
        }

        public async Task<bool> HomologarResultadoAsync(int resultadoId, int responsavelId, string parecer)
        {
            try
            {
                var resultado = await _resultadoRepository.GetByIdAsync(resultadoId);
                if (resultado == null)
                    throw new ArgumentException("Resultado não encontrado");

                if (resultado.Status != StatusApuracao.Concluida)
                    throw new InvalidOperationException("Apuração deve estar concluída para homologação");

                resultado.Status = StatusApuracao.Homologada;
                resultado.DataHomologacao = DateTime.Now;
                resultado.ResponsavelHomologacaoId = responsavelId;
                resultado.ParecerHomologacao = parecer;

                await _resultadoRepository.UpdateAsync(resultado);

                // Atualizar eleição
                var eleicao = await _eleicaoRepository.GetByIdAsync(resultado.EleicaoId);
                if (eleicao != null)
                {
                    eleicao.Status = StatusEleicao.Finalizada;
                    await _eleicaoRepository.UpdateAsync(eleicao);
                }

                await _unitOfWork.CommitAsync();

                // Notificar homologação
                await NotificarHomologacaoResultado(resultado);

                _logger.LogInformation($"Resultado {resultadoId} homologado");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao homologar resultado {resultadoId}");
                throw;
            }
        }

        public async Task<bool> ImpugnarResultadoAsync(int resultadoId, string motivo, int impugnanteId)
        {
            try
            {
                var resultado = await _resultadoRepository.GetByIdAsync(resultadoId);
                if (resultado == null)
                    throw new ArgumentException("Resultado não encontrado");

                resultado.Status = StatusApuracao.Impugnada;
                resultado.MotivoImpugnacao = motivo;
                resultado.DataImpugnacao = DateTime.Now;
                resultado.ImpugnanteId = impugnanteId;

                await _resultadoRepository.UpdateAsync(resultado);
                await _unitOfWork.CommitAsync();

                // Criar processo de impugnação
                await CriarProcessoImpugnacaoAsync(resultado, motivo, impugnanteId);

                _logger.LogInformation($"Resultado {resultadoId} impugnado");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao impugnar resultado {resultadoId}");
                throw;
            }
        }

        public async Task<RelatorioApuracao> GerarRelatorioOficialAsync(int resultadoId)
        {
            try
            {
                var resultado = await _resultadoRepository.GetCompletoAsync(resultadoId);
                if (resultado == null)
                    throw new ArgumentException("Resultado não encontrado");

                var relatorio = new RelatorioApuracao
                {
                    ResultadoId = resultadoId,
                    DataGeracao = DateTime.Now,
                    TituloEleicao = resultado.Eleicao.Titulo,
                    DataEleicao = resultado.Eleicao.DataInicioVotacao,
                    
                    // Estatísticas gerais
                    TotalEleitoresAptos = resultado.TotalEleitoresAptos,
                    TotalVotos = resultado.TotalVotos,
                    VotosBrancos = resultado.VotosBrancos,
                    VotosNulos = resultado.VotosNulos,
                    VotosValidos = resultado.VotosValidos,
                    PercentualParticipacao = resultado.PercentualParticipacao,
                    PercentualAbstencao = resultado.PercentualAbstencao,
                    QuorumAtingido = resultado.QuorumAtingido,
                    
                    // Resultados por chapa
                    ResultadosChapas = resultado.ResultadosChapas.Select(r => new ResultadoChapaRelatorio
                    {
                        NomeChapa = r.ChapaEleicao?.Nome,
                        NumeroChapa = r.ChapaEleicao?.Numero ?? 0,
                        TotalVotos = r.TotalVotos,
                        PercentualVotos = r.PercentualVotos,
                        Classificacao = r.Classificacao,
                        Eleita = r.Eleita,
                        Suplente = r.Suplente
                    }).OrderBy(r => r.Classificacao).ToList(),
                    
                    // Assinaturas
                    AssinadoPor = "Comissão Eleitoral",
                    CargoAssinante = "Presidente da Comissão"
                };

                _logger.LogInformation($"Relatório oficial gerado para resultado {resultadoId}");
                return relatorio;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar relatório do resultado {resultadoId}");
                throw;
            }
        }

        private async Task CriarProcessoImpugnacaoAsync(ResultadoApuracao resultado, string motivo, int impugnanteId)
        {
            // Criar processo de impugnação de resultado
            // Implementar integração com módulo de impugnações
            await Task.CompletedTask;
        }

        private async Task NotificarConclusaoApuracao(ResultadoApuracao resultado)
        {
            await _emailService.EnviarEmailAsync(
                "comissao@cau.gov.br",
                "Apuração Concluída",
                $"A apuração da eleição foi concluída. Total de votos: {resultado.TotalVotos}");
        }

        private async Task NotificarHomologacaoResultado(ResultadoApuracao resultado)
        {
            await _emailService.EnviarEmailAsync(
                "todos@cau.gov.br",
                "Resultado Homologado",
                "O resultado da eleição foi homologado e está disponível para consulta.");
        }
    }

    public class RelatorioApuracao
    {
        public int ResultadoId { get; set; }
        public DateTime DataGeracao { get; set; }
        public string TituloEleicao { get; set; }
        public DateTime DataEleicao { get; set; }
        
        public int TotalEleitoresAptos { get; set; }
        public int TotalVotos { get; set; }
        public int VotosBrancos { get; set; }
        public int VotosNulos { get; set; }
        public int VotosValidos { get; set; }
        public decimal PercentualParticipacao { get; set; }
        public decimal PercentualAbstencao { get; set; }
        public bool QuorumAtingido { get; set; }
        
        public List<ResultadoChapaRelatorio> ResultadosChapas { get; set; }
        
        public string AssinadoPor { get; set; }
        public string CargoAssinante { get; set; }
    }

    public class ResultadoChapaRelatorio
    {
        public string NomeChapa { get; set; }
        public int NumeroChapa { get; set; }
        public int TotalVotos { get; set; }
        public decimal PercentualVotos { get; set; }
        public int Classificacao { get; set; }
        public bool Eleita { get; set; }
        public bool Suplente { get; set; }
    }

    public class VotoRegional
    {
        public int RegiaoId { get; set; }
        public string NomeRegiao { get; set; }
        public int TotalVotos { get; set; }
        public decimal PercentualRegional { get; set; }
    }

    public enum StatusApuracao
    {
        NaoIniciada = 1,
        EmAndamento = 2,
        Concluida = 3,
        Homologada = 4,
        Impugnada = 5,
        Invalidada = 6,
        Recontagem = 7
    }
}