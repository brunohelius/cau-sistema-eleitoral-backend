using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SistemaEleitoral.Application.Interfaces;
using SistemaEleitoral.Domain.Entities.Julgamento;
using SistemaEleitoral.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Service que gerencia o workflow completo de julgamentos
    /// </summary>
    public class JulgamentoWorkflowService : IJulgamentoWorkflowService
    {
        private readonly ILogger<JulgamentoWorkflowService> _logger;
        private readonly IEmailService _emailService;
        private readonly INotificacaoService _notificacaoService;
        private readonly IUnitOfWork _unitOfWork;
        
        // Repositories
        private readonly IRepository<JulgamentoAdmissibilidade> _admissibilidadeRepository;
        private readonly IRepository<RecursoEleitoral> _recursoRepository;
        private readonly IRepository<JulgamentoRecurso> _julgamentoRepository;
        private readonly IRepository<ContrarrazaoRecurso> _contrarrazaoRepository;
        private readonly IMembroComissaoRepository _membroComissaoRepository;

        public JulgamentoWorkflowService(
            ILogger<JulgamentoWorkflowService> logger,
            IEmailService emailService,
            INotificacaoService notificacaoService,
            IUnitOfWork unitOfWork,
            IRepository<JulgamentoAdmissibilidade> admissibilidadeRepository,
            IRepository<RecursoEleitoral> recursoRepository,
            IRepository<JulgamentoRecurso> julgamentoRepository,
            IRepository<ContrarrazaoRecurso> contrarrazaoRepository,
            IMembroComissaoRepository membroComissaoRepository)
        {
            _logger = logger;
            _emailService = emailService;
            _notificacaoService = notificacaoService;
            _unitOfWork = unitOfWork;
            _admissibilidadeRepository = admissibilidadeRepository;
            _recursoRepository = recursoRepository;
            _julgamentoRepository = julgamentoRepository;
            _contrarrazaoRepository = contrarrazaoRepository;
            _membroComissaoRepository = membroComissaoRepository;
        }

        #region Admissibilidade

        public async Task<JulgamentoAdmissibilidade> CriarJulgamentoAdmissibilidadeAsync(
            TipoProcessoJulgamento tipoProcesso, 
            int processoId,
            int relatorId)
        {
            try
            {
                var relator = await _membroComissaoRepository.GetByIdAsync(relatorId);
                if (relator == null)
                    throw new ArgumentException("Relator não encontrado");

                var julgamento = new JulgamentoAdmissibilidade
                {
                    TipoProcesso = tipoProcesso,
                    ProcessoId = processoId,
                    RelatorId = relatorId,
                    PrazoAnalise = DateTime.Now.AddDays(GetPrazoPorTipo(tipoProcesso)),
                    Status = StatusJulgamento.PendenteAnalise
                };

                await _admissibilidadeRepository.AddAsync(julgamento);
                await _unitOfWork.CommitAsync();

                // Notificar relator
                await NotificarRelatorDesignado(relator, julgamento);

                _logger.LogInformation($"Julgamento de admissibilidade criado: {julgamento.Id}");
                return julgamento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar julgamento de admissibilidade");
                throw;
            }
        }

        public async Task<JulgamentoAdmissibilidade> JulgarAdmissibilidadeAsync(
            int julgamentoId,
            ResultadoAdmissibilidade resultado,
            string fundamentacao,
            bool tempestividade,
            bool legitimidade,
            bool interesse,
            bool requisitosFormal)
        {
            try
            {
                var julgamento = await _admissibilidadeRepository.GetByIdAsync(julgamentoId);
                if (julgamento == null)
                    throw new ArgumentException("Julgamento não encontrado");

                julgamento.Tempestividade = tempestividade;
                julgamento.Legitimidade = legitimidade;
                julgamento.Interesse = interesse;
                julgamento.RequisitosFormal = requisitosFormal;
                julgamento.AnaliseRequisitos = GerarAnaliseRequisitos(tempestividade, legitimidade, interesse, requisitosFormal);

                if (resultado == ResultadoAdmissibilidade.Admitido)
                {
                    julgamento.Admitir(fundamentacao);
                    await ProcessarAdmissao(julgamento);
                }
                else
                {
                    julgamento.NaoAdmitir(fundamentacao);
                    await ProcessarNaoAdmissao(julgamento);
                }

                await _admissibilidadeRepository.UpdateAsync(julgamento);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation($"Admissibilidade julgada: {julgamento.Id} - Resultado: {resultado}");
                return julgamento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao julgar admissibilidade");
                throw;
            }
        }

        #endregion

        #region Recursos

        public async Task<RecursoEleitoral> InterporRecursoAsync(
            TipoRecurso tipoRecurso,
            int processoOrigemId,
            int recorrenteId,
            string fundamentacao,
            string pedido,
            DateTime prazoFinal)
        {
            try
            {
                var recurso = new RecursoEleitoral
                {
                    TipoRecurso = tipoRecurso,
                    ProcessoOrigemId = processoOrigemId,
                    RecorrenteId = recorrenteId,
                    Fundamentacao = fundamentacao,
                    PedidoRecurso = pedido,
                    PrazoFinal = prazoFinal,
                    NumeroProcesso = GerarNumeroProcesso(tipoRecurso)
                };

                recurso.VerificarTempestividade();

                await _recursoRepository.AddAsync(recurso);
                await _unitOfWork.CommitAsync();

                // Criar julgamento de admissibilidade
                if (recurso.Tempestivo)
                {
                    var relator = await DesignarRelatorAsync();
                    await CriarJulgamentoAdmissibilidadeAsync(
                        TipoProcessoJulgamento.RecursoImpugnacao,
                        recurso.Id,
                        relator.Id);
                }

                // Notificar partes
                await NotificarInterposicaoRecurso(recurso);

                _logger.LogInformation($"Recurso interposto: {recurso.NumeroProcesso}");
                return recurso;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao interpor recurso");
                throw;
            }
        }

        public async Task<ContrarrazaoRecurso> ApresentarContrarrazoesAsync(
            int recursoId,
            int autorId,
            string argumentacao,
            string pedido)
        {
            try
            {
                var recurso = await _recursoRepository.GetByIdAsync(recursoId);
                if (recurso == null)
                    throw new ArgumentException("Recurso não encontrado");

                if (recurso.Status != StatusRecurso.AguardandoContrarrazoes)
                    throw new InvalidOperationException("Recurso não está aguardando contrarrazões");

                var contrarrazao = new ContrarrazaoRecurso
                {
                    RecursoEleitoralId = recursoId,
                    AutorId = autorId,
                    Argumentacao = argumentacao,
                    PedidoContrarrazao = pedido,
                    Tempestiva = DateTime.Now <= recurso.PrazoFinal.AddDays(5) // Prazo de 5 dias para contrarrazões
                };

                await _contrarrazaoRepository.AddAsync(contrarrazao);
                
                // Atualizar status do recurso
                recurso.Status = StatusRecurso.AguardandoJulgamento;
                await _recursoRepository.UpdateAsync(recurso);
                
                await _unitOfWork.CommitAsync();

                // Notificar partes
                await NotificarContrarrazoesApresentadas(contrarrazao);

                _logger.LogInformation($"Contrarrazões apresentadas para recurso: {recurso.NumeroProcesso}");
                return contrarrazao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao apresentar contrarrazões");
                throw;
            }
        }

        #endregion

        #region Julgamento de Recursos

        public async Task<JulgamentoRecurso> JulgarRecursoAsync(
            int recursoId,
            int relatorId,
            ResultadoJulgamentoRecurso resultado,
            string acordao,
            string ementa,
            string votoRelator,
            int votosFavoraveis,
            int votosContrarios)
        {
            try
            {
                var recurso = await _recursoRepository.GetByIdAsync(recursoId);
                if (recurso == null)
                    throw new ArgumentException("Recurso não encontrado");

                if (recurso.Status != StatusRecurso.AguardandoJulgamento)
                    throw new InvalidOperationException("Recurso não está pronto para julgamento");

                var julgamento = new JulgamentoRecurso
                {
                    RecursoEleitoralId = recursoId,
                    RelatorId = relatorId,
                    Acordao = acordao,
                    Ementa = ementa,
                    VotoRelator = votoRelator,
                    VotosFavoraveis = votosFavoraveis,
                    VotosContrarios = votosContrarios
                };

                julgamento.ProferirDecisao(resultado, acordao);

                await _julgamentoRepository.AddAsync(julgamento);
                await _recursoRepository.UpdateAsync(recurso);
                await _unitOfWork.CommitAsync();

                // Processar efeitos do julgamento
                await ProcessarEfeitosJulgamento(julgamento);

                // Notificar partes
                await NotificarResultadoJulgamento(julgamento);

                _logger.LogInformation($"Recurso julgado: {recurso.NumeroProcesso} - Resultado: {resultado}");
                return julgamento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao julgar recurso");
                throw;
            }
        }

        public async Task<VotoJulgamentoRecurso> RegistrarVotoAsync(
            int julgamentoId,
            int membroId,
            TipoVoto tipoVoto,
            string fundamentacao,
            bool divergente = false)
        {
            try
            {
                var julgamento = await _julgamentoRepository.GetByIdAsync(julgamentoId);
                if (julgamento == null)
                    throw new ArgumentException("Julgamento não encontrado");

                var voto = new VotoJulgamentoRecurso
                {
                    JulgamentoRecursoId = julgamentoId,
                    MembroComissaoId = membroId,
                    TipoVoto = tipoVoto,
                    Fundamentacao = fundamentacao,
                    VotoDivergente = divergente,
                    DataVoto = DateTime.Now
                };

                julgamento.Votos.Add(voto);
                
                // Atualizar contagem de votos
                if (tipoVoto == TipoVoto.Favoravel)
                    julgamento.VotosFavoraveis++;
                else if (tipoVoto == TipoVoto.Contrario)
                    julgamento.VotosContrarios++;
                else if (tipoVoto == TipoVoto.Abstencao)
                    julgamento.Abstencoes++;

                await _julgamentoRepository.UpdateAsync(julgamento);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation($"Voto registrado no julgamento: {julgamentoId}");
                return voto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar voto");
                throw;
            }
        }

        #endregion

        #region Métodos Auxiliares

        private int GetPrazoPorTipo(TipoProcessoJulgamento tipo)
        {
            return tipo switch
            {
                TipoProcessoJulgamento.Denuncia => 10,
                TipoProcessoJulgamento.RecursoImpugnacao => 5,
                TipoProcessoJulgamento.RecursoDenuncia => 5,
                TipoProcessoJulgamento.RecursoResultado => 3,
                TipoProcessoJulgamento.PedidoSubstituicao => 7,
                _ => 5
            };
        }

        private string GerarAnaliseRequisitos(bool tempestividade, bool legitimidade, bool interesse, bool requisitos)
        {
            var analise = "ANÁLISE DOS REQUISITOS DE ADMISSIBILIDADE:\n\n";
            analise += $"1. Tempestividade: {(tempestividade ? "CUMPRIDO" : "NÃO CUMPRIDO")}\n";
            analise += $"2. Legitimidade: {(legitimidade ? "PRESENTE" : "AUSENTE")}\n";
            analise += $"3. Interesse: {(interesse ? "DEMONSTRADO" : "NÃO DEMONSTRADO")}\n";
            analise += $"4. Requisitos Formais: {(requisitos ? "ATENDIDOS" : "NÃO ATENDIDOS")}\n";
            return analise;
        }

        private string GerarNumeroProcesso(TipoRecurso tipo)
        {
            var ano = DateTime.Now.Year;
            var sequencial = new Random().Next(1000, 9999);
            var prefixo = tipo switch
            {
                TipoRecurso.Ordinario => "RO",
                TipoRecurso.Especial => "RE",
                TipoRecurso.Extraordinario => "REX",
                TipoRecurso.Agravo => "AG",
                TipoRecurso.Embargos => "ED",
                _ => "REC"
            };
            return $"{prefixo}-{sequencial}/{ano}";
        }

        private async Task<MembroComissao> DesignarRelatorAsync()
        {
            // Lógica para designar relator (distribuição por sorteio ou rodízio)
            var membros = await _membroComissaoRepository.GetAtivosAsync();
            var random = new Random();
            return membros.ElementAt(random.Next(membros.Count()));
        }

        private async Task ProcessarAdmissao(JulgamentoAdmissibilidade julgamento)
        {
            // Processar admissão baseado no tipo de processo
            switch (julgamento.TipoProcesso)
            {
                case TipoProcessoJulgamento.RecursoImpugnacao:
                    var recurso = await _recursoRepository.GetByIdAsync(julgamento.ProcessoId);
                    if (recurso != null)
                    {
                        recurso.Status = StatusRecurso.Admitido;
                        await _recursoRepository.UpdateAsync(recurso);
                    }
                    break;
                // Outros casos...
            }
        }

        private async Task ProcessarNaoAdmissao(JulgamentoAdmissibilidade julgamento)
        {
            // Processar não admissão
            switch (julgamento.TipoProcesso)
            {
                case TipoProcessoJulgamento.RecursoImpugnacao:
                    var recurso = await _recursoRepository.GetByIdAsync(julgamento.ProcessoId);
                    if (recurso != null)
                    {
                        recurso.Status = StatusRecurso.NaoAdmitido;
                        await _recursoRepository.UpdateAsync(recurso);
                    }
                    break;
                // Outros casos...
            }
        }

        private async Task ProcessarEfeitosJulgamento(JulgamentoRecurso julgamento)
        {
            // Processar efeitos do julgamento baseado no resultado
            if (julgamento.Resultado == ResultadoJulgamentoRecurso.Provido)
            {
                // Reverter decisão original
                // Implementar lógica específica
            }
            await Task.CompletedTask;
        }

        #endregion

        #region Notificações

        private async Task NotificarRelatorDesignado(MembroComissao relator, JulgamentoAdmissibilidade julgamento)
        {
            await _emailService.EnviarEmailAsync(
                relator.Email,
                "Designação como Relator",
                $"Você foi designado como relator do processo de admissibilidade nº {julgamento.Id}. Prazo para análise: {julgamento.PrazoAnalise:dd/MM/yyyy}");
        }

        private async Task NotificarInterposicaoRecurso(RecursoEleitoral recurso)
        {
            await _notificacaoService.NotificarAsync(
                "Novo Recurso Interposto",
                $"Foi interposto o recurso {recurso.NumeroProcesso}",
                TipoNotificacao.RecursoInterposto);
        }

        private async Task NotificarContrarrazoesApresentadas(ContrarrazaoRecurso contrarrazao)
        {
            await _notificacaoService.NotificarAsync(
                "Contrarrazões Apresentadas",
                $"Foram apresentadas contrarrazões ao recurso {contrarrazao.RecursoEleitoral?.NumeroProcesso}",
                TipoNotificacao.ContrarrazoesApresentadas);
        }

        private async Task NotificarResultadoJulgamento(JulgamentoRecurso julgamento)
        {
            await _notificacaoService.NotificarAsync(
                "Resultado do Julgamento",
                $"O recurso foi julgado: {julgamento.Resultado}",
                TipoNotificacao.JulgamentoProferido);
        }

        #endregion
    }
}