using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Infrastructure.Data;
using SistemaEleitoral.Application.DTOs;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Serviço responsável pela gestão de atividades do calendário eleitoral
    /// </summary>
    public class AtividadeCalendarioService : IAtividadeCalendarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AtividadeCalendarioService> _logger;
        private readonly INotificationService _notificationService;
        private readonly ICalendarioService _calendarioService;

        public AtividadeCalendarioService(
            ApplicationDbContext context,
            ILogger<AtividadeCalendarioService> logger,
            INotificationService notificationService,
            ICalendarioService calendarioService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _calendarioService = calendarioService;
        }

        #region Atividades Principais

        /// <summary>
        /// Cria uma nova atividade principal
        /// </summary>
        public async Task<AtividadePrincipalCalendario> CriarAtividadePrincipalAsync(CriarAtividadePrincipalDTO dto)
        {
            _logger.LogInformation("Criando atividade principal para calendário {CalendarioId}", dto.CalendarioId);

            // Validar calendário
            var calendario = await _context.Calendarios
                .FirstOrDefaultAsync(c => c.Id == dto.CalendarioId);

            if (calendario == null)
            {
                throw new InvalidOperationException($"Calendário {dto.CalendarioId} não encontrado");
            }

            if (calendario.Situacao == SituacaoCalendario.Encerrado)
            {
                throw new InvalidOperationException("Não é possível adicionar atividades a um calendário encerrado");
            }

            // Validar datas
            if (dto.DataInicio >= dto.DataFim)
            {
                throw new InvalidOperationException("Data de início deve ser anterior à data de fim");
            }

            // Criar atividade principal
            var atividade = new AtividadePrincipalCalendario
            {
                CalendarioId = dto.CalendarioId,
                Nome = dto.Nome,
                Descricao = dto.Descricao,
                DataInicio = dto.DataInicio,
                DataFim = dto.DataFim,
                TipoAtividade = dto.TipoAtividade,
                Obrigatoria = dto.Obrigatoria,
                PermiteAlteracao = dto.PermiteAlteracao,
                OrdemExibicao = await ObterProximaOrdemAsync(dto.CalendarioId),
                Ativo = true,
                DataCriacao = DateTime.Now,
                UsuarioCriacaoId = dto.UsuarioCriacaoId
            };

            _context.AtividadesPrincipaisCalendario.Add(atividade);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Atividade principal {Nome} criada com sucesso - ID: {Id}", 
                atividade.Nome, atividade.Id);

            // Notificar sobre nova atividade
            await NotificarNovaAtividadeAsync(atividade);

            return atividade;
        }

        /// <summary>
        /// Atualiza uma atividade principal
        /// </summary>
        public async Task<AtividadePrincipalCalendario> AtualizarAtividadePrincipalAsync(
            int id, AtualizarAtividadePrincipalDTO dto)
        {
            _logger.LogInformation("Atualizando atividade principal {Id}", id);

            var atividade = await _context.AtividadesPrincipaisCalendario
                .Include(a => a.Calendario)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (atividade == null)
            {
                throw new InvalidOperationException($"Atividade principal {id} não encontrada");
            }

            if (!atividade.PermiteAlteracao)
            {
                throw new InvalidOperationException("Esta atividade não permite alterações");
            }

            if (atividade.Calendario.Situacao == SituacaoCalendario.Encerrado)
            {
                throw new InvalidOperationException("Não é possível alterar atividades de um calendário encerrado");
            }

            // Atualizar dados
            atividade.Nome = dto.Nome ?? atividade.Nome;
            atividade.Descricao = dto.Descricao ?? atividade.Descricao;
            atividade.DataInicio = dto.DataInicio ?? atividade.DataInicio;
            atividade.DataFim = dto.DataFim ?? atividade.DataFim;
            atividade.Obrigatoria = dto.Obrigatoria ?? atividade.Obrigatoria;
            atividade.DataAtualizacao = DateTime.Now;
            atividade.UsuarioAtualizacaoId = dto.UsuarioAtualizacaoId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Atividade principal {Id} atualizada com sucesso", id);

            return atividade;
        }

        /// <summary>
        /// Lista atividades principais de um calendário
        /// </summary>
        public async Task<List<AtividadePrincipalCalendario>> ListarAtividadesPrincipaisAsync(
            int calendarioId, FiltroAtividadesDTO filtro)
        {
            var query = _context.AtividadesPrincipaisCalendario
                .Include(a => a.AtividadesSecundarias)
                .Where(a => a.CalendarioId == calendarioId && a.Ativo);

            if (filtro.TipoAtividade.HasValue)
            {
                query = query.Where(a => a.TipoAtividade == filtro.TipoAtividade.Value);
            }

            if (filtro.ApenasObrigatorias == true)
            {
                query = query.Where(a => a.Obrigatoria);
            }

            if (filtro.DataInicio.HasValue)
            {
                query = query.Where(a => a.DataInicio >= filtro.DataInicio.Value);
            }

            if (filtro.DataFim.HasValue)
            {
                query = query.Where(a => a.DataFim <= filtro.DataFim.Value);
            }

            return await query
                .OrderBy(a => a.OrdemExibicao)
                .ThenBy(a => a.DataInicio)
                .ToListAsync();
        }

        #endregion

        #region Atividades Secundárias

        /// <summary>
        /// Cria uma nova atividade secundária
        /// </summary>
        public async Task<AtividadeSecundariaCalendario> CriarAtividadeSecundariaAsync(
            CriarAtividadeSecundariaDTO dto)
        {
            _logger.LogInformation("Criando atividade secundária para atividade principal {AtividadePrincipalId}", 
                dto.AtividadePrincipalId);

            // Validar atividade principal
            var atividadePrincipal = await _context.AtividadesPrincipaisCalendario
                .Include(a => a.Calendario)
                .FirstOrDefaultAsync(a => a.Id == dto.AtividadePrincipalId);

            if (atividadePrincipal == null)
            {
                throw new InvalidOperationException($"Atividade principal {dto.AtividadePrincipalId} não encontrada");
            }

            // Validar datas dentro do período da atividade principal
            if (dto.DataInicio < atividadePrincipal.DataInicio || dto.DataFim > atividadePrincipal.DataFim)
            {
                throw new InvalidOperationException(
                    "As datas da atividade secundária devem estar dentro do período da atividade principal");
            }

            // Criar atividade secundária
            var atividade = new AtividadeSecundariaCalendario
            {
                AtividadePrincipalId = dto.AtividadePrincipalId,
                CalendarioId = atividadePrincipal.CalendarioId,
                Nome = dto.Nome,
                Descricao = dto.Descricao,
                DataInicio = dto.DataInicio,
                DataFim = dto.DataFim,
                TipoAtividade = dto.TipoAtividade,
                Responsavel = dto.Responsavel,
                Local = dto.Local,
                OrdemExibicao = await ObterProximaOrdemSecundariaAsync(dto.AtividadePrincipalId),
                Ativo = true,
                DataCriacao = DateTime.Now,
                UsuarioCriacaoId = dto.UsuarioCriacaoId
            };

            _context.AtividadesSecundariasCalendario.Add(atividade);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Atividade secundária {Nome} criada com sucesso - ID: {Id}", 
                atividade.Nome, atividade.Id);

            return atividade;
        }

        /// <summary>
        /// Atualiza uma atividade secundária
        /// </summary>
        public async Task<AtividadeSecundariaCalendario> AtualizarAtividadeSecundariaAsync(
            int id, AtualizarAtividadeSecundariaDTO dto)
        {
            _logger.LogInformation("Atualizando atividade secundária {Id}", id);

            var atividade = await _context.AtividadesSecundariasCalendario
                .Include(a => a.AtividadePrincipal)
                    .ThenInclude(ap => ap.Calendario)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (atividade == null)
            {
                throw new InvalidOperationException($"Atividade secundária {id} não encontrada");
            }

            if (atividade.AtividadePrincipal.Calendario.Situacao == SituacaoCalendario.Encerrado)
            {
                throw new InvalidOperationException("Não é possível alterar atividades de um calendário encerrado");
            }

            // Atualizar dados
            atividade.Nome = dto.Nome ?? atividade.Nome;
            atividade.Descricao = dto.Descricao ?? atividade.Descricao;
            atividade.DataInicio = dto.DataInicio ?? atividade.DataInicio;
            atividade.DataFim = dto.DataFim ?? atividade.DataFim;
            atividade.Responsavel = dto.Responsavel ?? atividade.Responsavel;
            atividade.Local = dto.Local ?? atividade.Local;
            atividade.DataAtualizacao = DateTime.Now;
            atividade.UsuarioAtualizacaoId = dto.UsuarioAtualizacaoId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Atividade secundária {Id} atualizada com sucesso", id);

            return atividade;
        }

        #endregion

        #region Prazos e Validações

        /// <summary>
        /// Valida prazos de atividades
        /// </summary>
        public async Task<ValidacaoPrazosDTO> ValidarPrazosAtividadesAsync(int calendarioId)
        {
            var calendario = await _context.Calendarios
                .Include(c => c.AtividadesPrincipais)
                    .ThenInclude(ap => ap.AtividadesSecundarias)
                .FirstOrDefaultAsync(c => c.Id == calendarioId);

            if (calendario == null)
            {
                throw new InvalidOperationException($"Calendário {calendarioId} não encontrado");
            }

            var validacao = new ValidacaoPrazosDTO
            {
                CalendarioId = calendarioId,
                DataValidacao = DateTime.Now,
                AtividadesVencidas = new List<AtividadeVencidaDTO>(),
                AtividadesProximas = new List<AtividadeProximaDTO>(),
                Conflitos = new List<ConflitoAtividadeDTO>()
            };

            var dataAtual = DateTime.Now;

            // Verificar atividades vencidas
            foreach (var atividade in calendario.AtividadesPrincipais.Where(a => a.Ativo))
            {
                if (atividade.DataFim < dataAtual && atividade.Obrigatoria)
                {
                    validacao.AtividadesVencidas.Add(new AtividadeVencidaDTO
                    {
                        AtividadeId = atividade.Id,
                        Nome = atividade.Nome,
                        DataVencimento = atividade.DataFim,
                        DiasAtraso = (int)(dataAtual - atividade.DataFim).TotalDays
                    });
                }

                // Atividades próximas (próximos 7 dias)
                if (atividade.DataInicio > dataAtual && 
                    atividade.DataInicio <= dataAtual.AddDays(7))
                {
                    validacao.AtividadesProximas.Add(new AtividadeProximaDTO
                    {
                        AtividadeId = atividade.Id,
                        Nome = atividade.Nome,
                        DataInicio = atividade.DataInicio,
                        DiasRestantes = (int)(atividade.DataInicio - dataAtual).TotalDays
                    });
                }
            }

            // Verificar conflitos de datas
            var atividadesOrdenadas = calendario.AtividadesPrincipais
                .Where(a => a.Ativo)
                .OrderBy(a => a.DataInicio)
                .ToList();

            for (int i = 0; i < atividadesOrdenadas.Count - 1; i++)
            {
                for (int j = i + 1; j < atividadesOrdenadas.Count; j++)
                {
                    if (atividadesOrdenadas[i].DataFim > atividadesOrdenadas[j].DataInicio)
                    {
                        validacao.Conflitos.Add(new ConflitoAtividadeDTO
                        {
                            AtividadeId1 = atividadesOrdenadas[i].Id,
                            NomeAtividade1 = atividadesOrdenadas[i].Nome,
                            AtividadeId2 = atividadesOrdenadas[j].Id,
                            NomeAtividade2 = atividadesOrdenadas[j].Nome,
                            TipoConflito = "Sobreposição de datas"
                        });
                    }
                }
            }

            validacao.TotalAtividadesVencidas = validacao.AtividadesVencidas.Count;
            validacao.TotalAtividadesProximas = validacao.AtividadesProximas.Count;
            validacao.TotalConflitos = validacao.Conflitos.Count;
            validacao.StatusGeral = validacao.TotalAtividadesVencidas == 0 && validacao.TotalConflitos == 0 
                ? "OK" : "Com Pendências";

            return validacao;
        }

        /// <summary>
        /// Reordena atividades
        /// </summary>
        public async Task<bool> ReordenarAtividadesAsync(ReordenarAtividadesDTO dto)
        {
            _logger.LogInformation("Reordenando atividades do calendário {CalendarioId}", dto.CalendarioId);

            var atividades = await _context.AtividadesPrincipaisCalendario
                .Where(a => a.CalendarioId == dto.CalendarioId && a.Ativo)
                .ToListAsync();

            foreach (var ordem in dto.NovasOrdens)
            {
                var atividade = atividades.FirstOrDefault(a => a.Id == ordem.AtividadeId);
                if (atividade != null)
                {
                    atividade.OrdemExibicao = ordem.NovaOrdem;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Atividades reordenadas com sucesso");

            return true;
        }

        #endregion

        #region Métodos Privados

        private async Task<int> ObterProximaOrdemAsync(int calendarioId)
        {
            var ultimaOrdem = await _context.AtividadesPrincipaisCalendario
                .Where(a => a.CalendarioId == calendarioId)
                .MaxAsync(a => (int?)a.OrdemExibicao) ?? 0;

            return ultimaOrdem + 1;
        }

        private async Task<int> ObterProximaOrdemSecundariaAsync(int atividadePrincipalId)
        {
            var ultimaOrdem = await _context.AtividadesSecundariasCalendario
                .Where(a => a.AtividadePrincipalId == atividadePrincipalId)
                .MaxAsync(a => (int?)a.OrdemExibicao) ?? 0;

            return ultimaOrdem + 1;
        }

        private async Task NotificarNovaAtividadeAsync(AtividadePrincipalCalendario atividade)
        {
            try
            {
                _logger.LogInformation("Notificando sobre nova atividade {Nome}", atividade.Nome);
                
                // Implementar notificação
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao notificar sobre nova atividade");
            }
        }

        #endregion
    }
}