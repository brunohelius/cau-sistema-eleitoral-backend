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
using Hangfire;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Service responsável pela lógica de negócio de Impugnações
    /// Gerencia pedidos de impugnação de chapas e candidatos
    /// </summary>
    public class ImpugnacaoService : IImpugnacaoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ImpugnacaoService> _logger;
        private readonly ICalendarioService _calendarioService;
        private readonly INotificationService _notificationService;
        private readonly IChapaEleicaoService _chapaService;
        private readonly IArquivoService _arquivoService;
        
        public ImpugnacaoService(
            ApplicationDbContext context,
            ILogger<ImpugnacaoService> logger,
            ICalendarioService calendarioService,
            INotificationService notificationService,
            IChapaEleicaoService chapaService,
            IArquivoService arquivoService)
        {
            _context = context;
            _logger = logger;
            _calendarioService = calendarioService;
            _notificationService = notificationService;
            _chapaService = chapaService;
            _arquivoService = arquivoService;
        }

        #region Registro de Impugnações

        /// <summary>
        /// Registra um pedido de impugnação
        /// </summary>
        public async Task<PedidoImpugnacaoDTO> RegistrarPedidoImpugnacaoAsync(RegistrarImpugnacaoDTO dto)
        {
            // Validar período para impugnações
            var periodoValido = await _calendarioService.ValidarPeriodoParaAcaoAsync(
                dto.CalendarioId, 
                TipoAtividadeCalendario.Impugnacao);

            if (!periodoValido)
                throw new Exception("Fora do período para registro de impugnações");

            // Validar se a chapa pode ser impugnada
            var chapa = await _context.ChapasEleicao
                .Include(c => c.Calendario)
                .FirstOrDefaultAsync(c => c.Id == dto.ChapaId);

            if (chapa == null)
                throw new Exception("Chapa não encontrada");

            if (chapa.Status != StatusChapa.Confirmada && chapa.Status != StatusChapa.Deferida)
                throw new Exception($"Chapa não pode ser impugnada no status {chapa.Status}");

            // Verificar se já existe impugnação pendente para a mesma chapa pelo mesmo solicitante
            var impugnacaoExistente = await _context.PedidosImpugnacao
                .AnyAsync(p => 
                    p.ChapaId == dto.ChapaId &&
                    p.SolicitanteId == dto.SolicitanteId &&
                    p.Status == StatusPedidoImpugnacao.EmAnalise);

            if (impugnacaoExistente)
                throw new Exception("Já existe um pedido de impugnação em análise para esta chapa");

            // Gerar protocolo
            var protocolo = await GerarProtocoloImpugnacaoAsync(dto.CalendarioId, chapa.UfId);

            // Criar pedido de impugnação
            var pedido = new PedidoImpugnacao
            {
                Protocolo = protocolo,
                CalendarioId = dto.CalendarioId,
                ChapaId = dto.ChapaId,
                SolicitanteId = dto.SolicitanteId,
                TipoImpugnacao = dto.TipoImpugnacao,
                Fundamentacao = dto.Fundamentacao,
                Status = StatusPedidoImpugnacao.EmAnalise,
                DataSolicitacao = DateTime.Now,
                
                // Membro específico se aplicável
                MembroChapaId = dto.MembroChapaId,
                
                // Flags
                Urgente = dto.Urgente,
                Sigiloso = dto.Sigiloso
            };

            // Adicionar documentos comprobatórios
            if (dto.DocumentosComprobatorios != null && dto.DocumentosComprobatorios.Any())
            {
                foreach (var doc in dto.DocumentosComprobatorios)
                {
                    var caminhoArquivo = await _arquivoService.SalvarArquivoAsync(
                        doc.ConteudoArquivo,
                        doc.NomeArquivo,
                        "impugnacoes");

                    pedido.ArquivosPedidoImpugnacao.Add(new ArquivoPedidoImpugnacao
                    {
                        PedidoImpugnacao = pedido,
                        NomeArquivo = doc.NomeArquivo,
                        CaminhoArquivo = caminhoArquivo,
                        TipoArquivo = doc.TipoArquivo,
                        TamanhoBytes = doc.ConteudoArquivo.Length,
                        DataUpload = DateTime.Now
                    });
                }
            }

            // Registrar histórico
            pedido.HistoricoPedido.Add(new HistoricoPedidoImpugnacao
            {
                PedidoImpugnacao = pedido,
                Status = StatusPedidoImpugnacao.EmAnalise,
                Descricao = "Pedido de impugnação registrado",
                DataAlteracao = DateTime.Now,
                UsuarioId = dto.UsuarioRegistroId
            });

            _context.PedidosImpugnacao.Add(pedido);
            await _context.SaveChangesAsync();

            // Notificar responsável da chapa
            await NotificarPedidoImpugnacaoAsync(pedido, chapa);

            // Agendar email de confirmação
            BackgroundJob.Enqueue(() => EnviarEmailPedidoImpugnacaoAsync(pedido.Id));

            _logger.LogInformation($"Pedido de impugnação {protocolo} registrado para chapa {chapa.NumeroChapa}");

            return await ObterPedidoPorIdAsync(pedido.Id);
        }

        /// <summary>
        /// Gera protocolo único para o pedido de impugnação
        /// </summary>
        private async Task<string> GerarProtocoloImpugnacaoAsync(int calendarioId, int ufId)
        {
            var calendario = await _context.Calendarios
                .FirstOrDefaultAsync(c => c.Id == calendarioId);

            var uf = await _context.Ufs
                .FirstOrDefaultAsync(u => u.Id == ufId);

            var sequencial = await _context.PedidosImpugnacao
                .Where(p => p.CalendarioId == calendarioId)
                .CountAsync() + 1;

            // Formato: IMP-ANO-UF-SEQUENCIAL
            return $"IMP-{calendario.Ano}-{uf.Sigla}-{sequencial:D4}";
        }

        #endregion

        #region Defesa de Impugnação

        /// <summary>
        /// Registra defesa contra impugnação
        /// </summary>
        public async Task<bool> RegistrarDefesaImpugnacaoAsync(int pedidoId, RegistrarDefesaImpugnacaoDTO dto)
        {
            var pedido = await _context.PedidosImpugnacao
                .Include(p => p.Chapa)
                .Include(p => p.DefesaImpugnacao)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
                throw new Exception("Pedido de impugnação não encontrado");

            if (pedido.Status != StatusPedidoImpugnacao.EmAnalise)
                throw new Exception($"Não é possível apresentar defesa no status {pedido.Status}");

            if (pedido.DefesaImpugnacao != null)
                throw new Exception("Já existe defesa registrada para este pedido");

            // Verificar prazo para defesa
            var prazoDefesa = pedido.DataSolicitacao.AddDays(5); // 5 dias para defesa
            if (DateTime.Now > prazoDefesa && !dto.ForaDoPrazo)
                throw new Exception("Prazo para defesa expirado");

            // Criar defesa
            var defesa = new DefesaImpugnacao
            {
                PedidoImpugnacaoId = pedidoId,
                Argumentacao = dto.Argumentacao,
                DataApresentacao = DateTime.Now,
                ApresentadaPorId = dto.ApresentadaPorId,
                ForaDoPrazo = dto.ForaDoPrazo
            };

            // Adicionar documentos da defesa
            if (dto.DocumentosDefesa != null && dto.DocumentosDefesa.Any())
            {
                foreach (var doc in dto.DocumentosDefesa)
                {
                    var caminhoArquivo = await _arquivoService.SalvarArquivoAsync(
                        doc.ConteudoArquivo,
                        doc.NomeArquivo,
                        "impugnacoes/defesas");

                    defesa.ArquivosDefesa.Add(new ArquivoDefesaImpugnacao
                    {
                        DefesaImpugnacao = defesa,
                        NomeArquivo = doc.NomeArquivo,
                        CaminhoArquivo = caminhoArquivo,
                        TipoArquivo = doc.TipoArquivo,
                        TamanhoBytes = doc.ConteudoArquivo.Length,
                        DataUpload = DateTime.Now
                    });
                }
            }

            _context.DefesasImpugnacao.Add(defesa);

            // Atualizar status do pedido
            pedido.Status = StatusPedidoImpugnacao.ComDefesa;
            
            // Registrar histórico
            pedido.HistoricoPedido.Add(new HistoricoPedidoImpugnacao
            {
                PedidoImpugnacaoId = pedidoId,
                Status = StatusPedidoImpugnacao.ComDefesa,
                Descricao = "Defesa apresentada",
                DataAlteracao = DateTime.Now,
                UsuarioId = dto.UsuarioRegistroId
            });

            await _context.SaveChangesAsync();

            // Notificar solicitante da impugnação
            await NotificarDefesaApresentadaAsync(pedido);

            _logger.LogInformation($"Defesa registrada para impugnação {pedido.Protocolo}");

            return true;
        }

        #endregion

        #region Julgamento de Impugnação

        /// <summary>
        /// Registra julgamento de impugnação
        /// </summary>
        public async Task<bool> JulgarImpugnacaoAsync(int pedidoId, JulgarImpugnacaoDTO dto)
        {
            var pedido = await _context.PedidosImpugnacao
                .Include(p => p.Chapa)
                .Include(p => p.DefesaImpugnacao)
                .Include(p => p.JulgamentoImpugnacao)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
                throw new Exception("Pedido de impugnação não encontrado");

            if (pedido.Status != StatusPedidoImpugnacao.EmAnalise && 
                pedido.Status != StatusPedidoImpugnacao.ComDefesa)
                throw new Exception($"Não é possível julgar no status {pedido.Status}");

            if (pedido.JulgamentoImpugnacao != null)
                throw new Exception("Impugnação já foi julgada");

            // Criar julgamento
            var julgamento = new JulgamentoImpugnacao
            {
                PedidoImpugnacaoId = pedidoId,
                Decisao = dto.Decisao,
                Fundamentacao = dto.Fundamentacao,
                DataJulgamento = DateTime.Now,
                RelatorId = dto.RelatorId,
                
                // Votos se aplicável
                VotosFavoraveis = dto.VotosFavoraveis,
                VotosContrarios = dto.VotosContrarios,
                Abstencoes = dto.Abstencoes
            };

            // Adicionar documentos do julgamento
            if (dto.DocumentosJulgamento != null && dto.DocumentosJulgamento.Any())
            {
                foreach (var doc in dto.DocumentosJulgamento)
                {
                    var caminhoArquivo = await _arquivoService.SalvarArquivoAsync(
                        doc.ConteudoArquivo,
                        doc.NomeArquivo,
                        "impugnacoes/julgamentos");

                    julgamento.ArquivosJulgamento.Add(new ArquivoJulgamentoImpugnacao
                    {
                        JulgamentoImpugnacao = julgamento,
                        NomeArquivo = doc.NomeArquivo,
                        CaminhoArquivo = caminhoArquivo,
                        TipoArquivo = doc.TipoArquivo,
                        TamanhoBytes = doc.ConteudoArquivo.Length,
                        DataUpload = DateTime.Now
                    });
                }
            }

            _context.JulgamentosImpugnacao.Add(julgamento);

            // Atualizar status do pedido e da chapa conforme decisão
            if (dto.Decisao == DecisaoJulgamento.Procedente)
            {
                pedido.Status = StatusPedidoImpugnacao.Deferido;
                pedido.Chapa.Status = StatusChapa.Impugnada;
                
                // Se for impugnação de membro específico
                if (pedido.MembroChapaId.HasValue)
                {
                    var membro = await _context.MembrosChapa
                        .FirstOrDefaultAsync(m => m.Id == pedido.MembroChapaId.Value);
                    
                    if (membro != null)
                    {
                        membro.Status = StatusMembroChapa.Impugnado;
                        membro.DataSaida = DateTime.Now;
                        membro.MotivoSaida = "Impugnação deferida";
                    }
                }
            }
            else
            {
                pedido.Status = StatusPedidoImpugnacao.Indeferido;
                // Chapa mantém status atual
            }

            // Registrar histórico
            pedido.HistoricoPedido.Add(new HistoricoPedidoImpugnacao
            {
                PedidoImpugnacaoId = pedidoId,
                Status = pedido.Status,
                Descricao = $"Impugnação julgada: {dto.Decisao}",
                DataAlteracao = DateTime.Now,
                UsuarioId = dto.UsuarioJulgamentoId
            });

            await _context.SaveChangesAsync();

            // Notificar partes interessadas
            await NotificarJulgamentoImpugnacaoAsync(pedido, julgamento);

            // Agendar emails
            BackgroundJob.Enqueue(() => EnviarEmailJulgamentoImpugnacaoAsync(pedido.Id));

            _logger.LogInformation($"Impugnação {pedido.Protocolo} julgada: {dto.Decisao}");

            return true;
        }

        #endregion

        #region Recursos

        /// <summary>
        /// Registra recurso contra decisão de impugnação
        /// </summary>
        public async Task<bool> RegistrarRecursoImpugnacaoAsync(int pedidoId, RegistrarRecursoImpugnacaoDTO dto)
        {
            var pedido = await _context.PedidosImpugnacao
                .Include(p => p.JulgamentoImpugnacao)
                .Include(p => p.RecursosImpugnacao)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
                throw new Exception("Pedido de impugnação não encontrado");

            if (pedido.JulgamentoImpugnacao == null)
                throw new Exception("Impugnação ainda não foi julgada");

            if (pedido.Status != StatusPedidoImpugnacao.Deferido && 
                pedido.Status != StatusPedidoImpugnacao.Indeferido)
                throw new Exception("Não é possível recorrer neste status");

            // Verificar prazo para recurso (48 horas após julgamento)
            var prazoRecurso = pedido.JulgamentoImpugnacao.DataJulgamento.AddHours(48);
            if (DateTime.Now > prazoRecurso && !dto.ForaDoPrazo)
                throw new Exception("Prazo para recurso expirado");

            // Verificar se já existe recurso do mesmo recorrente
            var recursoExistente = pedido.RecursosImpugnacao
                .Any(r => r.RecorrenteId == dto.RecorrenteId && r.Status == StatusRecurso.EmAnalise);

            if (recursoExistente)
                throw new Exception("Já existe recurso em análise para este pedido");

            // Criar recurso
            var recurso = new RecursoImpugnacao
            {
                PedidoImpugnacaoId = pedidoId,
                RecorrenteId = dto.RecorrenteId,
                TipoRecorrente = dto.TipoRecorrente,
                Fundamentacao = dto.Fundamentacao,
                Status = StatusRecurso.EmAnalise,
                DataApresentacao = DateTime.Now,
                ForaDoPrazo = dto.ForaDoPrazo
            };

            // Adicionar documentos do recurso
            if (dto.DocumentosRecurso != null && dto.DocumentosRecurso.Any())
            {
                foreach (var doc in dto.DocumentosRecurso)
                {
                    var caminhoArquivo = await _arquivoService.SalvarArquivoAsync(
                        doc.ConteudoArquivo,
                        doc.NomeArquivo,
                        "impugnacoes/recursos");

                    recurso.ArquivosRecurso.Add(new ArquivoRecursoImpugnacao
                    {
                        RecursoImpugnacao = recurso,
                        NomeArquivo = doc.NomeArquivo,
                        CaminhoArquivo = caminhoArquivo,
                        TipoArquivo = doc.TipoArquivo,
                        TamanhoBytes = doc.ConteudoArquivo.Length,
                        DataUpload = DateTime.Now
                    });
                }
            }

            _context.RecursosImpugnacao.Add(recurso);

            // Atualizar status do pedido
            pedido.Status = StatusPedidoImpugnacao.EmRecurso;

            // Registrar histórico
            pedido.HistoricoPedido.Add(new HistoricoPedidoImpugnacao
            {
                PedidoImpugnacaoId = pedidoId,
                Status = StatusPedidoImpugnacao.EmRecurso,
                Descricao = "Recurso apresentado",
                DataAlteracao = DateTime.Now,
                UsuarioId = dto.UsuarioRegistroId
            });

            await _context.SaveChangesAsync();

            // Notificar partes
            await NotificarRecursoApresentadoAsync(pedido, recurso);

            _logger.LogInformation($"Recurso registrado para impugnação {pedido.Protocolo}");

            return true;
        }

        #endregion

        #region Consultas

        /// <summary>
        /// Obtém pedido de impugnação por ID
        /// </summary>
        public async Task<PedidoImpugnacaoDTO> ObterPedidoPorIdAsync(int id)
        {
            var pedido = await _context.PedidosImpugnacao
                .Include(p => p.Calendario)
                .Include(p => p.Chapa)
                    .ThenInclude(c => c.Uf)
                .Include(p => p.MembroChapa)
                    .ThenInclude(m => m.Profissional)
                .Include(p => p.Solicitante)
                .Include(p => p.DefesaImpugnacao)
                .Include(p => p.JulgamentoImpugnacao)
                .Include(p => p.RecursosImpugnacao)
                .Include(p => p.ArquivosPedidoImpugnacao)
                .Include(p => p.HistoricoPedido)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                throw new Exception("Pedido de impugnação não encontrado");

            return MapearParaDTO(pedido);
        }

        /// <summary>
        /// Lista pedidos de impugnação por chapa
        /// </summary>
        public async Task<List<PedidoImpugnacaoDTO>> ObterPedidosPorChapaAsync(int chapaId)
        {
            var pedidos = await _context.PedidosImpugnacao
                .Include(p => p.Solicitante)
                .Include(p => p.JulgamentoImpugnacao)
                .Where(p => p.ChapaId == chapaId)
                .OrderByDescending(p => p.DataSolicitacao)
                .ToListAsync();

            return pedidos.Select(p => MapearParaDTO(p)).ToList();
        }

        /// <summary>
        /// Obtém quantidade de pedidos por UF
        /// </summary>
        public async Task<List<QuantidadeImpugnacaoPorUfDTO>> ObterQuantidadePorUfAsync(int calendarioId)
        {
            var resultado = await _context.PedidosImpugnacao
                .Include(p => p.Chapa)
                    .ThenInclude(c => c.Uf)
                .Where(p => p.CalendarioId == calendarioId)
                .GroupBy(p => new { p.Chapa.UfId, p.Chapa.Uf.Nome, p.Chapa.Uf.Sigla })
                .Select(g => new QuantidadeImpugnacaoPorUfDTO
                {
                    UfId = g.Key.UfId,
                    UfNome = g.Key.Nome,
                    UfSigla = g.Key.Sigla,
                    Total = g.Count(),
                    EmAnalise = g.Count(p => p.Status == StatusPedidoImpugnacao.EmAnalise),
                    Deferidos = g.Count(p => p.Status == StatusPedidoImpugnacao.Deferido),
                    Indeferidos = g.Count(p => p.Status == StatusPedidoImpugnacao.Indeferido),
                    EmRecurso = g.Count(p => p.Status == StatusPedidoImpugnacao.EmRecurso)
                })
                .OrderBy(r => r.UfNome)
                .ToListAsync();

            return resultado;
        }

        #endregion

        #region Métodos Auxiliares

        private async Task NotificarPedidoImpugnacaoAsync(PedidoImpugnacao pedido, ChapaEleicao chapa)
        {
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.ImpugnacaoRecebida,
                Titulo = "Pedido de Impugnação Recebido",
                Mensagem = $"Sua chapa {chapa.NumeroChapa} recebeu um pedido de impugnação",
                ChapaId = chapa.Id,
                DestinatariosIds = new List<int> { chapa.ResponsavelId }
            });
        }

        private async Task NotificarDefesaApresentadaAsync(PedidoImpugnacao pedido)
        {
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.DefesaApresentada,
                Titulo = "Defesa Apresentada",
                Mensagem = $"Foi apresentada defesa para a impugnação {pedido.Protocolo}",
                DestinatariosIds = new List<int> { pedido.SolicitanteId }
            });
        }

        private async Task NotificarJulgamentoImpugnacaoAsync(PedidoImpugnacao pedido, JulgamentoImpugnacao julgamento)
        {
            var decisao = julgamento.Decisao == DecisaoJulgamento.Procedente ? "DEFERIDA" : "INDEFERIDA";
            
            // Notificar solicitante
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.JulgamentoRealizado,
                Titulo = "Impugnação Julgada",
                Mensagem = $"Impugnação {pedido.Protocolo} foi {decisao}",
                DestinatariosIds = new List<int> { pedido.SolicitanteId }
            });

            // Notificar chapa
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.JulgamentoRealizado,
                Titulo = "Resultado de Impugnação",
                Mensagem = $"A impugnação contra sua chapa foi {decisao}",
                ChapaId = pedido.ChapaId,
                DestinatariosIds = new List<int> { pedido.Chapa.ResponsavelId }
            });
        }

        private async Task NotificarRecursoApresentadoAsync(PedidoImpugnacao pedido, RecursoImpugnacao recurso)
        {
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.RecursoApresentado,
                Titulo = "Recurso Apresentado",
                Mensagem = $"Foi apresentado recurso para a impugnação {pedido.Protocolo}",
                DestinatariosIds = new List<int> { pedido.SolicitanteId, pedido.Chapa.ResponsavelId }
            });
        }

        [BackgroundJob]
        public async Task EnviarEmailPedidoImpugnacaoAsync(int pedidoId)
        {
            var pedido = await _context.PedidosImpugnacao
                .Include(p => p.Chapa)
                .Include(p => p.Solicitante)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
                return;

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = new List<string> { pedido.Chapa.ResponsavelEmail },
                Assunto = "Pedido de Impugnação Recebido",
                TemplateId = "PedidoImpugnacao",
                ParametrosTemplate = new Dictionary<string, string>
                {
                    ["NumeroChapa"] = pedido.Chapa.NumeroChapa,
                    ["NomeChapa"] = pedido.Chapa.Nome,
                    ["Protocolo"] = pedido.Protocolo,
                    ["TipoImpugnacao"] = pedido.TipoImpugnacao.ToString(),
                    ["DataLimiteDefesa"] = pedido.DataSolicitacao.AddDays(5).ToString("dd/MM/yyyy"),
                    ["LinkDefesa"] = $"/impugnacoes/{pedido.Id}/defesa"
                }
            });
        }

        [BackgroundJob]
        public async Task EnviarEmailJulgamentoImpugnacaoAsync(int pedidoId)
        {
            var pedido = await _context.PedidosImpugnacao
                .Include(p => p.Chapa)
                .Include(p => p.Solicitante)
                .Include(p => p.JulgamentoImpugnacao)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null || pedido.JulgamentoImpugnacao == null)
                return;

            var decisao = pedido.JulgamentoImpugnacao.Decisao == DecisaoJulgamento.Procedente ? "DEFERIDA" : "INDEFERIDA";

            // Email para o solicitante
            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = new List<string> { pedido.Solicitante.Email },
                Assunto = $"Resultado do Julgamento - Impugnação {pedido.Protocolo}",
                TemplateId = "JulgamentoImpugnacao",
                ParametrosTemplate = new Dictionary<string, string>
                {
                    ["Protocolo"] = pedido.Protocolo,
                    ["Decisao"] = decisao,
                    ["Fundamentacao"] = pedido.JulgamentoImpugnacao.Fundamentacao,
                    ["DataJulgamento"] = pedido.JulgamentoImpugnacao.DataJulgamento.ToString("dd/MM/yyyy"),
                    ["PrazoRecurso"] = pedido.JulgamentoImpugnacao.DataJulgamento.AddHours(48).ToString("dd/MM/yyyy HH:mm")
                }
            });

            // Email para a chapa
            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = new List<string> { pedido.Chapa.ResponsavelEmail },
                Assunto = $"Resultado de Impugnação - Chapa {pedido.Chapa.NumeroChapa}",
                TemplateId = "ResultadoImpugnacaoChapa",
                ParametrosTemplate = new Dictionary<string, string>
                {
                    ["NumeroChapa"] = pedido.Chapa.NumeroChapa,
                    ["NomeChapa"] = pedido.Chapa.Nome,
                    ["Protocolo"] = pedido.Protocolo,
                    ["Decisao"] = decisao,
                    ["Fundamentacao"] = pedido.JulgamentoImpugnacao.Fundamentacao
                }
            });
        }

        private PedidoImpugnacaoDTO MapearParaDTO(PedidoImpugnacao pedido)
        {
            return new PedidoImpugnacaoDTO
            {
                Id = pedido.Id,
                Protocolo = pedido.Protocolo,
                CalendarioId = pedido.CalendarioId,
                ChapaId = pedido.ChapaId,
                NumeroChapa = pedido.Chapa?.NumeroChapa,
                NomeChapa = pedido.Chapa?.Nome,
                MembroChapaId = pedido.MembroChapaId,
                NomeMembroImpugnado = pedido.MembroChapa?.Profissional?.Nome,
                SolicitanteId = pedido.SolicitanteId,
                NomeSolicitante = pedido.Solicitante?.Nome,
                TipoImpugnacao = pedido.TipoImpugnacao.ToString(),
                Fundamentacao = pedido.Fundamentacao,
                Status = pedido.Status.ToString(),
                DataSolicitacao = pedido.DataSolicitacao,
                Urgente = pedido.Urgente,
                Sigiloso = pedido.Sigiloso,
                
                // Defesa
                TemDefesa = pedido.DefesaImpugnacao != null,
                DataDefesa = pedido.DefesaImpugnacao?.DataApresentacao,
                
                // Julgamento
                FoiJulgado = pedido.JulgamentoImpugnacao != null,
                DataJulgamento = pedido.JulgamentoImpugnacao?.DataJulgamento,
                Decisao = pedido.JulgamentoImpugnacao?.Decisao.ToString(),
                
                // Recursos
                QuantidadeRecursos = pedido.RecursosImpugnacao?.Count ?? 0,
                
                // Arquivos
                QuantidadeArquivos = pedido.ArquivosPedidoImpugnacao?.Count ?? 0,
                
                // Histórico
                Historico = pedido.HistoricoPedido?.Select(h => new HistoricoImpugnacaoDTO
                {
                    Status = h.Status.ToString(),
                    Descricao = h.Descricao,
                    DataAlteracao = h.DataAlteracao
                }).ToList()
            };
        }

        #endregion
    }
}