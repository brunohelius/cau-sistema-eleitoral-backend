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
    /// Serviço responsável pela gestão de eleições
    /// </summary>
    public class EleicaoService : IEleicaoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EleicaoService> _logger;
        private readonly INotificationService _notificationService;

        public EleicaoService(
            ApplicationDbContext context,
            ILogger<EleicaoService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Cria uma nova eleição
        /// </summary>
        public async Task<Eleicao> CriarEleicaoAsync(CriarEleicaoDTO dto)
        {
            _logger.LogInformation("Criando nova eleição para o ano {Ano}", dto.Ano);

            // Validar se já existe eleição para o ano
            var eleicaoExistente = await _context.Eleicoes
                .AnyAsync(e => e.Ano == dto.Ano && e.Ativo);

            if (eleicaoExistente)
            {
                throw new InvalidOperationException($"Já existe uma eleição ativa para o ano {dto.Ano}");
            }

            // Criar nova eleição
            var eleicao = new Eleicao
            {
                Nome = dto.Nome,
                Descricao = dto.Descricao,
                Ano = dto.Ano,
                TipoProcessoId = dto.TipoProcessoId,
                DataInicio = dto.DataInicio,
                DataFim = dto.DataFim,
                SequenciaAno = await ObterProximaSequenciaAnoAsync(dto.Ano),
                Ativo = true,
                DataCriacao = DateTime.Now,
                UsuarioCriacaoId = dto.UsuarioCriacaoId
            };

            // Criar situação inicial
            eleicao.Situacao = SituacaoEleicao.EmAndamento;
            eleicao.DataSituacao = DateTime.Now;

            _context.Eleicoes.Add(eleicao);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Eleição {Nome} criada com sucesso - ID: {Id}", eleicao.Nome, eleicao.Id);

            // Notificar sobre nova eleição
            await NotificarNovaEleicaoAsync(eleicao);

            return eleicao;
        }

        /// <summary>
        /// Atualiza uma eleição existente
        /// </summary>
        public async Task<Eleicao> AtualizarEleicaoAsync(int id, AtualizarEleicaoDTO dto)
        {
            _logger.LogInformation("Atualizando eleição {Id}", id);

            var eleicao = await _context.Eleicoes
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eleicao == null)
            {
                throw new InvalidOperationException($"Eleição {id} não encontrada");
            }

            // Validar se pode atualizar
            if (eleicao.Situacao == SituacaoEleicao.Encerrada)
            {
                throw new InvalidOperationException("Não é possível atualizar uma eleição encerrada");
            }

            // Atualizar dados
            eleicao.Nome = dto.Nome ?? eleicao.Nome;
            eleicao.Descricao = dto.Descricao ?? eleicao.Descricao;
            eleicao.DataInicio = dto.DataInicio ?? eleicao.DataInicio;
            eleicao.DataFim = dto.DataFim ?? eleicao.DataFim;
            eleicao.DataAtualizacao = DateTime.Now;
            eleicao.UsuarioAtualizacaoId = dto.UsuarioAtualizacaoId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Eleição {Id} atualizada com sucesso", id);

            return eleicao;
        }

        /// <summary>
        /// Obtém eleição por ID
        /// </summary>
        public async Task<Eleicao> ObterEleicaoPorIdAsync(int id)
        {
            var eleicao = await _context.Eleicoes
                .Include(e => e.TipoProcesso)
                .Include(e => e.Calendarios)
                    .ThenInclude(c => c.Uf)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eleicao == null)
            {
                throw new InvalidOperationException($"Eleição {id} não encontrada");
            }

            return eleicao;
        }

        /// <summary>
        /// Lista todas as eleições
        /// </summary>
        public async Task<List<Eleicao>> ListarEleicoesAsync(FiltroEleicoesDTO filtro)
        {
            var query = _context.Eleicoes
                .Include(e => e.TipoProcesso)
                .Where(e => e.Ativo);

            if (filtro.Ano.HasValue)
            {
                query = query.Where(e => e.Ano == filtro.Ano.Value);
            }

            if (filtro.Situacao.HasValue)
            {
                query = query.Where(e => e.Situacao == filtro.Situacao.Value);
            }

            if (!string.IsNullOrEmpty(filtro.Nome))
            {
                query = query.Where(e => e.Nome.Contains(filtro.Nome));
            }

            return await query
                .OrderByDescending(e => e.Ano)
                .ThenBy(e => e.SequenciaAno)
                .ToListAsync();
        }

        /// <summary>
        /// Obtém anos com eleições
        /// </summary>
        public async Task<List<int>> ObterAnosComEleicoesAsync()
        {
            return await _context.Eleicoes
                .Where(e => e.Ativo)
                .Select(e => e.Ano)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();
        }

        /// <summary>
        /// Altera situação da eleição
        /// </summary>
        public async Task<Eleicao> AlterarSituacaoEleicaoAsync(int id, AlterarSituacaoEleicaoDTO dto)
        {
            _logger.LogInformation("Alterando situação da eleição {Id} para {Situacao}", id, dto.NovaSituacao);

            var eleicao = await _context.Eleicoes
                .Include(e => e.Calendarios)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eleicao == null)
            {
                throw new InvalidOperationException($"Eleição {id} não encontrada");
            }

            // Validar transição de situação
            ValidarTransicaoSituacao(eleicao.Situacao, dto.NovaSituacao);

            // Registrar histórico
            var historico = new HistoricoEleicao
            {
                EleicaoId = eleicao.Id,
                SituacaoAnterior = eleicao.Situacao,
                SituacaoNova = dto.NovaSituacao,
                Motivo = dto.Motivo,
                DataAlteracao = DateTime.Now,
                UsuarioAlteracaoId = dto.UsuarioAlteracaoId
            };

            _context.HistoricosEleicao.Add(historico);

            // Atualizar situação
            eleicao.Situacao = dto.NovaSituacao;
            eleicao.DataSituacao = DateTime.Now;
            eleicao.MotivoSituacao = dto.Motivo;

            // Se encerrar eleição, encerrar calendários
            if (dto.NovaSituacao == SituacaoEleicao.Encerrada)
            {
                foreach (var calendario in eleicao.Calendarios)
                {
                    calendario.Situacao = SituacaoCalendario.Encerrado;
                    calendario.DataSituacao = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Situação da eleição {Id} alterada com sucesso", id);

            // Notificar sobre mudança de situação
            await NotificarMudancaSituacaoAsync(eleicao, dto.NovaSituacao);

            return eleicao;
        }

        /// <summary>
        /// Exclui logicamente uma eleição
        /// </summary>
        public async Task<bool> ExcluirEleicaoAsync(int id, int usuarioExclusaoId)
        {
            _logger.LogInformation("Excluindo eleição {Id}", id);

            var eleicao = await _context.Eleicoes
                .Include(e => e.Calendarios)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eleicao == null)
            {
                throw new InvalidOperationException($"Eleição {id} não encontrada");
            }

            // Validar se pode excluir
            if (eleicao.Calendarios.Any(c => c.Ativo))
            {
                throw new InvalidOperationException("Não é possível excluir eleição com calendários ativos");
            }

            eleicao.Ativo = false;
            eleicao.Excluido = true;
            eleicao.DataExclusao = DateTime.Now;
            eleicao.UsuarioExclusaoId = usuarioExclusaoId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Eleição {Id} excluída com sucesso", id);

            return true;
        }

        /// <summary>
        /// Obtém estatísticas da eleição
        /// </summary>
        public async Task<EstatisticasEleicaoDTO> ObterEstatisticasEleicaoAsync(int eleicaoId)
        {
            var eleicao = await _context.Eleicoes
                .Include(e => e.Calendarios)
                    .ThenInclude(c => c.Chapas)
                .Include(e => e.Calendarios)
                    .ThenInclude(c => c.Denuncias)
                .Include(e => e.Calendarios)
                    .ThenInclude(c => c.PedidosImpugnacao)
                .FirstOrDefaultAsync(e => e.Id == eleicaoId);

            if (eleicao == null)
            {
                throw new InvalidOperationException($"Eleição {eleicaoId} não encontrada");
            }

            var estatisticas = new EstatisticasEleicaoDTO
            {
                EleicaoId = eleicaoId,
                NomeEleicao = eleicao.Nome,
                Ano = eleicao.Ano,
                TotalCalendarios = eleicao.Calendarios.Count(c => c.Ativo),
                TotalChapas = eleicao.Calendarios.SelectMany(c => c.Chapas).Count(ch => ch.Ativa),
                TotalChapasConfirmadas = eleicao.Calendarios.SelectMany(c => c.Chapas)
                    .Count(ch => ch.Status == StatusChapa.Confirmada),
                TotalDenuncias = eleicao.Calendarios.SelectMany(c => c.Denuncias).Count(),
                TotalImpugnacoes = eleicao.Calendarios.SelectMany(c => c.PedidosImpugnacao).Count(),
                TotalEleitores = await ObterTotalEleitoresAsync(eleicaoId),
                Situacao = eleicao.Situacao.ToString()
            };

            return estatisticas;
        }

        /// <summary>
        /// Obtém tipos de processo disponíveis
        /// </summary>
        public async Task<List<TipoProcesso>> ObterTiposProcessoAsync()
        {
            return await _context.TiposProcesso
                .Where(t => t.Ativo)
                .OrderBy(t => t.Nome)
                .ToListAsync();
        }

        #region Métodos Privados

        private async Task<int> ObterProximaSequenciaAnoAsync(int ano)
        {
            var ultimaSequencia = await _context.Eleicoes
                .Where(e => e.Ano == ano)
                .MaxAsync(e => (int?)e.SequenciaAno) ?? 0;

            return ultimaSequencia + 1;
        }

        private void ValidarTransicaoSituacao(SituacaoEleicao situacaoAtual, SituacaoEleicao novaSituacao)
        {
            // Regras de transição
            var transicoesValidas = new Dictionary<SituacaoEleicao, List<SituacaoEleicao>>
            {
                [SituacaoEleicao.EmAndamento] = new List<SituacaoEleicao> 
                { 
                    SituacaoEleicao.Suspensa, 
                    SituacaoEleicao.Encerrada 
                },
                [SituacaoEleicao.Suspensa] = new List<SituacaoEleicao> 
                { 
                    SituacaoEleicao.EmAndamento, 
                    SituacaoEleicao.Encerrada 
                },
                [SituacaoEleicao.Encerrada] = new List<SituacaoEleicao>() // Não pode mudar
            };

            if (!transicoesValidas.ContainsKey(situacaoAtual) || 
                !transicoesValidas[situacaoAtual].Contains(novaSituacao))
            {
                throw new InvalidOperationException(
                    $"Transição inválida de {situacaoAtual} para {novaSituacao}");
            }
        }

        private async Task<int> ObterTotalEleitoresAsync(int eleicaoId)
        {
            // Buscar total de eleitores aptos para a eleição
            // Isso dependerá da implementação específica do sistema de votação
            return await _context.Profissionais
                .CountAsync(p => p.Ativo && p.PodeVotar);
        }

        private async Task NotificarNovaEleicaoAsync(Eleicao eleicao)
        {
            try
            {
                // Implementar notificação sobre nova eleição
                _logger.LogInformation("Notificando sobre nova eleição {Nome}", eleicao.Nome);
                
                // Aqui você pode enviar emails, notificações push, etc.
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao notificar sobre nova eleição");
            }
        }

        private async Task NotificarMudancaSituacaoAsync(Eleicao eleicao, SituacaoEleicao novaSituacao)
        {
            try
            {
                _logger.LogInformation("Notificando mudança de situação da eleição {Nome} para {Situacao}", 
                    eleicao.Nome, novaSituacao);
                
                // Implementar notificação sobre mudança de situação
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao notificar mudança de situação");
            }
        }

        #endregion
    }
}