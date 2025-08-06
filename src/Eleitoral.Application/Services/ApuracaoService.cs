using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Eleitoral.Domain.Entities.Apuracao;
using Eleitoral.Domain.Entities.Eleicoes;
using Eleitoral.Domain.Interfaces.Repositories;
using Eleitoral.Application.Interfaces;
using Eleitoral.Application.DTOs.Apuracao;

namespace Eleitoral.Application.Services
{
    /// <summary>
    /// Serviço para gerenciar a apuração de eleições
    /// </summary>
    public class ApuracaoService : IApuracaoService
    {
        private readonly IResultadoApuracaoRepository _resultadoRepository;
        private readonly IBoletimUrnaRepository _boletimRepository;
        private readonly IEleicaoRepository _eleicaoRepository;
        private readonly IChapaEleicaoRepository _chapaRepository;
        private readonly ILogger<ApuracaoService> _logger;
        private readonly INotificationService _notificationService;
        
        public ApuracaoService(
            IResultadoApuracaoRepository resultadoRepository,
            IBoletimUrnaRepository boletimRepository,
            IEleicaoRepository eleicaoRepository,
            IChapaEleicaoRepository chapaRepository,
            ILogger<ApuracaoService> logger,
            INotificationService notificationService)
        {
            _resultadoRepository = resultadoRepository;
            _boletimRepository = boletimRepository;
            _eleicaoRepository = eleicaoRepository;
            _chapaRepository = chapaRepository;
            _logger = logger;
            _notificationService = notificationService;
        }
        
        /// <summary>
        /// Inicia a apuração de uma eleição
        /// </summary>
        public async Task<ResultadoApuracaoDto> IniciarApuracaoAsync(int eleicaoId)
        {
            try
            {
                _logger.LogInformation($"Iniciando apuração da eleição {eleicaoId}");
                
                // Verificar se já existe apuração em andamento
                if (await _resultadoRepository.ExisteApuracaoEmAndamentoAsync(eleicaoId))
                {
                    throw new InvalidOperationException("Já existe uma apuração em andamento para esta eleição.");
                }
                
                // Obter dados da eleição
                var eleicao = await _eleicaoRepository.GetByIdAsync(eleicaoId);
                if (eleicao == null)
                {
                    throw new ArgumentException($"Eleição {eleicaoId} não encontrada.");
                }
                
                // Obter total de eleitores
                var totalEleitores = await ObterTotalEleitoresAsync(eleicaoId);
                
                // Criar novo resultado de apuração
                var resultado = new ResultadoApuracao(eleicaoId, totalEleitores);
                resultado.IniciarApuracao();
                
                // Salvar resultado
                await _resultadoRepository.SalvarCompletoAsync(resultado);
                
                // Notificar início da apuração
                await _notificationService.NotificarInicioApuracaoAsync(eleicaoId);
                
                _logger.LogInformation($"Apuração da eleição {eleicaoId} iniciada com sucesso");
                
                return MapearParaDto(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao iniciar apuração da eleição {eleicaoId}");
                throw;
            }
        }
        
        /// <summary>
        /// Processa um boletim de urna
        /// </summary>
        public async Task<ResultadoApuracaoDto> ProcessarBoletimUrnaAsync(ProcessarBoletimDto dto)
        {
            try
            {
                _logger.LogInformation($"Processando boletim da urna {dto.NumeroUrna}");
                
                // Obter resultado da apuração
                var resultado = await _resultadoRepository.ObterCompletoAsync(dto.ResultadoApuracaoId);
                if (resultado == null)
                {
                    throw new ArgumentException($"Resultado de apuração {dto.ResultadoApuracaoId} não encontrado.");
                }
                
                // Verificar se boletim já foi processado
                if (await _boletimRepository.BoletimJaProcessadoAsync(dto.NumeroUrna, dto.ResultadoApuracaoId))
                {
                    throw new InvalidOperationException($"Boletim da urna {dto.NumeroUrna} já foi processado.");
                }
                
                // Criar boletim de urna
                var boletim = new BoletimUrna(
                    dto.ResultadoApuracaoId,
                    dto.NumeroUrna,
                    dto.CodigoIdentificacao,
                    dto.LocalVotacao,
                    dto.Secao,
                    dto.Zona,
                    dto.TotalEleitoresUrna,
                    dto.TotalUrnasEleicao
                );
                
                // Registrar votação
                boletim.RegistrarVotacao(
                    dto.DataHoraAbertura,
                    dto.DataHoraEncerramento,
                    dto.TotalVotantes,
                    dto.VotosBrancos,
                    dto.VotosNulos
                );
                
                // Adicionar votos por chapa
                foreach (var votoChapa in dto.VotosChapas)
                {
                    boletim.AdicionarVotoChapa(votoChapa.ChapaId, votoChapa.QuantidadeVotos);
                }
                
                // Processar boletim
                boletim.ProcessarBoletim();
                
                // Salvar boletim
                await _boletimRepository.SalvarComVotosAsync(boletim);
                
                // Atualizar resultado da apuração
                resultado.ProcessarBoletimUrna(boletim);
                await _resultadoRepository.SalvarCompletoAsync(resultado);
                
                // Notificar atualização da apuração
                await _notificationService.NotificarAtualizacaoApuracaoAsync(resultado.Id, resultado.PercentualApuracao);
                
                _logger.LogInformation($"Boletim da urna {dto.NumeroUrna} processado com sucesso");
                
                return MapearParaDto(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao processar boletim da urna {dto.NumeroUrna}");
                throw;
            }
        }
        
        /// <summary>
        /// Obtém o resultado da apuração em tempo real
        /// </summary>
        public async Task<ResultadoApuracaoDto> ObterResultadoTempoRealAsync(int eleicaoId)
        {
            try
            {
                var resultado = await _resultadoRepository.ObterUltimoResultadoAsync(eleicaoId);
                if (resultado == null)
                {
                    return null;
                }
                
                return MapearParaDtoCompleto(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter resultado em tempo real da eleição {eleicaoId}");
                throw;
            }
        }
        
        /// <summary>
        /// Finaliza a apuração
        /// </summary>
        public async Task<ResultadoApuracaoDto> FinalizarApuracaoAsync(int resultadoApuracaoId)
        {
            try
            {
                _logger.LogInformation($"Finalizando apuração {resultadoApuracaoId}");
                
                var resultado = await _resultadoRepository.ObterCompletoAsync(resultadoApuracaoId);
                if (resultado == null)
                {
                    throw new ArgumentException($"Resultado de apuração {resultadoApuracaoId} não encontrado.");
                }
                
                // Finalizar apuração
                resultado.FinalizarApuracao();
                
                // Salvar resultado
                await _resultadoRepository.SalvarCompletoAsync(resultado);
                
                // Notificar fim da apuração
                await _notificationService.NotificarFimApuracaoAsync(resultado.EleicaoId, resultado.Id);
                
                _logger.LogInformation($"Apuração {resultadoApuracaoId} finalizada com sucesso");
                
                return MapearParaDtoCompleto(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao finalizar apuração {resultadoApuracaoId}");
                throw;
            }
        }
        
        /// <summary>
        /// Audita o resultado da apuração
        /// </summary>
        public async Task<ResultadoApuracaoDto> AuditarApuracaoAsync(int resultadoApuracaoId, string auditorId)
        {
            try
            {
                _logger.LogInformation($"Auditando apuração {resultadoApuracaoId}");
                
                var resultado = await _resultadoRepository.ObterCompletoAsync(resultadoApuracaoId);
                if (resultado == null)
                {
                    throw new ArgumentException($"Resultado de apuração {resultadoApuracaoId} não encontrado.");
                }
                
                // Auditar resultado
                resultado.Auditar(auditorId);
                
                // Salvar resultado
                await _resultadoRepository.SalvarCompletoAsync(resultado);
                
                _logger.LogInformation($"Apuração {resultadoApuracaoId} auditada com sucesso");
                
                return MapearParaDto(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao auditar apuração {resultadoApuracaoId}");
                throw;
            }
        }
        
        /// <summary>
        /// Reabre a apuração para correções
        /// </summary>
        public async Task<ResultadoApuracaoDto> ReabrirApuracaoAsync(int resultadoApuracaoId, string motivo)
        {
            try
            {
                _logger.LogInformation($"Reabrindo apuração {resultadoApuracaoId}");
                
                var resultado = await _resultadoRepository.ObterCompletoAsync(resultadoApuracaoId);
                if (resultado == null)
                {
                    throw new ArgumentException($"Resultado de apuração {resultadoApuracaoId} não encontrado.");
                }
                
                // Reabrir apuração
                resultado.Reabrir(motivo);
                
                // Salvar resultado
                await _resultadoRepository.SalvarCompletoAsync(resultado);
                
                // Notificar reabertura
                await _notificationService.NotificarReaberturaApuracaoAsync(resultado.EleicaoId, motivo);
                
                _logger.LogInformation($"Apuração {resultadoApuracaoId} reaberta com sucesso");
                
                return MapearParaDto(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao reabrir apuração {resultadoApuracaoId}");
                throw;
            }
        }
        
        /// <summary>
        /// Obtém as estatísticas da apuração
        /// </summary>
        public async Task<EstatisticasApuracaoDto> ObterEstatisticasAsync(int resultadoApuracaoId)
        {
            try
            {
                var estatisticas = await _resultadoRepository.ObterEstatisticasAsync(resultadoApuracaoId);
                if (estatisticas == null)
                {
                    // Gerar estatísticas se não existirem
                    estatisticas = await GerarEstatisticasAsync(resultadoApuracaoId);
                }
                
                return MapearEstatisticasParaDto(estatisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter estatísticas da apuração {resultadoApuracaoId}");
                throw;
            }
        }
        
        /// <summary>
        /// Obtém os boletins de urna pendentes
        /// </summary>
        public async Task<IEnumerable<BoletimUrnaDto>> ObterBoletinsPendentesAsync()
        {
            try
            {
                var boletins = await _boletimRepository.ObterPendentesAsync();
                return boletins.Select(MapearBoletimParaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter boletins pendentes");
                throw;
            }
        }
        
        /// <summary>
        /// Obtém os logs da apuração
        /// </summary>
        public async Task<IEnumerable<LogApuracaoDto>> ObterLogsApuracaoAsync(int resultadoApuracaoId)
        {
            try
            {
                var logs = await _resultadoRepository.ObterLogsApuracaoAsync(resultadoApuracaoId);
                return logs.Select(MapearLogParaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao obter logs da apuração {resultadoApuracaoId}");
                throw;
            }
        }
        
        /// <summary>
        /// Valida a integridade da apuração
        /// </summary>
        public async Task<bool> ValidarIntegridadeApuracaoAsync(int resultadoApuracaoId)
        {
            try
            {
                var resultado = await _resultadoRepository.ObterCompletoAsync(resultadoApuracaoId);
                if (resultado == null)
                {
                    return false;
                }
                
                // Verificar totais
                var totalVotosChapas = resultado.ResultadosChapas.Sum(r => r.TotalVotos);
                var totalVotosCalculado = totalVotosChapas + resultado.VotosBrancos + resultado.VotosNulos;
                
                if (totalVotosCalculado != resultado.TotalVotantes)
                {
                    _logger.LogWarning($"Inconsistência nos totais da apuração {resultadoApuracaoId}");
                    return false;
                }
                
                // Verificar boletins
                var boletins = await _boletimRepository.ObterProcessadosAsync(resultadoApuracaoId);
                var totalVotosBoletins = boletins.Sum(b => b.TotalVotantes);
                
                if (totalVotosBoletins != resultado.TotalVotantes)
                {
                    _logger.LogWarning($"Inconsistência nos boletins da apuração {resultadoApuracaoId}");
                    return false;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao validar integridade da apuração {resultadoApuracaoId}");
                throw;
            }
        }
        
        // Métodos privados auxiliares
        
        private async Task<int> ObterTotalEleitoresAsync(int eleicaoId)
        {
            // Implementar lógica para obter total de eleitores
            // Por enquanto, retornar um valor fixo para testes
            return await Task.FromResult(10000);
        }
        
        private async Task<EstatisticasApuracao> GerarEstatisticasAsync(int resultadoApuracaoId)
        {
            var resultado = await _resultadoRepository.ObterCompletoAsync(resultadoApuracaoId);
            if (resultado == null)
            {
                return null;
            }
            
            var estatisticas = new EstatisticasApuracao(
                resultadoApuracaoId,
                resultado.TotalEleitores,
                resultado.BoletinsUrna.Count
            );
            
            estatisticas.AtualizarEstatisticasVotacao(
                resultado.TotalVotantes,
                resultado.VotosValidos,
                resultado.VotosBrancos,
                resultado.VotosNulos
            );
            
            var (total, processados, pendentes, rejeitados) = 
                await _boletimRepository.ObterEstatisticasAsync(resultadoApuracaoId);
            
            estatisticas.AtualizarEstatisticasUrnas(processados, rejeitados);
            
            return estatisticas;
        }
        
        // Métodos de mapeamento
        
        private ResultadoApuracaoDto MapearParaDto(ResultadoApuracao resultado)
        {
            return new ResultadoApuracaoDto
            {
                Id = resultado.Id,
                EleicaoId = resultado.EleicaoId,
                InicioApuracao = resultado.InicioApuracao,
                FimApuracao = resultado.FimApuracao,
                TotalEleitores = resultado.TotalEleitores,
                TotalVotantes = resultado.TotalVotantes,
                VotosBrancos = resultado.VotosBrancos,
                VotosNulos = resultado.VotosNulos,
                VotosValidos = resultado.VotosValidos,
                PercentualParticipacao = resultado.PercentualParticipacao,
                PercentualApuracao = resultado.PercentualApuracao,
                Status = resultado.Status.ToString(),
                Auditado = resultado.Auditado,
                DataAuditoria = resultado.DataAuditoria,
                HashApuracao = resultado.HashApuracao
            };
        }
        
        private ResultadoApuracaoDto MapearParaDtoCompleto(ResultadoApuracao resultado)
        {
            var dto = MapearParaDto(resultado);
            
            dto.ResultadosChapas = resultado.ResultadosChapas
                .Select(r => new ResultadoChapaDto
                {
                    Id = r.Id,
                    ChapaId = r.ChapaId,
                    NomeChapa = r.Chapa?.Nome,
                    NumeroChapa = r.Chapa?.Numero ?? 0,
                    TotalVotos = r.TotalVotos,
                    PercentualVotos = r.PercentualVotos,
                    Posicao = r.Posicao,
                    Eleita = r.Eleita
                })
                .OrderByDescending(r => r.TotalVotos)
                .ToList();
                
            return dto;
        }
        
        private BoletimUrnaDto MapearBoletimParaDto(BoletimUrna boletim)
        {
            return new BoletimUrnaDto
            {
                Id = boletim.Id,
                NumeroUrna = boletim.NumeroUrna,
                CodigoIdentificacao = boletim.CodigoIdentificacao,
                LocalVotacao = boletim.LocalVotacao,
                Secao = boletim.Secao,
                Zona = boletim.Zona,
                TotalEleitoresUrna = boletim.TotalEleitoresUrna,
                TotalVotantes = boletim.TotalVotantes,
                VotosBrancos = boletim.VotosBrancos,
                VotosNulos = boletim.VotosNulos,
                Status = boletim.Status.ToString(),
                Conferido = boletim.Conferido,
                DataHoraProcessamento = boletim.DataHoraProcessamento
            };
        }
        
        private LogApuracaoDto MapearLogParaDto(LogApuracao log)
        {
            return new LogApuracaoDto
            {
                Id = log.Id,
                DataHora = log.DataHora,
                Descricao = log.Descricao,
                Tipo = log.Tipo.ToString(),
                Usuario = log.Usuario,
                IpOrigem = log.IpOrigem,
                Observacoes = log.Observacoes
            };
        }
        
        private EstatisticasApuracaoDto MapearEstatisticasParaDto(EstatisticasApuracao estatisticas)
        {
            if (estatisticas == null) return null;
            
            return new EstatisticasApuracaoDto
            {
                TotalEleitoresAptos = estatisticas.TotalEleitoresAptos,
                TotalComparecimento = estatisticas.TotalComparecimento,
                TotalAbstencoes = estatisticas.TotalAbstencoes,
                PercentualComparecimento = estatisticas.PercentualComparecimento,
                PercentualAbstencao = estatisticas.PercentualAbstencao,
                VotosValidos = estatisticas.VotosValidos,
                VotosBrancos = estatisticas.VotosBrancos,
                VotosNulos = estatisticas.VotosNulos,
                PercentualVotosValidos = estatisticas.PercentualVotosValidos,
                PercentualVotosBrancos = estatisticas.PercentualVotosBrancos,
                PercentualVotosNulos = estatisticas.PercentualVotosNulos,
                TotalUrnas = estatisticas.TotalUrnas,
                UrnasProcessadas = estatisticas.UrnasProcessadas,
                UrnasPendentes = estatisticas.UrnasPendentes,
                UrnasComProblema = estatisticas.UrnasComProblema,
                PercentualUrnasProcessadas = estatisticas.PercentualUrnasProcessadas,
                DataGeracao = estatisticas.DataGeracao,
                UltimaAtualizacao = estatisticas.UltimaAtualizacao
            };
        }
    }
}