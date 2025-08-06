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
    /// Service responsável pela gestão detalhada de membros de chapas
    /// </summary>
    public class MembroChapaService : IMembroChapaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MembroChapaService> _logger;
        private readonly IValidacaoElegibilidadeService _validacaoService;
        private readonly INotificationService _notificationService;
        private readonly ICalendarioService _calendarioService;
        
        public MembroChapaService(
            ApplicationDbContext context,
            ILogger<MembroChapaService> logger,
            IValidacaoElegibilidadeService validacaoService,
            INotificationService notificationService,
            ICalendarioService calendarioService)
        {
            _context = context;
            _logger = logger;
            _validacaoService = validacaoService;
            _notificationService = notificationService;
            _calendarioService = calendarioService;
        }

        #region Consultas de Membros

        /// <summary>
        /// Obtém informações detalhadas de um membro
        /// </summary>
        public async Task<MembroChapaDetalhadoDTO> ObterMembroAsync(int membroId)
        {
            var membro = await _context.MembrosChapa
                .Include(m => m.Chapa)
                    .ThenInclude(c => c.Calendario)
                .Include(m => m.Profissional)
                    .ThenInclude(p => p.Endereco)
                .Include(m => m.Pendencias)
                .FirstOrDefaultAsync(m => m.Id == membroId);

            if (membro == null)
                throw new Exception("Membro não encontrado");

            // Obter validação de elegibilidade
            var elegibilidade = await _validacaoService.ValidarElegibilidadeCompletoAsync(
                membro.ProfissionalId,
                membro.Chapa.Calendario.EleicaoId);

            return MapearParaDetalhadoDTO(membro, elegibilidade);
        }

        /// <summary>
        /// Obtém todos os membros de uma chapa
        /// </summary>
        public async Task<List<MembroChapaDetalhadoDTO>> ObterMembrosPorChapaAsync(int chapaId)
        {
            var membros = await _context.MembrosChapa
                .Include(m => m.Profissional)
                    .ThenInclude(p => p.Endereco)
                .Include(m => m.Pendencias)
                .Include(m => m.Chapa)
                    .ThenInclude(c => c.Calendario)
                .Where(m => m.ChapaId == chapaId)
                .OrderBy(m => m.TipoParticipacao)
                .ThenBy(m => m.Ordem)
                .ToListAsync();

            var membrosDTO = new List<MembroChapaDetalhadoDTO>();
            
            foreach (var membro in membros)
            {
                var elegibilidade = await _validacaoService.ValidarElegibilidadeCompletoAsync(
                    membro.ProfissionalId,
                    membro.Chapa.Calendario.EleicaoId);
                
                membrosDTO.Add(MapearParaDetalhadoDTO(membro, elegibilidade));
            }

            return membrosDTO;
        }

        #endregion

        #region Convites e Confirmações

        /// <summary>
        /// Envia convite para um profissional participar da chapa
        /// </summary>
        public async Task<bool> ConvidarMembroAsync(ConviteMembroDTO dto)
        {
            var chapa = await _context.ChapasEleicao
                .Include(c => c.Calendario)
                .Include(c => c.Membros)
                .FirstOrDefaultAsync(c => c.Id == dto.ChapaId);

            if (chapa == null)
                throw new Exception("Chapa não encontrada");

            // Validar período
            var periodoValido = await _calendarioService.ValidarPeriodoParaAcaoAsync(
                chapa.CalendarioId,
                TipoAtividadeCalendario.RegistroChapa);

            if (!periodoValido)
                throw new Exception("Fora do período para alterações na chapa");

            // Validar se já está na chapa
            var jaEstaNaChapa = chapa.Membros
                .Any(m => m.ProfissionalId == dto.ProfissionalId && 
                         m.Status != StatusMembroChapa.Removido);

            if (jaEstaNaChapa)
                throw new Exception("Profissional já está na chapa");

            // Validar se está em outra chapa
            var estaEmOutraChapa = await _context.MembrosChapa
                .Include(m => m.Chapa)
                .AnyAsync(m => 
                    m.ProfissionalId == dto.ProfissionalId &&
                    m.Chapa.CalendarioId == chapa.CalendarioId &&
                    m.ChapaId != dto.ChapaId &&
                    m.Status == StatusMembroChapa.Confirmado);

            if (estaEmOutraChapa)
                throw new Exception("Profissional já está confirmado em outra chapa");

            // Validar elegibilidade
            var elegibilidade = await _validacaoService.ValidarElegibilidadeAsync(dto.ProfissionalId);
            if (!elegibilidade.IsElegivel)
            {
                throw new Exception($"Profissional não é elegível: {string.Join(", ", elegibilidade.Restricoes)}");
            }

            // Gerar token único para confirmação
            var tokenConfirmacao = Guid.NewGuid().ToString();

            // Criar membro com status de convite pendente
            var novoMembro = new MembroChapa
            {
                ChapaId = dto.ChapaId,
                ProfissionalId = dto.ProfissionalId,
                TipoParticipacao = dto.TipoParticipacao,
                Cargo = dto.Cargo,
                Status = StatusMembroChapa.ConvitePendente,
                DataInclusao = DateTime.Now,
                TokenConfirmacao = tokenConfirmacao,
                DataExpiracaoToken = DateTime.Now.AddDays(7),
                IsResponsavel = false
            };

            _context.MembrosChapa.Add(novoMembro);
            await _context.SaveChangesAsync();

            // Enviar email de convite
            await EnviarEmailConviteAsync(novoMembro, dto.MensagemConvite);

            _logger.LogInformation($"Convite enviado para profissional {dto.ProfissionalId} para chapa {dto.ChapaId}");

            return true;
        }

        /// <summary>
        /// Confirma convite para participar da chapa
        /// </summary>
        public async Task<bool> ConfirmarConviteAsync(int membroId, int profissionalId, string tokenConfirmacao)
        {
            var membro = await _context.MembrosChapa
                .Include(m => m.Chapa)
                .Include(m => m.Profissional)
                .FirstOrDefaultAsync(m => 
                    m.Id == membroId && 
                    m.ProfissionalId == profissionalId &&
                    m.TokenConfirmacao == tokenConfirmacao);

            if (membro == null)
                throw new Exception("Convite não encontrado ou inválido");

            if (membro.Status != StatusMembroChapa.ConvitePendente)
                throw new Exception("Convite já foi processado");

            if (membro.DataExpiracaoToken < DateTime.Now)
                throw new Exception("Token de confirmação expirado");

            // Confirmar participação
            membro.Status = StatusMembroChapa.Confirmado;
            membro.DataConfirmacao = DateTime.Now;
            membro.TokenConfirmacao = null;
            membro.DataExpiracaoToken = null;

            // Criar pendências básicas
            await CriarPendenciasIniciaisAsync(membro);

            await _context.SaveChangesAsync();

            // Notificar responsável da chapa
            await NotificarConfirmacaoMembroAsync(membro);

            _logger.LogInformation($"Membro {profissionalId} confirmou participação na chapa {membro.ChapaId}");

            return true;
        }

        /// <summary>
        /// Recusa convite para participar da chapa
        /// </summary>
        public async Task<bool> RecusarConviteAsync(int membroId, int profissionalId, string motivo)
        {
            var membro = await _context.MembrosChapa
                .Include(m => m.Chapa)
                .FirstOrDefaultAsync(m => 
                    m.Id == membroId && 
                    m.ProfissionalId == profissionalId);

            if (membro == null)
                throw new Exception("Convite não encontrado");

            if (membro.Status != StatusMembroChapa.ConvitePendente)
                throw new Exception("Convite já foi processado");

            // Recusar convite
            membro.Status = StatusMembroChapa.Recusado;
            membro.DataRecusa = DateTime.Now;
            membro.MotivoRecusa = motivo;

            await _context.SaveChangesAsync();

            // Notificar responsável da chapa
            await NotificarRecusaMembroAsync(membro, motivo);

            _logger.LogInformation($"Membro {profissionalId} recusou participação na chapa {membro.ChapaId}");

            return true;
        }

        #endregion

        #region Gestão de Membros

        /// <summary>
        /// Substitui um membro da chapa por outro
        /// </summary>
        public async Task<bool> SubstituirMembroAsync(SubstituirMembroDTO dto)
        {
            var membroAtual = await _context.MembrosChapa
                .Include(m => m.Chapa)
                    .ThenInclude(c => c.Calendario)
                .Include(m => m.Profissional)
                .FirstOrDefaultAsync(m => m.Id == dto.MembroAtualId);

            if (membroAtual == null)
                throw new Exception("Membro atual não encontrado");

            if (membroAtual.Status != StatusMembroChapa.Confirmado)
                throw new Exception("Apenas membros confirmados podem ser substituídos");

            // Validar período
            var periodoValido = await _calendarioService.ValidarPeriodoParaAcaoAsync(
                membroAtual.Chapa.CalendarioId,
                TipoAtividadeCalendario.Substituicao);

            if (!periodoValido)
                throw new Exception("Fora do período para substituições");

            // Validar novo membro
            var validacao = await ValidarMembroAsync(dto.NovoProfissionalId, membroAtual.ChapaId);
            if (!validacao.IsValido)
            {
                throw new Exception(validacao.MensagemValidacao);
            }

            // Marcar membro atual como substituído
            membroAtual.Status = StatusMembroChapa.Substituido;
            membroAtual.DataSaida = DateTime.Now;
            membroAtual.MotivoSaida = dto.MotivoSubstituicao;

            // Criar novo membro
            var novoMembro = new MembroChapa
            {
                ChapaId = membroAtual.ChapaId,
                ProfissionalId = dto.NovoProfissionalId,
                TipoParticipacao = dto.ManterTipoParticipacao ? 
                    membroAtual.TipoParticipacao : dto.NovoTipoParticipacao,
                Cargo = dto.ManterCargo ? membroAtual.Cargo : dto.NovoCargo,
                Status = StatusMembroChapa.Confirmado,
                DataInclusao = DateTime.Now,
                DataConfirmacao = DateTime.Now,
                IsResponsavel = false,
                SubstitutoDeId = membroAtual.Id
            };

            _context.MembrosChapa.Add(novoMembro);

            // Registrar substituição
            var substituicao = new SubstituicaoMembro
            {
                MembroAnteriorId = membroAtual.Id,
                MembroNovoId = novoMembro.Id,
                MotivoSubstituicao = dto.MotivoSubstituicao,
                DataSubstituicao = DateTime.Now,
                UsuarioSubstituicaoId = dto.UsuarioSubstituicaoId
            };

            _context.SubstituicoesMembros.Add(substituicao);

            await _context.SaveChangesAsync();

            // Notificar partes interessadas
            await NotificarSubstituicaoMembroAsync(membroAtual, novoMembro);

            _logger.LogInformation($"Membro {membroAtual.ProfissionalId} substituído por {dto.NovoProfissionalId} na chapa {membroAtual.ChapaId}");

            return true;
        }

        /// <summary>
        /// Altera o cargo de um membro
        /// </summary>
        public async Task<bool> AlterarCargoMembroAsync(int membroId, string novoCargo, int usuarioId)
        {
            var membro = await _context.MembrosChapa
                .Include(m => m.Chapa)
                .FirstOrDefaultAsync(m => m.Id == membroId);

            if (membro == null)
                throw new Exception("Membro não encontrado");

            if (membro.Status != StatusMembroChapa.Confirmado)
                throw new Exception("Apenas membros confirmados podem ter cargo alterado");

            var cargoAnterior = membro.Cargo;
            membro.Cargo = novoCargo;
            membro.DataUltimaAlteracao = DateTime.Now;

            // Registrar histórico
            var historico = new HistoricoMembroChapa
            {
                MembroChapaId = membroId,
                TipoAlteracao = "Alteração de Cargo",
                ValorAnterior = cargoAnterior,
                ValorNovo = novoCargo,
                DataAlteracao = DateTime.Now,
                UsuarioAlteracaoId = usuarioId
            };

            _context.HistoricosMembrosChapa.Add(historico);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Cargo do membro {membroId} alterado de '{cargoAnterior}' para '{novoCargo}'");

            return true;
        }

        /// <summary>
        /// Altera o tipo de participação de um membro
        /// </summary>
        public async Task<bool> AlterarTipoParticipacaoAsync(int membroId, TipoParticipacaoMembro novoTipo, int usuarioId)
        {
            var membro = await _context.MembrosChapa
                .Include(m => m.Chapa)
                    .ThenInclude(c => c.Calendario)
                .FirstOrDefaultAsync(m => m.Id == membroId);

            if (membro == null)
                throw new Exception("Membro não encontrado");

            if (membro.Status != StatusMembroChapa.Confirmado)
                throw new Exception("Apenas membros confirmados podem ter tipo alterado");

            // Validar quantidade de titulares e suplentes
            var parametros = await _context.ParametrosConselheiro
                .FirstOrDefaultAsync(p => p.EleicaoId == membro.Chapa.Calendario.EleicaoId);

            if (parametros != null)
            {
                var membrosChapa = await _context.MembrosChapa
                    .Where(m => m.ChapaId == membro.ChapaId && m.Status == StatusMembroChapa.Confirmado)
                    .ToListAsync();

                if (novoTipo == TipoParticipacaoMembro.Titular)
                {
                    var titularesAtuais = membrosChapa.Count(m => m.TipoParticipacao == TipoParticipacaoMembro.Titular && m.Id != membroId);
                    if (titularesAtuais >= parametros.QuantidadeTitulares)
                    {
                        throw new Exception($"Número máximo de titulares ({parametros.QuantidadeTitulares}) já atingido");
                    }
                }
                else
                {
                    var suplentesAtuais = membrosChapa.Count(m => m.TipoParticipacao == TipoParticipacaoMembro.Suplente && m.Id != membroId);
                    if (suplentesAtuais >= parametros.QuantidadeSuplentes)
                    {
                        throw new Exception($"Número máximo de suplentes ({parametros.QuantidadeSuplentes}) já atingido");
                    }
                }
            }

            var tipoAnterior = membro.TipoParticipacao;
            membro.TipoParticipacao = novoTipo;
            membro.DataUltimaAlteracao = DateTime.Now;

            // Registrar histórico
            var historico = new HistoricoMembroChapa
            {
                MembroChapaId = membroId,
                TipoAlteracao = "Alteração de Tipo de Participação",
                ValorAnterior = tipoAnterior.ToString(),
                ValorNovo = novoTipo.ToString(),
                DataAlteracao = DateTime.Now,
                UsuarioAlteracaoId = usuarioId
            };

            _context.HistoricosMembrosChapa.Add(historico);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Tipo de participação do membro {membroId} alterado de '{tipoAnterior}' para '{novoTipo}'");

            return true;
        }

        #endregion

        #region Validações

        /// <summary>
        /// Valida se um profissional pode ser membro de uma chapa
        /// </summary>
        public async Task<ValidacaoMembroResult> ValidarMembroAsync(int profissionalId, int chapaId)
        {
            var resultado = new ValidacaoMembroResult
            {
                IsValido = true,
                Restricoes = new List<string>()
            };

            // Verificar se o profissional existe
            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == profissionalId);

            if (profissional == null)
            {
                resultado.IsValido = false;
                resultado.Restricoes.Add("Profissional não encontrado");
                resultado.MensagemValidacao = "Profissional não encontrado no sistema";
                return resultado;
            }

            // Verificar elegibilidade
            var elegibilidade = await _validacaoService.ValidarElegibilidadeAsync(profissionalId);
            resultado.IsElegivel = elegibilidade.IsElegivel;
            
            if (!elegibilidade.IsElegivel)
            {
                resultado.IsValido = false;
                resultado.Restricoes.AddRange(elegibilidade.Restricoes);
            }

            // Verificar se já está na chapa
            var jaEstaNaChapa = await _context.MembrosChapa
                .AnyAsync(m => 
                    m.ChapaId == chapaId && 
                    m.ProfissionalId == profissionalId &&
                    m.Status != StatusMembroChapa.Removido &&
                    m.Status != StatusMembroChapa.Substituido);

            if (jaEstaNaChapa)
            {
                resultado.IsValido = false;
                resultado.JaEstaNaChapa = true;
                resultado.Restricoes.Add("Profissional já está na chapa");
            }

            // Verificar se está em outra chapa do mesmo calendário
            var chapa = await _context.ChapasEleicao
                .FirstOrDefaultAsync(c => c.Id == chapaId);

            if (chapa != null)
            {
                var estaEmOutraChapa = await _context.MembrosChapa
                    .Include(m => m.Chapa)
                    .AnyAsync(m => 
                        m.ProfissionalId == profissionalId &&
                        m.Chapa.CalendarioId == chapa.CalendarioId &&
                        m.ChapaId != chapaId &&
                        m.Status == StatusMembroChapa.Confirmado);

                if (estaEmOutraChapa)
                {
                    resultado.IsValido = false;
                    resultado.JaEstaEmOutraChapa = true;
                    resultado.Restricoes.Add("Profissional já está confirmado em outra chapa desta eleição");
                }
            }

            // Consolidar mensagem
            if (!resultado.IsValido)
            {
                resultado.MensagemValidacao = string.Join("; ", resultado.Restricoes);
            }
            else
            {
                resultado.MensagemValidacao = "Profissional apto para participar da chapa";
            }

            return resultado;
        }

        #endregion

        #region Pendências

        /// <summary>
        /// Verifica se um membro tem pendências
        /// </summary>
        public async Task<bool> VerificarPendenciasMembroAsync(int membroId)
        {
            var pendencias = await _context.PendenciasMembrosChapa
                .Where(p => p.MembroChapaId == membroId && !p.Resolvida)
                .AnyAsync();

            return pendencias;
        }

        /// <summary>
        /// Obtém pendências de um membro
        /// </summary>
        public async Task<List<PendenciaMembroDTO>> ObterPendenciasMembroAsync(int membroId)
        {
            var pendencias = await _context.PendenciasMembrosChapa
                .Where(p => p.MembroChapaId == membroId)
                .OrderBy(p => p.Resolvida)
                .ThenByDescending(p => p.Impeditiva)
                .ThenBy(p => p.DataCriacao)
                .ToListAsync();

            return pendencias.Select(p => new PendenciaMembroDTO
            {
                Id = p.Id,
                Tipo = p.Tipo,
                Descricao = p.Descricao,
                DataCriacao = p.DataCriacao,
                DataResolucao = p.DataResolucao,
                Resolvida = p.Resolvida,
                Impeditiva = p.Impeditiva,
                Observacao = p.Observacao
            }).ToList();
        }

        /// <summary>
        /// Cria pendências iniciais para um novo membro
        /// </summary>
        private async Task CriarPendenciasIniciaisAsync(MembroChapa membro)
        {
            var pendencias = new List<PendenciaMembroChapa>
            {
                new PendenciaMembroChapa
                {
                    MembroChapaId = membro.Id,
                    Tipo = TipoPendenciaMembro.FotoPerfil,
                    Descricao = "Adicionar foto de perfil",
                    DataCriacao = DateTime.Now,
                    Impeditiva = false,
                    Resolvida = false
                },
                new PendenciaMembroChapa
                {
                    MembroChapaId = membro.Id,
                    Tipo = TipoPendenciaMembro.CurriculoResumido,
                    Descricao = "Adicionar currículo resumido",
                    DataCriacao = DateTime.Now,
                    Impeditiva = false,
                    Resolvida = false
                },
                new PendenciaMembroChapa
                {
                    MembroChapaId = membro.Id,
                    Tipo = TipoPendenciaMembro.DeclaracaoElegibilidade,
                    Descricao = "Assinar declaração de elegibilidade",
                    DataCriacao = DateTime.Now,
                    Impeditiva = true,
                    Resolvida = false
                },
                new PendenciaMembroChapa
                {
                    MembroChapaId = membro.Id,
                    Tipo = TipoPendenciaMembro.AceiteTermos,
                    Descricao = "Aceitar termos e condições",
                    DataCriacao = DateTime.Now,
                    Impeditiva = true,
                    Resolvida = false
                }
            };

            _context.PendenciasMembrosChapa.AddRange(pendencias);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Métodos Auxiliares

        private async Task EnviarEmailConviteAsync(MembroChapa membro, string mensagemPersonalizada)
        {
            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == membro.ProfissionalId);

            var chapa = await _context.ChapasEleicao
                .Include(c => c.Calendario)
                    .ThenInclude(cal => cal.Eleicao)
                .FirstOrDefaultAsync(c => c.Id == membro.ChapaId);

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = new List<string> { profissional.Email },
                Assunto = "Convite para Participar de Chapa Eleitoral",
                TemplateId = "ConviteMembro",
                ParametrosTemplate = new Dictionary<string, string>
                {
                    ["NomeProfissional"] = profissional.Nome,
                    ["NumeroChapa"] = chapa.NumeroChapa,
                    ["NomeChapa"] = chapa.Nome,
                    ["Cargo"] = membro.Cargo,
                    ["TipoParticipacao"] = membro.TipoParticipacao.ToString(),
                    ["MensagemPersonalizada"] = mensagemPersonalizada,
                    ["LinkConfirmacao"] = $"/chapas/convite/confirmar/{membro.Id}?token={membro.TokenConfirmacao}",
                    ["DataExpiracao"] = membro.DataExpiracaoToken?.ToString("dd/MM/yyyy"),
                    ["NomeEleicao"] = chapa.Calendario.Eleicao?.Nome,
                    ["AnoEleicao"] = chapa.Calendario.Ano.ToString()
                }
            });
        }

        private async Task NotificarConfirmacaoMembroAsync(MembroChapa membro)
        {
            var chapa = await _context.ChapasEleicao
                .FirstOrDefaultAsync(c => c.Id == membro.ChapaId);

            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.MembroConfirmouParticipacao,
                Titulo = "Membro Confirmou Participação",
                Mensagem = $"{membro.Profissional?.Nome} confirmou participação na chapa",
                ChapaId = membro.ChapaId,
                DestinatariosIds = new List<int> { chapa.ResponsavelId }
            });
        }

        private async Task NotificarRecusaMembroAsync(MembroChapa membro, string motivo)
        {
            var chapa = await _context.ChapasEleicao
                .FirstOrDefaultAsync(c => c.Id == membro.ChapaId);

            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.MembroRecusouParticipacao,
                Titulo = "Membro Recusou Participação",
                Mensagem = $"{membro.Profissional?.Nome} recusou participação na chapa. Motivo: {motivo}",
                ChapaId = membro.ChapaId,
                DestinatariosIds = new List<int> { chapa.ResponsavelId }
            });
        }

        private async Task NotificarSubstituicaoMembroAsync(MembroChapa membroAnterior, MembroChapa membroNovo)
        {
            var chapa = await _context.ChapasEleicao
                .Include(c => c.Membros)
                    .ThenInclude(m => m.Profissional)
                .FirstOrDefaultAsync(c => c.Id == membroAnterior.ChapaId);

            var profissionalAnterior = membroAnterior.Profissional;
            var profissionalNovo = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == membroNovo.ProfissionalId);

            // Notificar todos os membros da chapa
            var destinatarios = chapa.Membros
                .Where(m => m.Status == StatusMembroChapa.Confirmado)
                .Select(m => m.Profissional.Email)
                .ToList();

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = destinatarios,
                Assunto = "Substituição de Membro na Chapa",
                TemplateId = "SubstituicaoMembro",
                ParametrosTemplate = new Dictionary<string, string>
                {
                    ["NumeroChapa"] = chapa.NumeroChapa,
                    ["NomeChapa"] = chapa.Nome,
                    ["MembroAnterior"] = profissionalAnterior?.Nome,
                    ["MembroNovo"] = profissionalNovo?.Nome,
                    ["Cargo"] = membroNovo.Cargo,
                    ["TipoParticipacao"] = membroNovo.TipoParticipacao.ToString()
                }
            });
        }

        private MembroChapaDetalhadoDTO MapearParaDetalhadoDTO(MembroChapa membro, ValidacaoElegibilidadeResult elegibilidade)
        {
            return new MembroChapaDetalhadoDTO
            {
                Id = membro.Id,
                ChapaId = membro.ChapaId,
                ProfissionalId = membro.ProfissionalId,
                Profissional = new ProfissionalResumoDTO
                {
                    Id = membro.Profissional.Id,
                    Nome = membro.Profissional.Nome,
                    Cpf = membro.Profissional.Cpf,
                    RegistroProfissional = membro.Profissional.RegistroProfissional,
                    Email = membro.Profissional.Email,
                    Telefone = membro.Profissional.Telefone,
                    UfRegistro = membro.Profissional.UfRegistro,
                    FotoUrl = membro.Profissional.FotoUrl,
                    Genero = membro.Profissional.Genero,
                    Etnia = membro.Profissional.Etnia,
                    IsPcD = membro.Profissional.IsPcD,
                    IsLGBTQI = membro.Profissional.IsLGBTQI,
                    DataRegistro = membro.Profissional.DataRegistro
                },
                TipoParticipacao = membro.TipoParticipacao,
                Cargo = membro.Cargo,
                Status = membro.Status,
                DataInclusao = membro.DataInclusao,
                DataConfirmacao = membro.DataConfirmacao,
                DataSaida = membro.DataSaida,
                MotivoSaida = membro.MotivoSaida,
                IsResponsavel = membro.IsResponsavel,
                CurriculoResumido = membro.CurriculoResumido,
                PropostasIndividuais = membro.PropostasIndividuais,
                Pendencias = membro.Pendencias?.Select(p => new PendenciaMembroDTO
                {
                    Id = p.Id,
                    Tipo = p.Tipo,
                    Descricao = p.Descricao,
                    DataCriacao = p.DataCriacao,
                    DataResolucao = p.DataResolucao,
                    Resolvida = p.Resolvida,
                    Impeditiva = p.Impeditiva,
                    Observacao = p.Observacao
                }).ToList(),
                Elegibilidade = elegibilidade
            };
        }

        #endregion
    }
}