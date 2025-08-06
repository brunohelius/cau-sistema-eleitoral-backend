using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces.Repositories;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Application.DTOs.ImpugnacaoResultado;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Serviço para gerenciamento de impugnações de resultado eleitoral
    /// </summary>
    public class ImpugnacaoResultadoService : IImpugnacaoResultadoService
    {
        private readonly IImpugnacaoResultadoRepository _repository;
        private readonly ICalendarioService _calendarioService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<ImpugnacaoResultadoService> _logger;

        public ImpugnacaoResultadoService(
            IImpugnacaoResultadoRepository repository,
            ICalendarioService calendarioService,
            INotificationService notificationService,
            ILogger<ImpugnacaoResultadoService> logger)
        {
            _repository = repository;
            _calendarioService = calendarioService;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Registra uma nova impugnação de resultado
        /// </summary>
        public async Task<ImpugnacaoResultadoDTO> RegistrarImpugnacaoAsync(RegistrarImpugnacaoResultadoDTO dto)
        {
            try
            {
                _logger.LogInformation("Registrando impugnação de resultado para calendário {CalendarioId}", dto.CalendarioId);

                // Validar período para impugnação de resultado
                var periodoValido = await _calendarioService.ValidarPeriodoImpugnacaoResultadoAsync(dto.CalendarioId);
                if (!periodoValido)
                {
                    throw new InvalidOperationException("Fora do período para impugnação de resultado");
                }

                // Verificar se já existe impugnação do profissional para este calendário
                var jaExiste = await _repository.ExisteImpugnacaoParaCalendarioAsync(dto.CalendarioId, dto.ProfissionalId);
                if (jaExiste)
                {
                    throw new InvalidOperationException("Já existe uma impugnação registrada para este calendário");
                }

                // Obter próximo número
                var proximoNumero = await _repository.GetProximoNumeroAsync(dto.CalendarioId);

                // Criar nova impugnação
                var impugnacao = new ImpugnacaoResultado
                {
                    NarracaoFatos = dto.NarracaoFatos,
                    Numero = proximoNumero,
                    DataCadastro = DateTime.Now,
                    CauBrId = dto.CauBrId,
                    NomeArquivo = dto.NomeArquivo,
                    NomeArquivoFisico = dto.NomeArquivoFisico,
                    ProfissionalId = dto.ProfissionalId,
                    StatusId = (int)StatusImpugnacaoResultadoEnum.EmAnalise,
                    CalendarioId = dto.CalendarioId
                };

                await _repository.AddAsync(impugnacao);

                // Enviar notificação
                await _notificationService.NotificarNovaImpugnacaoResultadoAsync(impugnacao);

                _logger.LogInformation("Impugnação de resultado {Numero} registrada com sucesso", impugnacao.Numero);

                return MapToDTO(impugnacao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar impugnação de resultado");
                throw;
            }
        }

        /// <summary>
        /// Adiciona alegação à impugnação
        /// </summary>
        public async Task<AlegacaoImpugnacaoResultadoDTO> AdicionarAlegacaoAsync(AdicionarAlegacaoDTO dto)
        {
            try
            {
                var impugnacao = await _repository.GetByIdAsync(dto.ImpugnacaoResultadoId);
                if (impugnacao == null)
                {
                    throw new InvalidOperationException("Impugnação não encontrada");
                }

                if (!impugnacao.PodeApresentarAlegacao())
                {
                    throw new InvalidOperationException("Impugnação não está em fase de alegações");
                }

                var alegacao = new AlegacaoImpugnacaoResultado
                {
                    Descricao = dto.Descricao,
                    DataCadastro = DateTime.Now,
                    ImpugnacaoResultadoId = dto.ImpugnacaoResultadoId,
                    ProfissionalId = dto.ProfissionalId,
                    ChapaEleicaoId = dto.ChapaEleicaoId,
                    NomeArquivo = dto.NomeArquivo,
                    NomeArquivoFisico = dto.NomeArquivoFisico,
                    IsImpugnante = dto.IsImpugnante
                };

                await _repository.AddAlegacaoAsync(alegacao);

                // Atualizar status se necessário
                if (impugnacao.StatusId == (int)StatusImpugnacaoResultadoEnum.AguardandoAlegacao)
                {
                    await _repository.AtualizarStatusAsync(impugnacao.Id, StatusImpugnacaoResultadoEnum.AlegacaoRecebida);
                }

                // Notificar partes interessadas
                await _notificationService.NotificarNovaAlegacaoAsync(alegacao);

                return MapAlegacaoToDTO(alegacao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar alegação");
                throw;
            }
        }

        /// <summary>
        /// Adiciona recurso à impugnação
        /// </summary>
        public async Task<RecursoImpugnacaoResultadoDTO> AdicionarRecursoAsync(AdicionarRecursoDTO dto)
        {
            try
            {
                var impugnacao = await _repository.GetByIdAsync(dto.ImpugnacaoResultadoId);
                if (impugnacao == null)
                {
                    throw new InvalidOperationException("Impugnação não encontrada");
                }

                if (!impugnacao.PodeRecorrer())
                {
                    throw new InvalidOperationException("Impugnação não permite recurso neste momento");
                }

                var recurso = new RecursoImpugnacaoResultado
                {
                    Descricao = dto.Descricao,
                    DataCadastro = DateTime.Now,
                    ImpugnacaoResultadoId = dto.ImpugnacaoResultadoId,
                    ProfissionalId = dto.ProfissionalId,
                    TipoRecursoId = dto.TipoRecursoId,
                    NomeArquivo = dto.NomeArquivo,
                    NomeArquivoFisico = dto.NomeArquivoFisico
                };

                await _repository.AddRecursoAsync(recurso);

                // Atualizar status para em recurso
                await _repository.AtualizarStatusAsync(impugnacao.Id, StatusImpugnacaoResultadoEnum.EmRecurso);

                // Notificar
                await _notificationService.NotificarNovoRecursoAsync(recurso);

                return MapRecursoToDTO(recurso);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar recurso");
                throw;
            }
        }

        /// <summary>
        /// Adiciona contrarrazão ao recurso
        /// </summary>
        public async Task<ContrarrazaoImpugnacaoResultadoDTO> AdicionarContrarrazaoAsync(AdicionarContrarrazaoDTO dto)
        {
            try
            {
                var recurso = await _repository.GetRecursosAsync(dto.RecursoImpugnacaoResultadoId)
                    .ContinueWith(t => t.Result.FirstOrDefault(r => r.Id == dto.RecursoImpugnacaoResultadoId));

                if (recurso == null)
                {
                    throw new InvalidOperationException("Recurso não encontrado");
                }

                if (!recurso.PodeApresentarContrarrazao())
                {
                    throw new InvalidOperationException("Recurso não permite contrarrazão");
                }

                var contrarrazao = new ContrarrazaoImpugnacaoResultado
                {
                    Descricao = dto.Descricao,
                    DataCadastro = DateTime.Now,
                    RecursoImpugnacaoResultadoId = dto.RecursoImpugnacaoResultadoId,
                    ProfissionalId = dto.ProfissionalId,
                    ChapaEleicaoId = dto.ChapaEleicaoId,
                    NomeArquivo = dto.NomeArquivo,
                    NomeArquivoFisico = dto.NomeArquivoFisico
                };

                await _repository.AddContrarrazaoAsync(contrarrazao);

                // Notificar
                await _notificationService.NotificarNovaContrarrazaoAsync(contrarrazao);

                return MapContrarrazaoToDTO(contrarrazao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar contrarrazão");
                throw;
            }
        }

        /// <summary>
        /// Julga alegação de impugnação
        /// </summary>
        public async Task<JulgamentoAlegacaoImpugResultadoDTO> JulgarAlegacaoAsync(JulgarAlegacaoDTO dto)
        {
            try
            {
                var impugnacao = await _repository.GetByIdAsync(dto.ImpugnacaoResultadoId);
                if (impugnacao == null)
                {
                    throw new InvalidOperationException("Impugnação não encontrada");
                }

                var julgamento = new JulgamentoAlegacaoImpugResultado
                {
                    ImpugnacaoResultadoId = dto.ImpugnacaoResultadoId,
                    Parecer = dto.Parecer,
                    Deferido = dto.Deferido,
                    DataJulgamento = DateTime.Now,
                    ProfissionalJulgadorId = dto.ProfissionalJulgadorId,
                    StatusJulgamentoId = dto.Deferido ? 2 : 3,
                    NomeArquivoDecisao = dto.NomeArquivoDecisao,
                    NomeArquivoFisicoDecisao = dto.NomeArquivoFisicoDecisao
                };

                await _repository.AddJulgamentoAlegacaoAsync(julgamento);

                // Atualizar status
                await _repository.AtualizarStatusAsync(impugnacao.Id, StatusImpugnacaoResultadoEnum.Julgada);

                // Notificar
                await _notificationService.NotificarJulgamentoAlegacaoAsync(julgamento);

                return MapJulgamentoAlegacaoToDTO(julgamento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao julgar alegação");
                throw;
            }
        }

        /// <summary>
        /// Julga recurso de impugnação
        /// </summary>
        public async Task<JulgamentoRecursoImpugResultadoDTO> JulgarRecursoAsync(JulgarRecursoDTO dto)
        {
            try
            {
                var impugnacao = await _repository.GetByIdAsync(dto.ImpugnacaoResultadoId);
                if (impugnacao == null)
                {
                    throw new InvalidOperationException("Impugnação não encontrada");
                }

                var julgamento = new JulgamentoRecursoImpugResultado
                {
                    ImpugnacaoResultadoId = dto.ImpugnacaoResultadoId,
                    Parecer = dto.Parecer,
                    Deferido = dto.Deferido,
                    DataJulgamento = DateTime.Now,
                    ProfissionalJulgadorId = dto.ProfissionalJulgadorId,
                    StatusJulgamentoId = dto.Deferido ? 2 : 3,
                    NomeArquivoDecisao = dto.NomeArquivoDecisao,
                    NomeArquivoFisicoDecisao = dto.NomeArquivoFisicoDecisao,
                    DecisaoFinal = dto.DecisaoFinal
                };

                await _repository.AddJulgamentoRecursoAsync(julgamento);

                // Atualizar status
                var novoStatus = dto.DecisaoFinal ? 
                    StatusImpugnacaoResultadoEnum.Finalizada : 
                    StatusImpugnacaoResultadoEnum.RecursoJulgado;
                
                await _repository.AtualizarStatusAsync(impugnacao.Id, novoStatus);

                // Notificar
                await _notificationService.NotificarJulgamentoRecursoAsync(julgamento);

                return MapJulgamentoRecursoToDTO(julgamento);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao julgar recurso");
                throw;
            }
        }

        /// <summary>
        /// Busca impugnação por ID
        /// </summary>
        public async Task<ImpugnacaoResultadoDetalheDTO?> ObterPorIdAsync(int id)
        {
            var impugnacao = await _repository.GetByIdWithRelationsAsync(id);
            return impugnacao != null ? MapToDetalheDTO(impugnacao) : null;
        }

        /// <summary>
        /// Lista impugnações por calendário
        /// </summary>
        public async Task<IEnumerable<ImpugnacaoResultadoDTO>> ListarPorCalendarioAsync(int calendarioId)
        {
            var impugnacoes = await _repository.GetByCalendarioAsync(calendarioId);
            return impugnacoes.Select(MapToDTO);
        }

        /// <summary>
        /// Lista impugnações por profissional
        /// </summary>
        public async Task<IEnumerable<ImpugnacaoResultadoDTO>> ListarPorProfissionalAsync(int profissionalId)
        {
            var impugnacoes = await _repository.GetByProfissionalAsync(profissionalId);
            return impugnacoes.Select(MapToDTO);
        }

        /// <summary>
        /// Lista impugnações pendentes de julgamento
        /// </summary>
        public async Task<IEnumerable<ImpugnacaoResultadoDTO>> ListarPendentesJulgamentoAsync()
        {
            var impugnacoes = await _repository.GetPendentesJulgamentoAsync();
            return impugnacoes.Select(MapToDTO);
        }

        #region Métodos de Mapeamento

        private ImpugnacaoResultadoDTO MapToDTO(ImpugnacaoResultado entity)
        {
            return new ImpugnacaoResultadoDTO
            {
                Id = entity.Id,
                NarracaoFatos = entity.NarracaoFatos,
                Numero = entity.Numero,
                DataCadastro = entity.DataCadastro,
                NomeArquivo = entity.NomeArquivo,
                ProfissionalNome = entity.Profissional?.Nome ?? "",
                StatusDescricao = ObterDescricaoStatus(entity.StatusId),
                CalendarioId = entity.CalendarioId,
                Protocolo = entity.GerarProtocolo()
            };
        }

        private ImpugnacaoResultadoDetalheDTO MapToDetalheDTO(ImpugnacaoResultado entity)
        {
            return new ImpugnacaoResultadoDetalheDTO
            {
                Id = entity.Id,
                NarracaoFatos = entity.NarracaoFatos,
                Numero = entity.Numero,
                DataCadastro = entity.DataCadastro,
                NomeArquivo = entity.NomeArquivo,
                NomeArquivoFisico = entity.NomeArquivoFisico,
                ProfissionalId = entity.ProfissionalId,
                ProfissionalNome = entity.Profissional?.Nome ?? "",
                StatusId = entity.StatusId,
                StatusDescricao = ObterDescricaoStatus(entity.StatusId),
                CalendarioId = entity.CalendarioId,
                Protocolo = entity.GerarProtocolo(),
                Alegacoes = entity.Alegacoes?.Select(MapAlegacaoToDTO).ToList() ?? new List<AlegacaoImpugnacaoResultadoDTO>(),
                Recursos = entity.Recursos?.Select(MapRecursoToDTO).ToList() ?? new List<RecursoImpugnacaoResultadoDTO>(),
                JulgamentoAlegacao = entity.JulgamentoAlegacao != null ? MapJulgamentoAlegacaoToDTO(entity.JulgamentoAlegacao) : null,
                JulgamentoRecurso = entity.JulgamentoRecurso != null ? MapJulgamentoRecursoToDTO(entity.JulgamentoRecurso) : null
            };
        }

        private AlegacaoImpugnacaoResultadoDTO MapAlegacaoToDTO(AlegacaoImpugnacaoResultado entity)
        {
            return new AlegacaoImpugnacaoResultadoDTO
            {
                Id = entity.Id,
                Descricao = entity.Descricao,
                DataCadastro = entity.DataCadastro,
                ProfissionalNome = entity.Profissional?.Nome ?? "",
                TipoAlegante = entity.ObterTipoAlegante(),
                NomeArquivo = entity.NomeArquivo
            };
        }

        private RecursoImpugnacaoResultadoDTO MapRecursoToDTO(RecursoImpugnacaoResultado entity)
        {
            return new RecursoImpugnacaoResultadoDTO
            {
                Id = entity.Id,
                Descricao = entity.Descricao,
                DataCadastro = entity.DataCadastro,
                ProfissionalNome = entity.Profissional?.Nome ?? "",
                TipoRecurso = entity.TipoRecurso?.Descricao ?? "",
                Deferido = entity.Deferido,
                Parecer = entity.Parecer,
                DataJulgamento = entity.DataJulgamento,
                NomeArquivo = entity.NomeArquivo
            };
        }

        private ContrarrazaoImpugnacaoResultadoDTO MapContrarrazaoToDTO(ContrarrazaoImpugnacaoResultado entity)
        {
            return new ContrarrazaoImpugnacaoResultadoDTO
            {
                Id = entity.Id,
                Descricao = entity.Descricao,
                DataCadastro = entity.DataCadastro,
                ProfissionalNome = entity.Profissional?.Nome ?? "",
                NomeArquivo = entity.NomeArquivo
            };
        }

        private JulgamentoAlegacaoImpugResultadoDTO MapJulgamentoAlegacaoToDTO(JulgamentoAlegacaoImpugResultado entity)
        {
            return new JulgamentoAlegacaoImpugResultadoDTO
            {
                Id = entity.Id,
                Parecer = entity.Parecer,
                Deferido = entity.Deferido,
                DataJulgamento = entity.DataJulgamento,
                ProfissionalJulgadorNome = entity.ProfissionalJulgador?.Nome ?? "",
                Resultado = entity.ObterResultado()
            };
        }

        private JulgamentoRecursoImpugResultadoDTO MapJulgamentoRecursoToDTO(JulgamentoRecursoImpugResultado entity)
        {
            return new JulgamentoRecursoImpugResultadoDTO
            {
                Id = entity.Id,
                Parecer = entity.Parecer,
                Deferido = entity.Deferido,
                DataJulgamento = entity.DataJulgamento,
                ProfissionalJulgadorNome = entity.ProfissionalJulgador?.Nome ?? "",
                Resultado = entity.ObterResultado(),
                Instancia = entity.ObterInstancia(),
                DecisaoFinal = entity.DecisaoFinal
            };
        }

        private string ObterDescricaoStatus(int statusId)
        {
            return (StatusImpugnacaoResultadoEnum)statusId switch
            {
                StatusImpugnacaoResultadoEnum.EmAnalise => "Em Análise",
                StatusImpugnacaoResultadoEnum.AguardandoAlegacao => "Aguardando Alegação",
                StatusImpugnacaoResultadoEnum.AlegacaoRecebida => "Alegação Recebida",
                StatusImpugnacaoResultadoEnum.AguardandoJulgamento => "Aguardando Julgamento",
                StatusImpugnacaoResultadoEnum.Julgada => "Julgada",
                StatusImpugnacaoResultadoEnum.EmRecurso => "Em Recurso",
                StatusImpugnacaoResultadoEnum.RecursoJulgado => "Recurso Julgado",
                StatusImpugnacaoResultadoEnum.Arquivada => "Arquivada",
                StatusImpugnacaoResultadoEnum.Finalizada => "Finalizada",
                _ => "Desconhecido"
            };
        }

        #endregion
    }
}