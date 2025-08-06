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
using Hangfire;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Service responsável pela lógica de negócio de Chapas Eleitorais
    /// Este é o CORE do processo eleitoral - gerencia todo o ciclo de vida das chapas
    /// </summary>
    public class ChapaEleicaoService : IChapaEleicaoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChapaEleicaoService> _logger;
        private readonly ICalendarioService _calendarioService;
        private readonly INotificationService _notificationService;
        private readonly IValidacaoElegibilidadeService _validacaoService;
        private readonly IMembroChapaService _membroChapaService;
        
        public ChapaEleicaoService(
            ApplicationDbContext context,
            ILogger<ChapaEleicaoService> logger,
            ICalendarioService calendarioService,
            INotificationService notificationService,
            IValidacaoElegibilidadeService validacaoService,
            IMembroChapaService membroChapaService)
        {
            _context = context;
            _logger = logger;
            _calendarioService = calendarioService;
            _notificationService = notificationService;
            _validacaoService = validacaoService;
            _membroChapaService = membroChapaService;
        }

        #region Registro e Criação de Chapas

        /// <summary>
        /// Registra uma nova chapa eleitoral
        /// </summary>
        public async Task<ChapaEleicaoDTO> RegistrarChapaAsync(RegistrarChapaDTO dto)
        {
            // Validar período de registro
            var periodoValido = await _calendarioService.ValidarPeriodoParaAcaoAsync(
                dto.CalendarioId, 
                TipoAtividadeCalendario.RegistroChapa);

            if (!periodoValido)
                throw new Exception("Fora do período de registro de chapas");

            // Validar se o responsável já tem chapa registrada
            var chapaExistente = await _context.ChapasEleicao
                .AnyAsync(c => 
                    c.CalendarioId == dto.CalendarioId && 
                    c.ResponsavelId == dto.ResponsavelId &&
                    c.Status != StatusChapa.Cancelada);

            if (chapaExistente)
                throw new Exception("Responsável já possui chapa registrada para esta eleição");

            // Gerar número da chapa automaticamente
            var numeroChapa = await GerarNumeroChapá(dto.CalendarioId, dto.UfId);

            // Criar nova chapa
            var chapa = new ChapaEleicao
            {
                CalendarioId = dto.CalendarioId,
                UfId = dto.UfId,
                ResponsavelId = dto.ResponsavelId,
                NumeroChapa = numeroChapa,
                Nome = dto.NomeChapa,
                Slogan = dto.Slogan,
                Status = StatusChapa.EmElaboracao,
                DataRegistro = DateTime.Now,
                UsuarioCriacaoId = dto.UsuarioCriacaoId,
                
                // Informações de diversidade (obrigatórias)
                QuantidadeHomens = 0,
                QuantidadeMulheres = 0,
                QuantidadeNegros = 0,
                QuantidadePcD = 0,
                QuantidadeLGBTQI = 0
            };

            // Adicionar responsável como primeiro membro
            chapa.Membros.Add(new MembroChapa
            {
                Chapa = chapa,
                ProfissionalId = dto.ResponsavelId,
                TipoParticipacao = TipoParticipacaoMembro.Titular,
                Cargo = "Responsável pela Chapa",
                DataInclusao = DateTime.Now,
                Status = StatusMembroChapa.Confirmado,
                IsResponsavel = true
            });

            _context.ChapasEleicao.Add(chapa);
            await _context.SaveChangesAsync();

            // Registrar histórico
            await RegistrarHistoricoChapaAsync(chapa.Id, "Chapa registrada", dto.UsuarioCriacaoId);

            // Notificar responsável
            await NotificarRegistroChapaAsync(chapa);

            _logger.LogInformation($"Chapa {chapa.NumeroChapa} registrada com sucesso");

            return await ObterChapaPorIdAsync(chapa.Id);
        }

        /// <summary>
        /// Gera número automático para a chapa
        /// </summary>
        private async Task<string> GerarNumeroChapá(int calendarioId, int ufId)
        {
            var calendario = await _context.Calendarios
                .Include(c => c.Eleicao)
                .FirstOrDefaultAsync(c => c.Id == calendarioId);

            var uf = await _context.Ufs
                .FirstOrDefaultAsync(u => u.Id == ufId);

            // Obter próximo número sequencial para a UF
            var ultimaChapa = await _context.ChapasEleicao
                .Where(c => c.CalendarioId == calendarioId && c.UfId == ufId)
                .OrderByDescending(c => c.NumeroChapa)
                .FirstOrDefaultAsync();

            int proximoNumero = 1;
            if (ultimaChapa != null && !string.IsNullOrEmpty(ultimaChapa.NumeroChapa))
            {
                // Extrair número da última chapa
                var partes = ultimaChapa.NumeroChapa.Split('-');
                if (partes.Length > 0 && int.TryParse(partes.Last(), out int ultimoNumero))
                {
                    proximoNumero = ultimoNumero + 1;
                }
            }

            // Formato: ANO-UF-TIPO-NUMERO
            return $"{calendario.Ano}-{uf.Sigla}-{calendario.Eleicao.Sigla}-{proximoNumero:D3}";
        }

        #endregion

        #region Gestão de Membros da Chapa

        /// <summary>
        /// Adiciona um membro à chapa
        /// </summary>
        public async Task<bool> AdicionarMembroAsync(int chapaId, AdicionarMembroChapaDTO dto)
        {
            var chapa = await _context.ChapasEleicao
                .Include(c => c.Membros)
                .Include(c => c.Calendario)
                .FirstOrDefaultAsync(c => c.Id == chapaId);

            if (chapa == null)
                throw new Exception("Chapa não encontrada");

            // Validar status da chapa
            if (chapa.Status != StatusChapa.EmElaboracao && chapa.Status != StatusChapa.EmAnalise)
                throw new Exception("Chapa não pode receber novos membros neste status");

            // Validar período
            var periodoValido = await _calendarioService.ValidarPeriodoParaAcaoAsync(
                chapa.CalendarioId, 
                TipoAtividadeCalendario.RegistroChapa);

            if (!periodoValido)
                throw new Exception("Fora do período para alterações na chapa");

            // Validar se o profissional já está em outra chapa
            var profissionalEmOutraChapa = await _context.MembrosChapa
                .Include(m => m.Chapa)
                .AnyAsync(m => 
                    m.ProfissionalId == dto.ProfissionalId &&
                    m.Chapa.CalendarioId == chapa.CalendarioId &&
                    m.Chapa.Id != chapaId &&
                    m.Status == StatusMembroChapa.Confirmado);

            if (profissionalEmOutraChapa)
                throw new Exception("Profissional já está registrado em outra chapa");

            // Validar elegibilidade
            var elegibilidade = await _validacaoService.ValidarElegibilidadeAsync(dto.ProfissionalId);
            if (!elegibilidade.IsElegivel)
                throw new Exception($"Profissional não é elegível: {string.Join(", ", elegibilidade.Restricoes)}");

            // Adicionar membro
            var novoMembro = new MembroChapa
            {
                ChapaId = chapaId,
                ProfissionalId = dto.ProfissionalId,
                TipoParticipacao = dto.TipoParticipacao,
                Cargo = dto.Cargo,
                DataInclusao = DateTime.Now,
                Status = StatusMembroChapa.ConvitePendente,
                IsResponsavel = false
            };

            chapa.Membros.Add(novoMembro);
            
            // Atualizar contadores de diversidade
            await AtualizarDiversidadeChapaAsync(chapa);

            await _context.SaveChangesAsync();

            // Enviar convite ao membro
            await EnviarConviteMembroAsync(novoMembro);

            // Registrar histórico
            await RegistrarHistoricoChapaAsync(chapaId, $"Membro {dto.ProfissionalId} adicionado", dto.UsuarioAdicaoId);

            _logger.LogInformation($"Membro {dto.ProfissionalId} adicionado à chapa {chapaId}");

            return true;
        }

        /// <summary>
        /// Confirma participação de um membro na chapa
        /// </summary>
        public async Task<bool> ConfirmarParticipaçãoMembroAsync(int chapaId, int membroId, int profissionalId)
        {
            var membro = await _context.MembrosChapa
                .Include(m => m.Chapa)
                .FirstOrDefaultAsync(m => 
                    m.Id == membroId && 
                    m.ChapaId == chapaId &&
                    m.ProfissionalId == profissionalId);

            if (membro == null)
                throw new Exception("Membro não encontrado ou não autorizado");

            if (membro.Status != StatusMembroChapa.ConvitePendente)
                throw new Exception("Convite não está pendente");

            membro.Status = StatusMembroChapa.Confirmado;
            membro.DataConfirmacao = DateTime.Now;

            await _context.SaveChangesAsync();

            // Notificar responsável da chapa
            await NotificarConfirmacaoMembroAsync(membro);

            // Registrar histórico
            await RegistrarHistoricoChapaAsync(chapaId, $"Membro {profissionalId} confirmou participação", profissionalId);

            return true;
        }

        #endregion

        #region Confirmação e Finalização da Chapa

        /// <summary>
        /// Confirma e finaliza o registro da chapa
        /// </summary>
        public async Task<bool> ConfirmarChapaAsync(int chapaId, ConfirmarChapaDTO dto)
        {
            var chapa = await _context.ChapasEleicao
                .Include(c => c.Membros)
                .Include(c => c.Calendario)
                    .ThenInclude(cal => cal.Eleicao)
                .Include(c => c.DocumentosComprobatorios)
                .FirstOrDefaultAsync(c => c.Id == chapaId);

            if (chapa == null)
                throw new Exception("Chapa não encontrada");

            // Validar status
            if (chapa.Status != StatusChapa.EmElaboracao && chapa.Status != StatusChapa.EmAnalise)
                throw new Exception("Chapa não pode ser confirmada neste status");

            // Validar período
            var periodoValido = await _calendarioService.ValidarPeriodoParaAcaoAsync(
                chapa.CalendarioId, 
                TipoAtividadeCalendario.RegistroChapa);

            if (!periodoValido)
                throw new Exception("Fora do período para confirmação de chapas");

            // Validar composição mínima
            await ValidarComposicaoMinimaAsync(chapa);

            // Validar diversidade obrigatória
            await ValidarDiversidadeAsync(chapa);

            // Validar documentação obrigatória
            await ValidarDocumentacaoObrigatoriaAsync(chapa);

            // Validar elegibilidade de todos os membros
            foreach (var membro in chapa.Membros.Where(m => m.Status == StatusMembroChapa.Confirmado))
            {
                var elegibilidade = await _validacaoService.ValidarElegibilidadeAsync(membro.ProfissionalId);
                if (!elegibilidade.IsElegivel)
                {
                    throw new Exception($"Membro {membro.ProfissionalId} não é elegível: {string.Join(", ", elegibilidade.Restricoes)}");
                }
            }

            // Confirmar chapa
            chapa.Status = StatusChapa.Confirmada;
            chapa.DataConfirmacao = DateTime.Now;
            chapa.UsuarioConfirmacaoId = dto.UsuarioConfirmacaoId;

            // Aceitar termos
            chapa.AceitouTermos = true;
            chapa.DataAceiteTermos = DateTime.Now;
            chapa.IpAceiteTermos = dto.IpConfirmacao;

            await _context.SaveChangesAsync();

            // Enviar email de confirmação
            BackgroundJob.Enqueue(() => EnviarEmailChapaConfirmadaAsync(chapaId));

            // Registrar histórico
            await RegistrarHistoricoChapaAsync(chapaId, "Chapa confirmada", dto.UsuarioConfirmacaoId);

            _logger.LogInformation($"Chapa {chapa.NumeroChapa} confirmada com sucesso");

            return true;
        }

        /// <summary>
        /// Valida se a chapa tem a composição mínima necessária
        /// </summary>
        private async Task ValidarComposicaoMinimaAsync(ChapaEleicao chapa)
        {
            var parametros = await _context.ParametrosConselheiro
                .FirstOrDefaultAsync(p => p.EleicaoId == chapa.Calendario.EleicaoId);

            if (parametros == null)
                throw new Exception("Parâmetros da eleição não configurados");

            var membrosConfirmados = chapa.Membros
                .Count(m => m.Status == StatusMembroChapa.Confirmado);

            var minimoTitulares = parametros.QuantidadeTitulares;
            var minimoSuplentes = parametros.QuantidadeSuplentes;
            var totalMinimo = minimoTitulares + minimoSuplentes;

            if (membrosConfirmados < totalMinimo)
            {
                throw new Exception($"Chapa deve ter no mínimo {totalMinimo} membros ({minimoTitulares} titulares e {minimoSuplentes} suplentes)");
            }

            var titulares = chapa.Membros
                .Count(m => m.Status == StatusMembroChapa.Confirmado && m.TipoParticipacao == TipoParticipacaoMembro.Titular);

            var suplentes = chapa.Membros
                .Count(m => m.Status == StatusMembroChapa.Confirmado && m.TipoParticipacao == TipoParticipacaoMembro.Suplente);

            if (titulares < minimoTitulares)
                throw new Exception($"Chapa deve ter no mínimo {minimoTitulares} titulares");

            if (suplentes < minimoSuplentes)
                throw new Exception($"Chapa deve ter no mínimo {minimoSuplentes} suplentes");
        }

        /// <summary>
        /// Valida requisitos de diversidade da chapa
        /// </summary>
        private async Task ValidarDiversidadeAsync(ChapaEleicao chapa)
        {
            var parametros = await _context.ParametrosConselheiro
                .FirstOrDefaultAsync(p => p.EleicaoId == chapa.Calendario.EleicaoId);

            if (parametros == null)
                return;

            // Validar porcentagem mínima de mulheres
            if (parametros.PercentualMinimoMulheres > 0)
            {
                var totalMembros = chapa.Membros.Count(m => m.Status == StatusMembroChapa.Confirmado);
                var percentualMulheres = (chapa.QuantidadeMulheres * 100) / totalMembros;
                
                if (percentualMulheres < parametros.PercentualMinimoMulheres)
                {
                    throw new Exception($"Chapa deve ter no mínimo {parametros.PercentualMinimoMulheres}% de mulheres");
                }
            }

            // Validar porcentagem mínima de negros
            if (parametros.PercentualMinimoNegros > 0)
            {
                var totalMembros = chapa.Membros.Count(m => m.Status == StatusMembroChapa.Confirmado);
                var percentualNegros = (chapa.QuantidadeNegros * 100) / totalMembros;
                
                if (percentualNegros < parametros.PercentualMinimoNegros)
                {
                    throw new Exception($"Chapa deve ter no mínimo {parametros.PercentualMinimoNegros}% de negros");
                }
            }
        }

        /// <summary>
        /// Valida se todos os documentos obrigatórios foram anexados
        /// </summary>
        private async Task ValidarDocumentacaoObrigatoriaAsync(ChapaEleicao chapa)
        {
            var documentosObrigatorios = new List<string>
            {
                "Plataforma Eleitoral",
                "Termo de Compromisso",
                "Declaração de Elegibilidade",
                "Plano de Gestão"
            };

            foreach (var tipoDocumento in documentosObrigatorios)
            {
                var temDocumento = chapa.DocumentosComprobatorios
                    .Any(d => d.TipoDocumento == tipoDocumento && d.Ativo);

                if (!temDocumento)
                {
                    throw new Exception($"Documento obrigatório não anexado: {tipoDocumento}");
                }
            }
        }

        #endregion

        #region Consultas e Listagens

        /// <summary>
        /// Obtém chapa por ID com todas as informações
        /// </summary>
        public async Task<ChapaEleicaoDTO> ObterChapaPorIdAsync(int id)
        {
            var chapa = await _context.ChapasEleicao
                .Include(c => c.Calendario)
                    .ThenInclude(cal => cal.Eleicao)
                .Include(c => c.Uf)
                .Include(c => c.Membros)
                    .ThenInclude(m => m.Profissional)
                .Include(c => c.DocumentosComprobatorios)
                .Include(c => c.RedesSociais)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chapa == null)
                throw new Exception("Chapa não encontrada");

            return MapearParaDTO(chapa);
        }

        /// <summary>
        /// Lista chapas com filtros
        /// </summary>
        public async Task<ListaChapasPaginadaDTO> ListarChapasAsync(FiltroChapasDTO filtro)
        {
            var query = _context.ChapasEleicao
                .Include(c => c.Calendario)
                    .ThenInclude(cal => cal.Eleicao)
                .Include(c => c.Uf)
                .Include(c => c.Membros)
                .AsQueryable();

            // Aplicar filtros
            if (filtro.CalendarioId.HasValue)
                query = query.Where(c => c.CalendarioId == filtro.CalendarioId.Value);

            if (filtro.UfId.HasValue)
                query = query.Where(c => c.UfId == filtro.UfId.Value);

            if (!string.IsNullOrEmpty(filtro.Status))
            {
                if (Enum.TryParse<StatusChapa>(filtro.Status, out var status))
                    query = query.Where(c => c.Status == status);
            }

            if (!string.IsNullOrEmpty(filtro.TextoBusca))
            {
                query = query.Where(c => 
                    c.Nome.Contains(filtro.TextoBusca) ||
                    c.NumeroChapa.Contains(filtro.TextoBusca) ||
                    c.Slogan.Contains(filtro.TextoBusca));
            }

            // Paginação
            var totalItens = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalItens / (double)filtro.ItensPorPagina);

            var chapas = await query
                .OrderBy(c => c.NumeroChapa)
                .Skip((filtro.Pagina - 1) * filtro.ItensPorPagina)
                .Take(filtro.ItensPorPagina)
                .ToListAsync();

            return new ListaChapasPaginadaDTO
            {
                Chapas = chapas.Select(c => MapearParaDTO(c)).ToList(),
                PaginaAtual = filtro.Pagina,
                TotalPaginas = totalPaginas,
                TotalItens = totalItens,
                ItensPorPagina = filtro.ItensPorPagina
            };
        }

        #endregion

        #region Métodos Auxiliares

        private async Task AtualizarDiversidadeChapaAsync(ChapaEleicao chapa)
        {
            // Buscar informações de diversidade dos membros confirmados
            var membrosIds = chapa.Membros
                .Where(m => m.Status == StatusMembroChapa.Confirmado)
                .Select(m => m.ProfissionalId)
                .ToList();

            var profissionais = await _context.Profissionais
                .Where(p => membrosIds.Contains(p.Id))
                .ToListAsync();

            // Atualizar contadores
            chapa.QuantidadeHomens = profissionais.Count(p => p.Genero == "M");
            chapa.QuantidadeMulheres = profissionais.Count(p => p.Genero == "F");
            chapa.QuantidadeNegros = profissionais.Count(p => p.Etnia == "Negro" || p.Etnia == "Pardo");
            chapa.QuantidadePcD = profissionais.Count(p => p.IsPcD);
            chapa.QuantidadeLGBTQI = profissionais.Count(p => p.IsLGBTQI);
        }

        private async Task RegistrarHistoricoChapaAsync(int chapaId, string descricao, int usuarioId)
        {
            var historico = new HistoricoChapaEleicao
            {
                ChapaEleicaoId = chapaId,
                Descricao = descricao,
                DataAlteracao = DateTime.Now,
                UsuarioId = usuarioId
            };

            _context.HistoricosChapasEleicao.Add(historico);
            await _context.SaveChangesAsync();
        }

        private async Task NotificarRegistroChapaAsync(ChapaEleicao chapa)
        {
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.ChapaCriada,
                Titulo = "Chapa Registrada com Sucesso",
                Mensagem = $"Sua chapa {chapa.NumeroChapa} foi registrada com sucesso. Complete as informações necessárias.",
                ChapaId = chapa.Id,
                DestinatariosIds = new List<int> { chapa.ResponsavelId }
            });
        }

        private async Task NotificarConfirmacaoMembroAsync(MembroChapa membro)
        {
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.MembroAdicionadoChapa,
                Titulo = "Membro Confirmou Participação",
                Mensagem = $"O membro {membro.ProfissionalId} confirmou participação na chapa.",
                ChapaId = membro.ChapaId,
                DestinatariosIds = new List<int> { membro.Chapa.ResponsavelId }
            });
        }

        private async Task EnviarConviteMembroAsync(MembroChapa membro)
        {
            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == membro.ProfissionalId);

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = new List<string> { profissional.Email },
                Assunto = "Convite para Participar de Chapa Eleitoral",
                TemplateId = "ConviteMembro",
                ParametrosTemplate = new Dictionary<string, string>
                {
                    ["NomeProfissional"] = profissional.Nome,
                    ["NumeroChapa"] = membro.Chapa.NumeroChapa,
                    ["NomeChapa"] = membro.Chapa.Nome,
                    ["Cargo"] = membro.Cargo,
                    ["LinkConfirmacao"] = $"/chapas/{membro.ChapaId}/confirmar-participacao/{membro.Id}"
                }
            });
        }

        [BackgroundJob]
        public async Task EnviarEmailChapaConfirmadaAsync(int chapaId)
        {
            var chapa = await _context.ChapasEleicao
                .Include(c => c.Membros)
                    .ThenInclude(m => m.Profissional)
                .Include(c => c.Calendario)
                    .ThenInclude(cal => cal.Eleicao)
                .FirstOrDefaultAsync(c => c.Id == chapaId);

            if (chapa == null)
                return;

            // Enviar email para todos os membros confirmados
            foreach (var membro in chapa.Membros.Where(m => m.Status == StatusMembroChapa.Confirmado))
            {
                await _notificationService.EnviarEmailAsync(new EmailModel
                {
                    Para = new List<string> { membro.Profissional.Email },
                    Assunto = "Chapa Confirmada com Sucesso",
                    TemplateId = "ChapaConfirmada",
                    ParametrosTemplate = new Dictionary<string, string>
                    {
                        ["NomeProfissional"] = membro.Profissional.Nome,
                        ["NumeroChapa"] = chapa.NumeroChapa,
                        ["NomeChapa"] = chapa.Nome,
                        ["NomeEleicao"] = chapa.Calendario.Eleicao.Nome,
                        ["Ano"] = chapa.Calendario.Ano.ToString()
                    }
                });
            }
        }

        private ChapaEleicaoDTO MapearParaDTO(ChapaEleicao chapa)
        {
            return new ChapaEleicaoDTO
            {
                Id = chapa.Id,
                NumeroChapa = chapa.NumeroChapa,
                Nome = chapa.Nome,
                Slogan = chapa.Slogan,
                Status = chapa.Status.ToString(),
                CalendarioId = chapa.CalendarioId,
                UfId = chapa.UfId,
                UfNome = chapa.Uf?.Nome,
                ResponsavelId = chapa.ResponsavelId,
                DataRegistro = chapa.DataRegistro,
                DataConfirmacao = chapa.DataConfirmacao,
                QuantidadeMembros = chapa.Membros.Count(m => m.Status == StatusMembroChapa.Confirmado),
                QuantidadeHomens = chapa.QuantidadeHomens,
                QuantidadeMulheres = chapa.QuantidadeMulheres,
                QuantidadeNegros = chapa.QuantidadeNegros,
                QuantidadePcD = chapa.QuantidadePcD,
                QuantidadeLGBTQI = chapa.QuantidadeLGBTQI,
                Membros = chapa.Membros.Select(m => new MembroChapaDTO
                {
                    Id = m.Id,
                    ProfissionalId = m.ProfissionalId,
                    NomeProfissional = m.Profissional?.Nome,
                    TipoParticipacao = m.TipoParticipacao.ToString(),
                    Cargo = m.Cargo,
                    Status = m.Status.ToString(),
                    DataInclusao = m.DataInclusao,
                    DataConfirmacao = m.DataConfirmacao,
                    IsResponsavel = m.IsResponsavel
                }).ToList()
            };
        }

        #endregion
    }
}