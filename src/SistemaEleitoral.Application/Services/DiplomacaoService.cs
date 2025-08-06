using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Application.Interfaces;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Entities.Diplomacao;
using SistemaEleitoral.Domain.Interfaces;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Service responsável pela diplomação e posse dos eleitos
    /// </summary>
    public class DiplomacaoService : IDiplomacaoService
    {
        private readonly ILogger<DiplomacaoService> _logger;
        private readonly IRepository<DiplomaEleitoral> _diplomaRepository;
        private readonly IRepository<TermoPosse> _termoPosseRepository;
        private readonly IRepository<MandatoConselheiro> _mandatoRepository;
        private readonly IChapaEleicaoRepository _chapaRepository;
        private readonly IEleicaoRepository _eleicaoRepository;
        private readonly IEmailService _emailService;
        private readonly IPdfService _pdfService;
        private readonly IUnitOfWork _unitOfWork;

        public DiplomacaoService(
            ILogger<DiplomacaoService> logger,
            IRepository<DiplomaEleitoral> diplomaRepository,
            IRepository<TermoPosse> termoPosseRepository,
            IRepository<MandatoConselheiro> mandatoRepository,
            IChapaEleicaoRepository chapaRepository,
            IEleicaoRepository eleicaoRepository,
            IEmailService emailService,
            IPdfService pdfService,
            IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _diplomaRepository = diplomaRepository;
            _termoPosseRepository = termoPosseRepository;
            _mandatoRepository = mandatoRepository;
            _chapaRepository = chapaRepository;
            _eleicaoRepository = eleicaoRepository;
            _emailService = emailService;
            _pdfService = pdfService;
            _unitOfWork = unitOfWork;
        }

        #region Diplomação

        public async Task<List<DiplomaEleitoral>> GerarDiplomasAsync(int eleicaoId)
        {
            try
            {
                _logger.LogInformation($"Iniciando geração de diplomas para eleição {eleicaoId}");

                var eleicao = await _eleicaoRepository.GetByIdComChapasAsync(eleicaoId);
                if (eleicao == null)
                    throw new ArgumentException("Eleição não encontrada");

                if (eleicao.Status != StatusEleicao.Finalizada)
                    throw new InvalidOperationException("Eleição deve estar finalizada para gerar diplomas");

                var diplomas = new List<DiplomaEleitoral>();
                var chapasEleitas = eleicao.Chapas.Where(c => c.Eleita).OrderBy(c => c.Classificacao);

                foreach (var chapa in chapasEleitas)
                {
                    // Gerar diploma para cada membro da chapa
                    foreach (var membro in chapa.Membros.Where(m => m.Elegivel))
                    {
                        var diploma = await CriarDiplomaAsync(eleicao, chapa, membro);
                        diplomas.Add(diploma);
                    }
                }

                // Gerar diplomas para suplentes
                var chapasSuplentes = eleicao.Chapas.Where(c => c.Status == StatusChapa.Homologada && !c.Eleita)
                    .OrderBy(c => c.Classificacao)
                    .Take(eleicao.NumeroSuplentes);

                foreach (var chapa in chapasSuplentes)
                {
                    foreach (var membro in chapa.Membros.Where(m => m.Elegivel))
                    {
                        var diploma = await CriarDiplomaAsync(eleicao, chapa, membro, TipoDiploma.Suplente);
                        diplomas.Add(diploma);
                    }
                }

                await _unitOfWork.CommitAsync();

                _logger.LogInformation($"Gerados {diplomas.Count} diplomas para eleição {eleicaoId}");
                return diplomas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao gerar diplomas para eleição {eleicaoId}");
                throw;
            }
        }

        private async Task<DiplomaEleitoral> CriarDiplomaAsync(
            Eleicao eleicao, 
            ChapaEleicao chapa, 
            MembroChapa membro,
            TipoDiploma tipo = TipoDiploma.Titular)
        {
            var diploma = new DiplomaEleitoral
            {
                EleicaoId = eleicao.Id,
                ChapaEleicaoId = chapa.Id,
                MembroChapaId = membro.Id,
                TipoDiploma = tipo,
                Cargo = membro.Cargo,
                DataValidadeInicio = eleicao.DataPosse,
                DataValidadeFim = eleicao.DataPosse.AddYears(eleicao.MandatoAnos),
                TextoDiploma = GerarTextoDiploma(eleicao, chapa, membro, tipo),
                Considerandos = GerarConsiderandos(eleicao),
                Status = StatusDiploma.AguardandoAssinatura
            };

            await _diplomaRepository.AddAsync(diploma);

            // Gerar PDF do diploma
            diploma.CaminhoArquivoPDF = await GerarPdfDiplomaAsync(diploma);
            diploma.HashDocumento = GerarHashDocumento(diploma);

            return diploma;
        }

        private string GerarTextoDiploma(Eleicao eleicao, ChapaEleicao chapa, MembroChapa membro, TipoDiploma tipo)
        {
            var tipoTexto = tipo == TipoDiploma.Titular ? "TITULAR" : "SUPLENTE";
            
            return $@"
                O CONSELHO DE ARQUITETURA E URBANISMO DO BRASIL - CAU/BR,
                no uso de suas atribuições legais e regulamentares,
                
                CERTIFICA que {membro.Profissional?.Nome},
                portador do registro profissional nº {membro.Profissional?.RegistroNacional},
                foi eleito(a) como {tipoTexto} para o cargo de {membro.Cargo}
                na eleição realizada em {eleicao.DataInicioVotacao:dd/MM/yyyy},
                integrante da {chapa.Nome} - Chapa nº {chapa.Numero},
                
                com mandato de {eleicao.MandatoAnos} anos,
                iniciando em {eleicao.DataPosse:dd/MM/yyyy}
                e terminando em {eleicao.DataPosse.AddYears(eleicao.MandatoAnos):dd/MM/yyyy}.
                
                Por ser verdade, firma-se o presente diploma.
            ";
        }

        private string GerarConsiderandos(Eleicao eleicao)
        {
            return $@"
                CONSIDERANDO o resultado oficial da eleição homologado;
                CONSIDERANDO o cumprimento de todos os requisitos legais;
                CONSIDERANDO a ausência de impedimentos ou impugnações procedentes;
                CONSIDERANDO o quorum atingido de eleitores;
            ";
        }

        public async Task<bool> AssinarDiplomaDigitalmenteAsync(
            int diplomaId, 
            int assinanteId, 
            string certificadoDigital)
        {
            try
            {
                var diploma = await _diplomaRepository.GetByIdAsync(diplomaId);
                if (diploma == null)
                    throw new ArgumentException("Diploma não encontrado");

                if (diploma.Status != StatusDiploma.AguardandoAssinatura)
                    throw new InvalidOperationException("Diploma não está aguardando assinatura");

                diploma.AssinarDigitalmente(assinanteId, certificadoDigital);
                
                await _diplomaRepository.UpdateAsync(diploma);
                await _unitOfWork.CommitAsync();

                // Se todas as assinaturas foram coletadas, registrar o diploma
                if (diploma.Status == StatusDiploma.Assinado)
                {
                    await RegistrarDiplomaAsync(diploma);
                }

                _logger.LogInformation($"Diploma {diplomaId} assinado por {assinanteId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao assinar diploma {diplomaId}");
                throw;
            }
        }

        private async Task RegistrarDiplomaAsync(DiplomaEleitoral diploma)
        {
            diploma.Status = StatusDiploma.Registrado;
            await _diplomaRepository.UpdateAsync(diploma);
            
            // Notificar o eleito
            await NotificarDiplomaRegistrado(diploma);
        }

        public async Task<bool> EntregarDiplomaAsync(int diplomaId, string local, string recebidoPor)
        {
            try
            {
                var diploma = await _diplomaRepository.GetByIdAsync(diplomaId);
                if (diploma == null)
                    throw new ArgumentException("Diploma não encontrado");

                diploma.RegistrarEntrega(local, recebidoPor);
                
                await _diplomaRepository.UpdateAsync(diploma);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation($"Diploma {diplomaId} entregue em {local} para {recebidoPor}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar entrega do diploma {diplomaId}");
                throw;
            }
        }

        #endregion

        #region Posse

        public async Task<TermoPosse> AgendarPosseAsync(
            int diplomaId,
            DateTime dataPosse,
            TimeSpan horaPosse,
            string localPosse)
        {
            try
            {
                var diploma = await _diplomaRepository.GetByIdAsync(diplomaId);
                if (diploma == null)
                    throw new ArgumentException("Diploma não encontrado");

                if (diploma.Status != StatusDiploma.Registrado && diploma.Status != StatusDiploma.Entregue)
                    throw new InvalidOperationException("Diploma deve estar registrado para agendar posse");

                var termoPosse = new TermoPosse
                {
                    DiplomaEleitoralId = diplomaId,
                    EmpossadoId = diploma.MembroChapa.ProfissionalId,
                    Cargo = diploma.Cargo,
                    DataPosse = dataPosse,
                    HoraPosse = horaPosse,
                    LocalPosse = localPosse,
                    DuracaoMandatoAnos = (int)(diploma.DataValidadeFim - diploma.DataValidadeInicio).TotalDays / 365,
                    TextoJuramento = GerarTextoJuramento(),
                    Status = StatusPosse.Agendada
                };

                await _termoPosseRepository.AddAsync(termoPosse);
                await _unitOfWork.CommitAsync();

                // Notificar sobre agendamento
                await NotificarAgendamentoPosse(termoPosse);

                _logger.LogInformation($"Posse agendada para {dataPosse:dd/MM/yyyy} às {horaPosse}");
                return termoPosse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao agendar posse para diploma {diplomaId}");
                throw;
            }
        }

        private string GerarTextoJuramento()
        {
            return @"
                Prometo manter, defender e cumprir a Constituição Federal,
                observar as leis, promover o bem geral do povo brasileiro
                e sustentar a união, a integridade e a independência do Brasil.
                Prometo ainda exercer com dedicação, ética e responsabilidade
                as funções do cargo para o qual fui eleito(a),
                sempre visando o interesse público e o desenvolvimento
                da Arquitetura e Urbanismo no Brasil.
            ";
        }

        public async Task<bool> IniciarCerimoniaPosseAsync(
            int termoPosseId,
            string presidenteCerimonia,
            string secretarioCerimonia)
        {
            try
            {
                var termoPosse = await _termoPosseRepository.GetByIdAsync(termoPosseId);
                if (termoPosse == null)
                    throw new ArgumentException("Termo de posse não encontrado");

                if (termoPosse.Status != StatusPosse.Agendada)
                    throw new InvalidOperationException("Posse deve estar agendada");

                termoPosse.Status = StatusPosse.EmAndamento;
                termoPosse.PresidenteCerimonia = presidenteCerimonia;
                termoPosse.SecretarioCerimonia = secretarioCerimonia;

                await _termoPosseRepository.UpdateAsync(termoPosse);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation($"Cerimônia de posse {termoPosseId} iniciada");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao iniciar cerimônia de posse {termoPosseId}");
                throw;
            }
        }

        public async Task<bool> RegistrarJuramentoAsync(int termoPosseId)
        {
            try
            {
                var termoPosse = await _termoPosseRepository.GetByIdAsync(termoPosseId);
                if (termoPosse == null)
                    throw new ArgumentException("Termo de posse não encontrado");

                termoPosse.RegistrarJuramento();

                await _termoPosseRepository.UpdateAsync(termoPosse);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation($"Juramento registrado para posse {termoPosseId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao registrar juramento {termoPosseId}");
                throw;
            }
        }

        public async Task<MandatoConselheiro> ConcluirPosseAsync(int termoPosseId, string ataPosse)
        {
            try
            {
                var termoPosse = await _termoPosseRepository.GetByIdAsync(termoPosseId);
                if (termoPosse == null)
                    throw new ArgumentException("Termo de posse não encontrado");

                termoPosse.ConcluirPosse();
                termoPosse.AtaPosse = ataPosse;

                // Criar mandato
                var mandato = new MandatoConselheiro
                {
                    ConselheiroId = termoPosse.EmpossadoId,
                    EleicaoId = termoPosse.DiplomaEleitoral.EleicaoId,
                    Cargo = termoPosse.Cargo,
                    InicioMandato = termoPosse.InicioMandato,
                    FimMandatoPrevisto = termoPosse.FimMandato,
                    Status = StatusMandato.Ativo
                };

                await _mandatoRepository.AddAsync(mandato);
                await _termoPosseRepository.UpdateAsync(termoPosse);
                await _unitOfWork.CommitAsync();

                // Gerar documentos
                await GerarDocumentosPosseAsync(termoPosse);

                // Notificar conclusão
                await NotificarConclusaoPosse(termoPosse, mandato);

                _logger.LogInformation($"Posse {termoPosseId} concluída - Mandato {mandato.Id} criado");
                return mandato;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao concluir posse {termoPosseId}");
                throw;
            }
        }

        #endregion

        #region Gestão de Mandatos

        public async Task<bool> FinalizarMandatoAsync(
            int mandatoId,
            TipoFinalizacaoMandato tipo,
            string motivo)
        {
            try
            {
                var mandato = await _mandatoRepository.GetByIdAsync(mandatoId);
                if (mandato == null)
                    throw new ArgumentException("Mandato não encontrado");

                mandato.FinalizarMandato(tipo, motivo);

                await _mandatoRepository.UpdateAsync(mandato);
                await _unitOfWork.CommitAsync();

                // Se for renúncia ou cassação, convocar suplente
                if (tipo == TipoFinalizacaoMandato.Renuncia || tipo == TipoFinalizacaoMandato.Cassacao)
                {
                    await ConvocarSuplenteAsync(mandato);
                }

                _logger.LogInformation($"Mandato {mandatoId} finalizado: {tipo}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao finalizar mandato {mandatoId}");
                throw;
            }
        }

        public async Task<SubstituicaoMandato> SubstituirConselheiroAsync(
            int mandatoTitularId,
            int substitutoId,
            TipoSubstituicao tipo,
            string motivo,
            DateTime dataInicio,
            DateTime? dataFim = null)
        {
            try
            {
                var mandato = await _mandatoRepository.GetByIdAsync(mandatoTitularId);
                if (mandato == null)
                    throw new ArgumentException("Mandato não encontrado");

                var substituicao = new SubstituicaoMandato
                {
                    MandatoTitularId = mandatoTitularId,
                    SubstitutoId = substitutoId,
                    DataInicio = dataInicio,
                    DataFim = dataFim,
                    MotivoSubstituicao = motivo,
                    TipoSubstituicao = tipo,
                    NumeroAto = GerarNumeroAto(),
                    DataAto = DateTime.Now
                };

                mandato.Substituicoes.Add(substituicao);

                // Se for substituição temporária, suspender mandato titular
                if (tipo == TipoSubstituicao.Temporaria || tipo == TipoSubstituicao.Licenca)
                {
                    mandato.SuspenderMandato(motivo);
                }

                await _mandatoRepository.UpdateAsync(mandato);
                await _unitOfWork.CommitAsync();

                _logger.LogInformation($"Substituição criada para mandato {mandatoTitularId}");
                return substituicao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao criar substituição para mandato {mandatoTitularId}");
                throw;
            }
        }

        #endregion

        #region Métodos Auxiliares

        private async Task<string> GerarPdfDiplomaAsync(DiplomaEleitoral diploma)
        {
            var html = await GerarHtmlDiploma(diploma);
            var pdf = await _pdfService.GerarPdfAsync(html);
            var caminho = $"/diplomas/{diploma.NumeroRegistro}.pdf";
            await _pdfService.SalvarArquivoAsync(pdf, caminho);
            return caminho;
        }

        private async Task<string> GerarHtmlDiploma(DiplomaEleitoral diploma)
        {
            // Implementar template HTML do diploma
            return await Task.FromResult($"<html><body>{diploma.TextoDiploma}</body></html>");
        }

        private string GerarHashDocumento(DiplomaEleitoral diploma)
        {
            // Implementar geração de hash SHA256 do documento
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        private string GerarNumeroAto()
        {
            var ano = DateTime.Now.Year;
            var sequencial = new Random().Next(100, 999);
            return $"ATO-{sequencial}/{ano}";
        }

        private async Task ConvocarSuplenteAsync(MandatoConselheiro mandato)
        {
            // Implementar convocação de suplente
            await Task.CompletedTask;
        }

        private async Task GerarDocumentosPosseAsync(TermoPosse termoPosse)
        {
            // Gerar ata de posse em PDF
            var html = $"<html><body><h1>Ata de Posse</h1>{termoPosse.AtaPosse}</body></html>";
            var pdf = await _pdfService.GerarPdfAsync(html);
            var caminho = $"/atas/{termoPosse.NumeroTermo}.pdf";
            await _pdfService.SalvarArquivoAsync(pdf, caminho);
            termoPosse.CaminhoArquivoAta = caminho;
        }

        #endregion

        #region Notificações

        private async Task NotificarDiplomaRegistrado(DiplomaEleitoral diploma)
        {
            await _emailService.EnviarEmailAsync(
                diploma.MembroChapa?.Profissional?.Email ?? "",
                "Diploma Registrado",
                $"Seu diploma eleitoral foi registrado com sucesso. Número: {diploma.NumeroRegistro}");
        }

        private async Task NotificarAgendamentoPosse(TermoPosse termoPosse)
        {
            await _emailService.EnviarEmailAsync(
                termoPosse.Empossado?.Email ?? "",
                "Posse Agendada",
                $"Sua posse está agendada para {termoPosse.DataPosse:dd/MM/yyyy} às {termoPosse.HoraPosse}");
        }

        private async Task NotificarConclusaoPosse(TermoPosse termoPosse, MandatoConselheiro mandato)
        {
            await _emailService.EnviarEmailAsync(
                termoPosse.Empossado?.Email ?? "",
                "Posse Realizada",
                $"Sua posse foi realizada com sucesso. Mandato iniciado em {mandato.InicioMandato:dd/MM/yyyy}");
        }

        #endregion
    }
}