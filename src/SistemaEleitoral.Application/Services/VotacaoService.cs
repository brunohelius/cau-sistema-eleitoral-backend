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
using System.Security.Cryptography;
using System.Text;
using Hangfire;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Service responsável pelo sistema de votação eletrônica
    /// Gerencia todo o processo de votação, validações e segurança
    /// </summary>
    public class VotacaoService : IVotacaoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VotacaoService> _logger;
        private readonly ICalendarioService _calendarioService;
        private readonly INotificationService _notificationService;
        private readonly IValidacaoElegibilidadeService _validacaoService;
        
        public VotacaoService(
            ApplicationDbContext context,
            ILogger<VotacaoService> logger,
            ICalendarioService calendarioService,
            INotificationService notificationService,
            IValidacaoElegibilidadeService validacaoService)
        {
            _context = context;
            _logger = logger;
            _calendarioService = calendarioService;
            _notificationService = notificationService;
            _validacaoService = validacaoService;
        }

        #region Abertura e Fechamento de Votação

        /// <summary>
        /// Abre o período de votação
        /// </summary>
        public async Task<bool> AbrirVotacaoAsync(AbrirVotacaoDTO dto)
        {
            var calendario = await _context.Calendarios
                .Include(c => c.Eleicao)
                .FirstOrDefaultAsync(c => c.Id == dto.CalendarioId);

            if (calendario == null)
                throw new Exception("Calendário não encontrado");

            // Verificar se já existe votação aberta
            var votacaoExistente = await _context.SessoesVotacao
                .AnyAsync(s => 
                    s.CalendarioId == dto.CalendarioId &&
                    s.UfId == dto.UfId &&
                    s.Status == StatusSessaoVotacao.Aberta);

            if (votacaoExistente)
                throw new Exception("Já existe votação aberta para esta UF");

            // Validar período de votação
            var periodoValido = await _calendarioService.ValidarPeriodoUFAsync(
                dto.CalendarioId,
                dto.UfSigla,
                TipoAtividadeCalendario.Votacao);

            if (!periodoValido)
                throw new Exception("Fora do período de votação");

            // Criar sessão de votação
            var sessao = new SessaoVotacao
            {
                CalendarioId = dto.CalendarioId,
                UfId = dto.UfId,
                Turno = dto.Turno,
                DataAbertura = DateTime.Now,
                Status = StatusSessaoVotacao.Aberta,
                UsuarioAberturaId = dto.UsuarioAberturaId,
                
                // Gerar hash de segurança
                HashAbertura = GerarHashSeguranca($"{dto.CalendarioId}-{dto.UfId}-{DateTime.Now.Ticks}")
            };

            _context.SessoesVotacao.Add(sessao);

            // Obter chapas aptas para votação
            var chapasAptas = await _context.ChapasEleicao
                .Where(c => 
                    c.CalendarioId == dto.CalendarioId &&
                    c.UfId == dto.UfId &&
                    (c.Status == StatusChapa.Deferida || c.Status == StatusChapa.Apta))
                .ToListAsync();

            // Criar urnas eletrônicas para cada chapa
            foreach (var chapa in chapasAptas)
            {
                var urna = new UrnaEletronica
                {
                    SessaoVotacaoId = sessao.Id,
                    ChapaId = chapa.Id,
                    TotalVotos = 0,
                    VotosNulos = 0,
                    VotosBrancos = 0,
                    Status = StatusUrna.Ativa
                };

                _context.UrnasEletronicas.Add(urna);
            }

            await _context.SaveChangesAsync();

            // Notificar eleitores
            await NotificarAberturaVotacaoAsync(sessao, dto.UfSigla);

            _logger.LogInformation($"Votação aberta para calendário {dto.CalendarioId}, UF {dto.UfSigla}");

            return true;
        }

        /// <summary>
        /// Fecha o período de votação
        /// </summary>
        public async Task<bool> FecharVotacaoAsync(FecharVotacaoDTO dto)
        {
            var sessao = await _context.SessoesVotacao
                .Include(s => s.UrnasEletronicas)
                .FirstOrDefaultAsync(s => 
                    s.CalendarioId == dto.CalendarioId &&
                    s.UfId == dto.UfId &&
                    s.Status == StatusSessaoVotacao.Aberta);

            if (sessao == null)
                throw new Exception("Não há votação aberta para esta UF");

            // Fechar sessão
            sessao.Status = StatusSessaoVotacao.Fechada;
            sessao.DataFechamento = DateTime.Now;
            sessao.UsuarioFechamentoId = dto.UsuarioFechamentoId;
            sessao.HashFechamento = GerarHashSeguranca($"{sessao.Id}-{DateTime.Now.Ticks}-FECHADO");

            // Fechar urnas
            foreach (var urna in sessao.UrnasEletronicas)
            {
                urna.Status = StatusUrna.Fechada;
                urna.DataFechamento = DateTime.Now;
            }

            // Registrar estatísticas de votação
            var estatisticas = new EstatisticaVotacao
            {
                SessaoVotacaoId = sessao.Id,
                TotalEleitores = await ObterTotalEleitoresAsync(dto.UfId),
                TotalVotantes = await _context.Votos
                    .Where(v => v.SessaoVotacaoId == sessao.Id)
                    .Select(v => v.EleitorId)
                    .Distinct()
                    .CountAsync(),
                PercentualParticipacao = 0, // Será calculado
                DataGeracao = DateTime.Now
            };

            if (estatisticas.TotalEleitores > 0)
            {
                estatisticas.PercentualParticipacao = 
                    (estatisticas.TotalVotantes * 100.0) / estatisticas.TotalEleitores;
            }

            _context.EstatisticasVotacao.Add(estatisticas);

            await _context.SaveChangesAsync();

            // Notificar fechamento
            await NotificarFechamentoVotacaoAsync(sessao);

            // Agendar apuração automática
            if (dto.IniciarApuracao)
            {
                BackgroundJob.Enqueue(() => IniciarApuracaoAsync(sessao.Id));
            }

            _logger.LogInformation($"Votação fechada para calendário {dto.CalendarioId}, UF {dto.UfId}");

            return true;
        }

        #endregion

        #region Registro de Votos

        /// <summary>
        /// Registra um voto
        /// </summary>
        public async Task<ComprovanteVotoDTO> RegistrarVotoAsync(RegistrarVotoDTO dto)
        {
            // Validar sessão de votação
            var sessao = await _context.SessoesVotacao
                .Include(s => s.UrnasEletronicas)
                .FirstOrDefaultAsync(s => 
                    s.Id == dto.SessaoVotacaoId &&
                    s.Status == StatusSessaoVotacao.Aberta);

            if (sessao == null)
                throw new Exception("Sessão de votação inválida ou fechada");

            // Verificar se eleitor já votou
            var jaVotou = await _context.Votos
                .AnyAsync(v => 
                    v.SessaoVotacaoId == dto.SessaoVotacaoId &&
                    v.EleitorId == dto.EleitorId);

            if (jaVotou)
                throw new Exception("Eleitor já votou nesta eleição");

            // Verificar elegibilidade do eleitor
            var podeVotar = await ValidarEleitorAsync(dto.EleitorId, sessao.UfId);
            if (!podeVotar)
                throw new Exception("Eleitor não está apto para votar");

            // Gerar hash único do voto (para auditoria)
            var hashVoto = GerarHashVoto(dto);
            var protocoloVoto = GerarProtocoloVoto();

            // Registrar voto
            var voto = new Voto
            {
                SessaoVotacaoId = dto.SessaoVotacaoId,
                EleitorId = dto.EleitorId,
                DataHoraVoto = DateTime.Now,
                HashVoto = hashVoto,
                ProtocoloComprovante = protocoloVoto,
                IpOrigem = dto.IpOrigem,
                
                // Voto é secreto - não armazenamos a escolha diretamente
                VotoCriptografado = CriptografarVoto(dto.ChapaId, dto.TipoVoto)
            };

            _context.Votos.Add(voto);

            // Atualizar contadores na urna (de forma anônima)
            UrnaEletronica urna = null;
            
            if (dto.TipoVoto == TipoVoto.Nominal && dto.ChapaId.HasValue)
            {
                urna = sessao.UrnasEletronicas
                    .FirstOrDefault(u => u.ChapaId == dto.ChapaId.Value);
                
                if (urna != null)
                {
                    urna.TotalVotos++;
                }
            }
            else if (dto.TipoVoto == TipoVoto.Branco)
            {
                // Incrementar brancos em todas as urnas proporcionalmente
                foreach (var u in sessao.UrnasEletronicas)
                {
                    u.VotosBrancos++;
                }
            }
            else if (dto.TipoVoto == TipoVoto.Nulo)
            {
                // Incrementar nulos em todas as urnas proporcionalmente
                foreach (var u in sessao.UrnasEletronicas)
                {
                    u.VotosNulos++;
                }
            }

            // Registrar log de auditoria
            var logAuditoria = new LogAuditoriaVoto
            {
                VotoId = voto.Id,
                Acao = "VOTO_REGISTRADO",
                DataHora = DateTime.Now,
                HashOperacao = GerarHashSeguranca($"{voto.Id}-{DateTime.Now.Ticks}")
            };

            _context.LogsAuditoriaVoto.Add(logAuditoria);

            await _context.SaveChangesAsync();

            // Gerar comprovante
            var comprovante = new ComprovanteVotoDTO
            {
                ProtocoloComprovante = protocoloVoto,
                DataHoraVoto = voto.DataHoraVoto,
                HashComprovante = hashVoto,
                CodigoVerificacao = GerarCodigoVerificacao(protocoloVoto),
                MensagemConfirmacao = "Seu voto foi registrado com sucesso"
            };

            // Enviar comprovante por email
            BackgroundJob.Enqueue(() => EnviarComprovanteVotoAsync(dto.EleitorId, comprovante));

            _logger.LogInformation($"Voto registrado com protocolo {protocoloVoto}");

            return comprovante;
        }

        /// <summary>
        /// Valida se um eleitor pode votar
        /// </summary>
        private async Task<bool> ValidarEleitorAsync(int eleitorId, int ufId)
        {
            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == eleitorId);

            if (profissional == null)
                return false;

            // Verificar registro ativo
            if (profissional.StatusRegistro != StatusRegistroProfissional.Ativo)
                return false;

            // Verificar UF de registro
            var ufProfissional = await _context.Ufs
                .FirstOrDefaultAsync(u => u.Sigla == profissional.UfRegistro);

            if (ufProfissional == null || ufProfissional.Id != ufId)
                return false;

            // Verificar adimplência
            var adimplente = await _validacaoService.VerificarAdimplenciaAsync(eleitorId);
            if (!adimplente)
                return false;

            return true;
        }

        #endregion

        #region Apuração

        /// <summary>
        /// Inicia processo de apuração
        /// </summary>
        [BackgroundJob]
        public async Task<ResultadoApuracaoDTO> IniciarApuracaoAsync(int sessaoVotacaoId)
        {
            var sessao = await _context.SessoesVotacao
                .Include(s => s.UrnasEletronicas)
                    .ThenInclude(u => u.Chapa)
                .Include(s => s.Calendario)
                .FirstOrDefaultAsync(s => s.Id == sessaoVotacaoId);

            if (sessao == null)
                throw new Exception("Sessão de votação não encontrada");

            if (sessao.Status != StatusSessaoVotacao.Fechada)
                throw new Exception("Votação deve estar fechada para iniciar apuração");

            // Marcar como em apuração
            sessao.Status = StatusSessaoVotacao.EmApuracao;
            await _context.SaveChangesAsync();

            // Criar resultado de apuração
            var resultado = new ResultadoApuracao
            {
                SessaoVotacaoId = sessaoVotacaoId,
                DataInicioApuracao = DateTime.Now,
                Status = StatusApuracao.EmAndamento
            };

            _context.ResultadosApuracao.Add(resultado);

            // Processar votos e calcular resultados
            var totalVotosValidos = 0;
            var totalVotosBrancos = 0;
            var totalVotosNulos = 0;

            var resultadosChapas = new List<ResultadoChapaApuracao>();

            foreach (var urna in sessao.UrnasEletronicas)
            {
                totalVotosValidos += urna.TotalVotos;
                totalVotosBrancos += urna.VotosBrancos;
                totalVotosNulos += urna.VotosNulos;

                var resultadoChapa = new ResultadoChapaApuracao
                {
                    ResultadoApuracaoId = resultado.Id,
                    ChapaId = urna.ChapaId.Value,
                    TotalVotos = urna.TotalVotos,
                    PercentualVotos = 0 // Será calculado
                };

                resultadosChapas.Add(resultadoChapa);
                _context.ResultadosChapasApuracao.Add(resultadoChapa);
            }

            var totalGeral = totalVotosValidos + totalVotosBrancos + totalVotosNulos;

            // Calcular percentuais
            foreach (var rc in resultadosChapas)
            {
                if (totalVotosValidos > 0)
                {
                    rc.PercentualVotos = (rc.TotalVotos * 100.0) / totalVotosValidos;
                }
            }

            // Determinar vencedor
            var chapaVencedora = resultadosChapas
                .OrderByDescending(rc => rc.TotalVotos)
                .FirstOrDefault();

            // Verificar se precisa de segundo turno
            var precisaSegundoTurno = false;
            if (chapaVencedora != null && totalVotosValidos > 0)
            {
                var percentualVencedor = (chapaVencedora.TotalVotos * 100.0) / totalVotosValidos;
                
                // Se não atingiu 50% + 1, precisa de segundo turno
                if (percentualVencedor <= 50)
                {
                    precisaSegundoTurno = true;
                    resultado.PrecisaSegundoTurno = true;
                }
                else
                {
                    resultado.ChapaVencedoraId = chapaVencedora.ChapaId;
                    
                    // Marcar chapa como eleita
                    var chapa = await _context.ChapasEleicao
                        .FirstOrDefaultAsync(c => c.Id == chapaVencedora.ChapaId);
                    
                    if (chapa != null)
                    {
                        chapa.Status = StatusChapa.Eleita;
                    }
                }
            }

            // Finalizar apuração
            resultado.DataFimApuracao = DateTime.Now;
            resultado.TotalVotosValidos = totalVotosValidos;
            resultado.TotalVotosBrancos = totalVotosBrancos;
            resultado.TotalVotosNulos = totalVotosNulos;
            resultado.TotalGeralVotos = totalGeral;
            resultado.Status = StatusApuracao.Concluida;
            resultado.HashResultado = GerarHashSeguranca($"{resultado.Id}-{totalGeral}-{DateTime.Now.Ticks}");

            // Marcar sessão como apurada
            sessao.Status = StatusSessaoVotacao.Apurada;

            await _context.SaveChangesAsync();

            // Notificar resultado
            await NotificarResultadoApuracaoAsync(resultado, resultadosChapas);

            // Publicar boletim de urna
            BackgroundJob.Enqueue(() => GerarBoletimUrnaAsync(resultado.Id));

            _logger.LogInformation($"Apuração concluída para sessão {sessaoVotacaoId}");

            return MapearResultadoApuracaoDTO(resultado, resultadosChapas);
        }

        #endregion

        #region Consultas e Estatísticas

        /// <summary>
        /// Obtém status da votação
        /// </summary>
        public async Task<StatusVotacaoDTO> ObterStatusVotacaoAsync(int calendarioId, int ufId)
        {
            var sessao = await _context.SessoesVotacao
                .Include(s => s.EstatisticasVotacao)
                .FirstOrDefaultAsync(s => 
                    s.CalendarioId == calendarioId &&
                    s.UfId == ufId &&
                    s.Status != StatusSessaoVotacao.Cancelada);

            if (sessao == null)
            {
                return new StatusVotacaoDTO
                {
                    Status = "NÃO_INICIADA",
                    PodeVotar = false
                };
            }

            var estatisticas = sessao.EstatisticasVotacao.FirstOrDefault();

            return new StatusVotacaoDTO
            {
                SessaoVotacaoId = sessao.Id,
                Status = sessao.Status.ToString(),
                DataAbertura = sessao.DataAbertura,
                DataFechamento = sessao.DataFechamento,
                PodeVotar = sessao.Status == StatusSessaoVotacao.Aberta,
                TotalEleitores = estatisticas?.TotalEleitores ?? 0,
                TotalVotantes = estatisticas?.TotalVotantes ?? 0,
                PercentualParticipacao = estatisticas?.PercentualParticipacao ?? 0
            };
        }

        /// <summary>
        /// Verifica comprovante de voto
        /// </summary>
        public async Task<bool> VerificarComprovanteAsync(string protocolo, string codigoVerificacao)
        {
            var voto = await _context.Votos
                .FirstOrDefaultAsync(v => v.ProtocoloComprovante == protocolo);

            if (voto == null)
                return false;

            // Verificar código
            var codigoEsperado = GerarCodigoVerificacao(protocolo);
            return codigoVerificacao == codigoEsperado;
        }

        /// <summary>
        /// Obtém estatísticas da votação
        /// </summary>
        public async Task<EstatisticasVotacaoDTO> ObterEstatisticasAsync(int sessaoVotacaoId)
        {
            var sessao = await _context.SessoesVotacao
                .Include(s => s.UrnasEletronicas)
                    .ThenInclude(u => u.Chapa)
                .Include(s => s.EstatisticasVotacao)
                .FirstOrDefaultAsync(s => s.Id == sessaoVotacaoId);

            if (sessao == null)
                throw new Exception("Sessão de votação não encontrada");

            var estatisticas = sessao.EstatisticasVotacao.FirstOrDefault();
            
            var votosHora = await _context.Votos
                .Where(v => v.SessaoVotacaoId == sessaoVotacaoId)
                .GroupBy(v => v.DataHoraVoto.Hour)
                .Select(g => new VotosPorHoraDTO
                {
                    Hora = g.Key,
                    QuantidadeVotos = g.Count()
                })
                .ToListAsync();

            return new EstatisticasVotacaoDTO
            {
                SessaoVotacaoId = sessaoVotacaoId,
                TotalEleitores = estatisticas?.TotalEleitores ?? 0,
                TotalVotantes = estatisticas?.TotalVotantes ?? 0,
                PercentualParticipacao = estatisticas?.PercentualParticipacao ?? 0,
                VotosPorHora = votosHora,
                ChapasVotacao = sessao.UrnasEletronicas.Select(u => new ChapaVotacaoDTO
                {
                    ChapaId = u.ChapaId.Value,
                    NumeroChapa = u.Chapa?.NumeroChapa,
                    NomeChapa = u.Chapa?.Nome,
                    TotalVotos = u.TotalVotos
                }).ToList()
            };
        }

        #endregion

        #region Métodos Auxiliares

        private string GerarHashSeguranca(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private string GerarHashVoto(RegistrarVotoDTO dto)
        {
            var data = $"{dto.SessaoVotacaoId}-{dto.EleitorId}-{DateTime.Now.Ticks}-{Guid.NewGuid()}";
            return GerarHashSeguranca(data);
        }

        private string GerarProtocoloVoto()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"VOTO-{timestamp}-{random}";
        }

        private string GerarCodigoVerificacao(string protocolo)
        {
            var hash = GerarHashSeguranca(protocolo);
            return hash.Substring(0, 8).ToUpper();
        }

        private string CriptografarVoto(int? chapaId, TipoVoto tipoVoto)
        {
            // Implementação simplificada - em produção usar criptografia mais robusta
            var votoData = $"{tipoVoto}:{chapaId ?? 0}";
            var bytes = Encoding.UTF8.GetBytes(votoData);
            return Convert.ToBase64String(bytes);
        }

        private async Task<int> ObterTotalEleitoresAsync(int ufId)
        {
            return await _context.Profissionais
                .Where(p => 
                    p.StatusRegistro == StatusRegistroProfissional.Ativo &&
                    p.UfId == ufId)
                .CountAsync();
        }

        private async Task NotificarAberturaVotacaoAsync(SessaoVotacao sessao, string ufSigla)
        {
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.VotacaoIniciada,
                Titulo = "Votação Aberta",
                Mensagem = $"A votação para a eleição está aberta em {ufSigla}",
                CalendarioId = sessao.CalendarioId
            });
        }

        private async Task NotificarFechamentoVotacaoAsync(SessaoVotacao sessao)
        {
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.VotacaoEncerrada,
                Titulo = "Votação Encerrada",
                Mensagem = "O período de votação foi encerrado. Aguarde a apuração dos resultados.",
                CalendarioId = sessao.CalendarioId
            });
        }

        private async Task NotificarResultadoApuracaoAsync(ResultadoApuracao resultado, List<ResultadoChapaApuracao> resultadosChapas)
        {
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.ResultadoPublicado,
                Titulo = "Resultado da Eleição Disponível",
                Mensagem = "O resultado da eleição foi apurado e está disponível para consulta."
            });
        }

        [BackgroundJob]
        public async Task EnviarComprovanteVotoAsync(int eleitorId, ComprovanteVotoDTO comprovante)
        {
            var eleitor = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == eleitorId);

            if (eleitor == null)
                return;

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = new List<string> { eleitor.Email },
                Assunto = "Comprovante de Votação",
                TemplateId = "ComprovanteVoto",
                ParametrosTemplate = new Dictionary<string, string>
                {
                    ["NomeEleitor"] = eleitor.Nome,
                    ["Protocolo"] = comprovante.ProtocoloComprovante,
                    ["DataHora"] = comprovante.DataHoraVoto.ToString("dd/MM/yyyy HH:mm:ss"),
                    ["CodigoVerificacao"] = comprovante.CodigoVerificacao,
                    ["LinkVerificacao"] = $"/votacao/verificar/{comprovante.ProtocoloComprovante}"
                }
            });
        }

        [BackgroundJob]
        public async Task GerarBoletimUrnaAsync(int resultadoApuracaoId)
        {
            // Implementar geração de boletim de urna em PDF
            await Task.CompletedTask;
        }

        private ResultadoApuracaoDTO MapearResultadoApuracaoDTO(ResultadoApuracao resultado, List<ResultadoChapaApuracao> resultadosChapas)
        {
            return new ResultadoApuracaoDTO
            {
                Id = resultado.Id,
                SessaoVotacaoId = resultado.SessaoVotacaoId,
                DataApuracao = resultado.DataFimApuracao ?? resultado.DataInicioApuracao,
                TotalVotosValidos = resultado.TotalVotosValidos,
                TotalVotosBrancos = resultado.TotalVotosBrancos,
                TotalVotosNulos = resultado.TotalVotosNulos,
                TotalGeralVotos = resultado.TotalGeralVotos,
                PrecisaSegundoTurno = resultado.PrecisaSegundoTurno,
                ChapaVencedoraId = resultado.ChapaVencedoraId,
                ResultadosChapas = resultadosChapas.Select(rc => new ResultadoChapaDTO
                {
                    ChapaId = rc.ChapaId,
                    TotalVotos = rc.TotalVotos,
                    PercentualVotos = rc.PercentualVotos,
                    Posicao = 0 // Será calculado
                }).OrderByDescending(r => r.TotalVotos).ToList()
            };
        }

        #endregion
    }
}