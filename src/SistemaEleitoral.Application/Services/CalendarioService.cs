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
using SistemaEleitoral.Application.Models;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Service responsável pela lógica de negócio de Calendários Eleitorais
    /// Este é o BACKBONE do sistema - controla todos os períodos e validações temporais
    /// </summary>
    public class CalendarioService : ICalendarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CalendarioService> _logger;
        private readonly INotificationService _notificationService;
        private readonly IComissaoEleitoralService _comissaoService;
        
        public CalendarioService(
            ApplicationDbContext context,
            ILogger<CalendarioService> logger,
            INotificationService notificationService,
            IComissaoEleitoralService comissaoService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
            _comissaoService = comissaoService;
        }

        #region Consultas Básicas

        /// <summary>
        /// Retorna os anos que tiveram calendários eleitorais
        /// </summary>
        public async Task<List<int>> ObterAnosComCalendariosAsync()
        {
            return await _context.Calendarios
                .Select(c => c.Ano)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();
        }

        /// <summary>
        /// Retorna lista de anos com eleições concluídas
        /// </summary>
        public async Task<List<int>> ObterAnosCalendariosConcluidosAsync()
        {
            return await _context.Calendarios
                .Where(c => c.Situacao == SituacaoCalendario.Concluido)
                .Select(c => c.Ano)
                .Distinct()
                .OrderByDescending(a => a)
                .ToListAsync();
        }

        /// <summary>
        /// Retorna o calendário por ID com todas as informações
        /// </summary>
        public async Task<CalendarioDTO> ObterPorIdAsync(int id)
        {
            var calendario = await _context.Calendarios
                .Include(c => c.Eleicao)
                .Include(c => c.UfsCalendario)
                    .ThenInclude(uf => uf.Uf)
                .Include(c => c.PrazosCalendario)
                .Include(c => c.AtividadesPrincipais)
                    .ThenInclude(ap => ap.AtividadesSecundarias)
                .Include(c => c.CalendarioSituacoes)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (calendario == null)
                throw new Exception("Calendário não encontrado");

            // Definir progresso da situação
            calendario = DefinirProgressoSituacao(calendario);
            
            return MapearParaDTO(calendario);
        }

        #endregion

        #region Validações Temporais (CRÍTICO)

        /// <summary>
        /// Verifica se uma ação pode ser executada baseada no período do calendário
        /// MÉTODO CRÍTICO - usado em todo o sistema para validações temporais
        /// </summary>
        public async Task<bool> ValidarPeriodoParaAcaoAsync(int calendarioId, TipoAtividadeCalendario tipoAtividade)
        {
            var calendario = await _context.Calendarios
                .Include(c => c.PrazosCalendario)
                .FirstOrDefaultAsync(c => c.Id == calendarioId);

            if (calendario == null)
                return false;

            var prazo = calendario.PrazosCalendario
                .FirstOrDefault(p => p.TipoAtividade == tipoAtividade);

            if (prazo == null)
                return false;

            var dataAtual = DateTime.Now.Date;
            return dataAtual >= prazo.DataInicio && dataAtual <= prazo.DataFim;
        }

        /// <summary>
        /// Valida se o calendário está em período válido para uma UF específica
        /// </summary>
        public async Task<bool> ValidarPeriodoUFAsync(int calendarioId, string uf, TipoAtividadeCalendario tipoAtividade)
        {
            var ufCalendario = await _context.UfCalendarios
                .Include(uc => uc.Calendario)
                    .ThenInclude(c => c.PrazosCalendario)
                .FirstOrDefaultAsync(uc => 
                    uc.CalendarioId == calendarioId && 
                    uc.Uf.Sigla == uf);

            if (ufCalendario == null)
                return false;

            // Verificar se há período específico para a UF
            if (ufCalendario.DataInicioEspecifica.HasValue && ufCalendario.DataFimEspecifica.HasValue)
            {
                var dataAtual = DateTime.Now.Date;
                return dataAtual >= ufCalendario.DataInicioEspecifica.Value && 
                       dataAtual <= ufCalendario.DataFimEspecifica.Value;
            }

            // Usar período geral do calendário
            return await ValidarPeriodoParaAcaoAsync(calendarioId, tipoAtividade);
        }

        /// <summary>
        /// Retorna o período atual do calendário
        /// </summary>
        public async Task<PeriodoCalendarioDTO> ObterPeriodoAtualAsync(int calendarioId)
        {
            var calendario = await _context.Calendarios
                .Include(c => c.PrazosCalendario)
                .Include(c => c.AtividadesPrincipais)
                .FirstOrDefaultAsync(c => c.Id == calendarioId);

            if (calendario == null)
                throw new Exception("Calendário não encontrado");

            var dataAtual = DateTime.Now.Date;
            
            // Identificar atividade principal atual
            var atividadeAtual = calendario.AtividadesPrincipais
                .Where(ap => dataAtual >= ap.DataInicio && dataAtual <= ap.DataFim)
                .OrderBy(ap => ap.Ordem)
                .FirstOrDefault();

            return new PeriodoCalendarioDTO
            {
                CalendarioId = calendarioId,
                AtividadeAtual = atividadeAtual?.Nome,
                DataInicioAtividade = atividadeAtual?.DataInicio,
                DataFimAtividade = atividadeAtual?.DataFim,
                DiasRestantes = atividadeAtual != null ? 
                    (atividadeAtual.DataFim - dataAtual).Days : 0,
                PermiteRegistroChapa = await ValidarPeriodoParaAcaoAsync(calendarioId, TipoAtividadeCalendario.RegistroChapa),
                PermiteImpugnacao = await ValidarPeriodoParaAcaoAsync(calendarioId, TipoAtividadeCalendario.Impugnacao),
                PermiteVotacao = await ValidarPeriodoParaAcaoAsync(calendarioId, TipoAtividadeCalendario.Votacao)
            };
        }

        #endregion

        #region Criação e Duplicação de Calendários

        /// <summary>
        /// Cria um novo calendário eleitoral
        /// </summary>
        public async Task<CalendarioDTO> CriarCalendarioAsync(CriarCalendarioDTO dto)
        {
            // Validar se já existe calendário para o ano/eleição
            var calendarioExistente = await _context.Calendarios
                .AnyAsync(c => c.Ano == dto.Ano && c.EleicaoId == dto.EleicaoId);

            if (calendarioExistente)
                throw new Exception($"Já existe calendário para o ano {dto.Ano} e eleição informada");

            var calendario = new Calendario
            {
                Ano = dto.Ano,
                EleicaoId = dto.EleicaoId,
                TipoProcessoId = dto.TipoProcessoId,
                NumeroProcesso = dto.NumeroProcesso,
                LinkResolucao = dto.LinkResolucao,
                Situacao = SituacaoCalendario.EmElaboracao,
                DataCriacao = DateTime.Now,
                UsuarioCriacaoId = dto.UsuarioCriacaoId
            };

            // Adicionar UFs
            foreach (var uf in dto.Ufs)
            {
                calendario.UfsCalendario.Add(new UfCalendario
                {
                    Calendario = calendario,
                    UfId = uf
                });
            }

            // Criar estrutura básica de atividades se solicitado
            if (dto.CriarEstruturaPadrao)
            {
                await CriarEstruturaPadraoCalendarioAsync(calendario);
            }

            _context.Calendarios.Add(calendario);
            await _context.SaveChangesAsync();

            // Registrar histórico
            await RegistrarHistoricoCalendarioAsync(calendario.Id, "Calendário criado", dto.UsuarioCriacaoId);

            _logger.LogInformation($"Calendário {calendario.Id} criado para o ano {calendario.Ano}");

            return await ObterPorIdAsync(calendario.Id);
        }

        /// <summary>
        /// Duplica um calendário existente para um novo ano
        /// </summary>
        public async Task<CalendarioDTO> DuplicarCalendarioAsync(int calendarioOrigemId, int novoAno, int usuarioId)
        {
            var calendarioOrigem = await _context.Calendarios
                .Include(c => c.UfsCalendario)
                .Include(c => c.PrazosCalendario)
                .Include(c => c.AtividadesPrincipais)
                    .ThenInclude(ap => ap.AtividadesSecundarias)
                .FirstOrDefaultAsync(c => c.Id == calendarioOrigemId);

            if (calendarioOrigem == null)
                throw new Exception("Calendário de origem não encontrado");

            // Verificar se já existe calendário para o novo ano
            var calendarioExistente = await _context.Calendarios
                .AnyAsync(c => c.Ano == novoAno && c.EleicaoId == calendarioOrigem.EleicaoId);

            if (calendarioExistente)
                throw new Exception($"Já existe calendário para o ano {novoAno}");

            // Criar novo calendário
            var novoCalendario = new Calendario
            {
                Ano = novoAno,
                EleicaoId = calendarioOrigem.EleicaoId,
                TipoProcessoId = calendarioOrigem.TipoProcessoId,
                NumeroProcesso = $"{calendarioOrigem.NumeroProcesso}-{novoAno}",
                LinkResolucao = calendarioOrigem.LinkResolucao,
                Situacao = SituacaoCalendario.EmElaboracao,
                DataCriacao = DateTime.Now,
                UsuarioCriacaoId = usuarioId
            };

            // Copiar UFs
            foreach (var uf in calendarioOrigem.UfsCalendario)
            {
                novoCalendario.UfsCalendario.Add(new UfCalendario
                {
                    Calendario = novoCalendario,
                    UfId = uf.UfId,
                    DataInicioEspecifica = AjustarDataParaNovoAno(uf.DataInicioEspecifica, calendarioOrigem.Ano, novoAno),
                    DataFimEspecifica = AjustarDataParaNovoAno(uf.DataFimEspecifica, calendarioOrigem.Ano, novoAno)
                });
            }

            // Copiar prazos ajustando as datas
            foreach (var prazo in calendarioOrigem.PrazosCalendario)
            {
                novoCalendario.PrazosCalendario.Add(new PrazoCalendario
                {
                    Calendario = novoCalendario,
                    Nome = prazo.Nome,
                    Descricao = prazo.Descricao,
                    TipoAtividade = prazo.TipoAtividade,
                    DataInicio = AjustarDataParaNovoAno(prazo.DataInicio, calendarioOrigem.Ano, novoAno).Value,
                    DataFim = AjustarDataParaNovoAno(prazo.DataFim, calendarioOrigem.Ano, novoAno).Value,
                    Ordem = prazo.Ordem
                });
            }

            // Copiar atividades principais e secundárias
            foreach (var atividadePrincipal in calendarioOrigem.AtividadesPrincipais.OrderBy(ap => ap.Ordem))
            {
                var novaAtividade = new AtividadePrincipalCalendario
                {
                    Calendario = novoCalendario,
                    Nome = atividadePrincipal.Nome,
                    Descricao = atividadePrincipal.Descricao,
                    DataInicio = AjustarDataParaNovoAno(atividadePrincipal.DataInicio, calendarioOrigem.Ano, novoAno).Value,
                    DataFim = AjustarDataParaNovoAno(atividadePrincipal.DataFim, calendarioOrigem.Ano, novoAno).Value,
                    Ordem = atividadePrincipal.Ordem,
                    Ativo = true
                };

                // Copiar atividades secundárias
                foreach (var atividadeSecundaria in atividadePrincipal.AtividadesSecundarias)
                {
                    novaAtividade.AtividadesSecundarias.Add(new AtividadeSecundariaCalendario
                    {
                        AtividadePrincipal = novaAtividade,
                        Nome = atividadeSecundaria.Nome,
                        Descricao = atividadeSecundaria.Descricao,
                        DataInicio = AjustarDataParaNovoAno(atividadeSecundaria.DataInicio, calendarioOrigem.Ano, novoAno).Value,
                        DataFim = AjustarDataParaNovoAno(atividadeSecundaria.DataFim, calendarioOrigem.Ano, novoAno).Value,
                        TipoAtividade = atividadeSecundaria.TipoAtividade,
                        EnviarEmail = atividadeSecundaria.EnviarEmail,
                        DiasAntecedenciaEmail = atividadeSecundaria.DiasAntecedenciaEmail,
                        Ordem = atividadeSecundaria.Ordem,
                        Ativo = true
                    });
                }

                novoCalendario.AtividadesPrincipais.Add(novaAtividade);
            }

            _context.Calendarios.Add(novoCalendario);
            await _context.SaveChangesAsync();

            // Registrar histórico
            await RegistrarHistoricoCalendarioAsync(
                novoCalendario.Id, 
                $"Calendário duplicado a partir do calendário {calendarioOrigemId}", 
                usuarioId);

            _logger.LogInformation($"Calendário {calendarioOrigemId} duplicado para o ano {novoAno}. Novo ID: {novoCalendario.Id}");

            return await ObterPorIdAsync(novoCalendario.Id);
        }

        #endregion

        #region Gestão de Estados e Transições

        /// <summary>
        /// Altera a situação do calendário
        /// </summary>
        public async Task<bool> AlterarSituacaoAsync(int calendarioId, SituacaoCalendario novaSituacao, int usuarioId)
        {
            var calendario = await _context.Calendarios
                .Include(c => c.CalendarioSituacoes)
                .FirstOrDefaultAsync(c => c.Id == calendarioId);

            if (calendario == null)
                throw new Exception("Calendário não encontrado");

            var situacaoAnterior = calendario.Situacao;

            // Validar transição de estado
            if (!ValidarTransicaoSituacao(situacaoAnterior, novaSituacao))
                throw new Exception($"Não é possível alterar de {situacaoAnterior} para {novaSituacao}");

            // Validações específicas por situação
            if (novaSituacao == SituacaoCalendario.Publicado)
            {
                await ValidarCalendarioParaPublicacaoAsync(calendario);
            }

            // Atualizar situação
            calendario.Situacao = novaSituacao;
            
            // Registrar mudança de situação
            calendario.CalendarioSituacoes.Add(new CalendarioSituacao
            {
                CalendarioId = calendarioId,
                Situacao = novaSituacao,
                DataAlteracao = DateTime.Now,
                UsuarioId = usuarioId,
                Observacao = $"Situação alterada de {situacaoAnterior} para {novaSituacao}"
            });

            await _context.SaveChangesAsync();

            // Registrar histórico
            await RegistrarHistoricoCalendarioAsync(
                calendarioId, 
                $"Situação alterada de {situacaoAnterior} para {novaSituacao}", 
                usuarioId);

            // Notificar interessados se publicado
            if (novaSituacao == SituacaoCalendario.Publicado)
            {
                await NotificarPublicacaoCalendarioAsync(calendario);
            }

            _logger.LogInformation($"Situação do calendário {calendarioId} alterada de {situacaoAnterior} para {novaSituacao}");

            return true;
        }

        /// <summary>
        /// Valida se a transição de situação é permitida
        /// </summary>
        private bool ValidarTransicaoSituacao(SituacaoCalendario atual, SituacaoCalendario nova)
        {
            var transicoesPermitidas = new Dictionary<SituacaoCalendario, List<SituacaoCalendario>>
            {
                [SituacaoCalendario.EmElaboracao] = new List<SituacaoCalendario> 
                { 
                    SituacaoCalendario.Publicado, 
                    SituacaoCalendario.Cancelado 
                },
                [SituacaoCalendario.Publicado] = new List<SituacaoCalendario> 
                { 
                    SituacaoCalendario.EmAndamento, 
                    SituacaoCalendario.Suspenso, 
                    SituacaoCalendario.Cancelado 
                },
                [SituacaoCalendario.EmAndamento] = new List<SituacaoCalendario> 
                { 
                    SituacaoCalendario.Suspenso, 
                    SituacaoCalendario.Concluido, 
                    SituacaoCalendario.Cancelado 
                },
                [SituacaoCalendario.Suspenso] = new List<SituacaoCalendario> 
                { 
                    SituacaoCalendario.EmAndamento, 
                    SituacaoCalendario.Cancelado 
                },
                [SituacaoCalendario.Concluido] = new List<SituacaoCalendario>(), // Estado final
                [SituacaoCalendario.Cancelado] = new List<SituacaoCalendario>()  // Estado final
            };

            return transicoesPermitidas.ContainsKey(atual) && 
                   transicoesPermitidas[atual].Contains(nova);
        }

        #endregion

        #region Métodos Auxiliares

        private DateTime? AjustarDataParaNovoAno(DateTime? data, int anoOrigem, int anoNovo)
        {
            if (!data.HasValue)
                return null;

            var diferenca = anoNovo - anoOrigem;
            return data.Value.AddYears(diferenca);
        }

        private Calendario DefinirProgressoSituacao(Calendario calendario)
        {
            // Implementar lógica para calcular progresso baseado nas atividades concluídas
            var totalAtividades = calendario.AtividadesPrincipais.Count;
            if (totalAtividades == 0)
            {
                calendario.ProgressoPercentual = 0;
                return calendario;
            }

            var dataAtual = DateTime.Now.Date;
            var atividadesConcluidas = calendario.AtividadesPrincipais
                .Count(ap => ap.DataFim < dataAtual);

            calendario.ProgressoPercentual = (atividadesConcluidas * 100) / totalAtividades;
            
            return calendario;
        }

        private async Task CriarEstruturaPadraoCalendarioAsync(Calendario calendario)
        {
            // Criar estrutura padrão de atividades para eleições
            var atividades = new List<AtividadePrincipalCalendario>
            {
                new AtividadePrincipalCalendario
                {
                    Nome = "Período de Registro de Chapas",
                    Descricao = "Período para registro de chapas eleitorais",
                    Ordem = 1,
                    Ativo = true
                },
                new AtividadePrincipalCalendario
                {
                    Nome = "Análise de Documentação",
                    Descricao = "Análise e validação das chapas registradas",
                    Ordem = 2,
                    Ativo = true
                },
                new AtividadePrincipalCalendario
                {
                    Nome = "Período de Impugnações",
                    Descricao = "Período para apresentação de impugnações",
                    Ordem = 3,
                    Ativo = true
                },
                new AtividadePrincipalCalendario
                {
                    Nome = "Campanha Eleitoral",
                    Descricao = "Período de campanha eleitoral",
                    Ordem = 4,
                    Ativo = true
                },
                new AtividadePrincipalCalendario
                {
                    Nome = "Votação",
                    Descricao = "Período de votação",
                    Ordem = 5,
                    Ativo = true
                },
                new AtividadePrincipalCalendario
                {
                    Nome = "Apuração e Divulgação",
                    Descricao = "Apuração dos votos e divulgação dos resultados",
                    Ordem = 6,
                    Ativo = true
                }
            };

            calendario.AtividadesPrincipais = atividades;
        }

        private async Task ValidarCalendarioParaPublicacaoAsync(Calendario calendario)
        {
            // Validar se o calendário tem todas as informações necessárias para publicação
            if (!calendario.UfsCalendario.Any())
                throw new Exception("Calendário deve ter pelo menos uma UF");

            if (!calendario.AtividadesPrincipais.Any())
                throw new Exception("Calendário deve ter atividades definidas");

            if (!calendario.PrazosCalendario.Any())
                throw new Exception("Calendário deve ter prazos definidos");

            // Validar se existe comissão eleitoral para todas as UFs
            foreach (var uf in calendario.UfsCalendario)
            {
                var temComissao = await _comissaoService.ExisteComissaoParaUFAsync(uf.UfId, calendario.EleicaoId);
                if (!temComissao)
                    throw new Exception($"Não existe comissão eleitoral definida para a UF {uf.Uf?.Sigla}");
            }
        }

        private async Task NotificarPublicacaoCalendarioAsync(Calendario calendario)
        {
            // Notificar todos os interessados sobre a publicação do calendário
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.CalendarioPublicado,
                Titulo = "Calendário Eleitoral Publicado",
                Mensagem = $"O calendário eleitoral para {calendario.Eleicao?.Nome} {calendario.Ano} foi publicado.",
                CalendarioId = calendario.Id
            });
        }

        private async Task RegistrarHistoricoCalendarioAsync(int calendarioId, string descricao, int usuarioId)
        {
            var historico = new HistoricoCalendario
            {
                CalendarioId = calendarioId,
                Descricao = descricao,
                DataAlteracao = DateTime.Now,
                UsuarioId = usuarioId
            };

            _context.HistoricosCalendario.Add(historico);
            await _context.SaveChangesAsync();
        }

        private CalendarioDTO MapearParaDTO(Calendario calendario)
        {
            return new CalendarioDTO
            {
                Id = calendario.Id,
                Ano = calendario.Ano,
                EleicaoId = calendario.EleicaoId,
                EleicaoNome = calendario.Eleicao?.Nome,
                Situacao = calendario.Situacao.ToString(),
                NumeroProcesso = calendario.NumeroProcesso,
                LinkResolucao = calendario.LinkResolucao,
                ProgressoPercentual = calendario.ProgressoPercentual,
                Ufs = calendario.UfsCalendario.Select(uf => uf.Uf?.Sigla).ToList(),
                AtividadesPrincipais = calendario.AtividadesPrincipais
                    .OrderBy(ap => ap.Ordem)
                    .Select(ap => new AtividadePrincipalDTO
                    {
                        Id = ap.Id,
                        Nome = ap.Nome,
                        Descricao = ap.Descricao,
                        DataInicio = ap.DataInicio,
                        DataFim = ap.DataFim,
                        Ordem = ap.Ordem,
                        Ativo = ap.Ativo
                    }).ToList()
            };
        }

        #endregion
    }
}