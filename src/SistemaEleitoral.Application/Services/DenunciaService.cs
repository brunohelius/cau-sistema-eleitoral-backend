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
    /// Service responsável pela lógica de negócio de Denúncias
    /// Sistema judicial completo - gestão de denúncias eleitorais
    /// </summary>
    public class DenunciaService : IDenunciaService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DenunciaService> _logger;
        private readonly ICalendarioService _calendarioService;
        private readonly INotificationService _notificationService;
        private readonly IComissaoEleitoralService _comissaoService;
        private readonly IArquivoService _arquivoService;
        
        public DenunciaService(
            ApplicationDbContext context,
            ILogger<DenunciaService> logger,
            ICalendarioService calendarioService,
            INotificationService notificationService,
            IComissaoEleitoralService comissaoService,
            IArquivoService arquivoService)
        {
            _context = context;
            _logger = logger;
            _calendarioService = calendarioService;
            _notificationService = notificationService;
            _comissaoService = comissaoService;
            _arquivoService = arquivoService;
        }

        #region Registro de Denúncias

        /// <summary>
        /// Registra uma nova denúncia
        /// </summary>
        public async Task<DenunciaDTO> RegistrarDenunciaAsync(RegistrarDenunciaDTO dto)
        {
            // Validar período para denúncias
            var periodoValido = await _calendarioService.ValidarPeriodoParaAcaoAsync(
                dto.CalendarioId, 
                TipoAtividadeCalendario.Denuncia);

            if (!periodoValido && !dto.ForaDoProzo)
            {
                throw new Exception("Fora do período para registro de denúncias");
            }

            // Validar tipo de denunciado
            await ValidarDenunciadoAsync(dto);

            // Criar nova denúncia
            var denuncia = new Denuncia
            {
                CalendarioId = dto.CalendarioId,
                UfId = dto.UfId,
                TipoDenuncia = dto.TipoDenuncia,
                Descricao = dto.Descricao,
                DataOcorrencia = dto.DataOcorrencia,
                LocalOcorrencia = dto.LocalOcorrencia,
                Situacao = SituacaoDenuncia.Registrada,
                DataRegistro = DateTime.Now,
                ProtocoloNumero = await GerarProtocoloAsync(dto.CalendarioId, dto.UfId),
                
                // Denunciante
                DenuncianteId = dto.DenuncianteId,
                DenuncianteAnonimo = dto.DenuncianteAnonimo,
                
                // Flags
                ForaDoPrazo = dto.ForaDoProzo,
                Urgente = dto.Urgente,
                Sigilosa = dto.Sigilosa
            };

            // Adicionar denunciados conforme o tipo
            if (dto.TipoDenunciado == TipoDenunciado.Chapa && dto.ChapaId.HasValue)
            {
                denuncia.DenunciaChapa = new DenunciaChapa
                {
                    Denuncia = denuncia,
                    ChapaId = dto.ChapaId.Value,
                    DataInclusao = DateTime.Now
                };
            }
            else if (dto.TipoDenunciado == TipoDenunciado.MembroChapa && dto.MembroChapaId.HasValue)
            {
                denuncia.DenunciaMembroChapa = new DenunciaMembroChapa
                {
                    Denuncia = denuncia,
                    MembroChapaId = dto.MembroChapaId.Value,
                    DataInclusao = DateTime.Now
                };
            }
            else if (dto.TipoDenunciado == TipoDenunciado.MembroComissao && dto.MembroComissaoId.HasValue)
            {
                denuncia.DenunciaMembroComissao = new DenunciaMembroComissao
                {
                    Denuncia = denuncia,
                    MembroComissaoId = dto.MembroComissaoId.Value,
                    DataInclusao = DateTime.Now
                };
            }
            else if (dto.TipoDenunciado == TipoDenunciado.Outro)
            {
                denuncia.DenunciaOutro = new DenunciaOutro
                {
                    Denuncia = denuncia,
                    Nome = dto.DenunciadoOutroNome,
                    Cpf = dto.DenunciadoOutroCpf,
                    Descricao = dto.DenunciadoOutroDescricao,
                    DataInclusao = DateTime.Now
                };
            }

            // Adicionar testemunhas
            if (dto.Testemunhas != null && dto.Testemunhas.Any())
            {
                foreach (var testemunha in dto.Testemunhas)
                {
                    denuncia.Testemunhas.Add(new TestemunhaDenuncia
                    {
                        Denuncia = denuncia,
                        Nome = testemunha.Nome,
                        Cpf = testemunha.Cpf,
                        Email = testemunha.Email,
                        Telefone = testemunha.Telefone,
                        Depoimento = testemunha.Depoimento,
                        DataInclusao = DateTime.Now
                    });
                }
            }

            // Adicionar arquivos/provas
            if (dto.Arquivos != null && dto.Arquivos.Any())
            {
                foreach (var arquivo in dto.Arquivos)
                {
                    var caminhoArquivo = await _arquivoService.SalvarArquivoAsync(
                        arquivo.ConteudoArquivo, 
                        arquivo.NomeArquivo,
                        "denuncias");

                    denuncia.ArquivosDenuncia.Add(new ArquivoDenuncia
                    {
                        Denuncia = denuncia,
                        NomeArquivo = arquivo.NomeArquivo,
                        CaminhoArquivo = caminhoArquivo,
                        TipoArquivo = arquivo.TipoArquivo,
                        TamanhoBytes = arquivo.ConteudoArquivo.Length,
                        DataUpload = DateTime.Now
                    });
                }
            }

            // Registrar situação inicial
            denuncia.HistoricoSituacoes.Add(new DenunciaSituacao
            {
                Denuncia = denuncia,
                Situacao = SituacaoDenuncia.Registrada,
                DataAlteracao = DateTime.Now,
                Observacao = "Denúncia registrada",
                UsuarioId = dto.UsuarioRegistroId
            });

            _context.Denuncias.Add(denuncia);
            await _context.SaveChangesAsync();

            // Registrar histórico
            await RegistrarHistoricoDenunciaAsync(denuncia.Id, "Denúncia registrada", dto.UsuarioRegistroId);

            // Notificar comissão eleitoral
            await NotificarNovaDenunciaAsync(denuncia);

            // Agendar email de confirmação
            if (!dto.DenuncianteAnonimo)
            {
                BackgroundJob.Enqueue(() => EnviarEmailConfirmacaoDenunciaAsync(denuncia.Id));
            }

            _logger.LogInformation($"Denúncia {denuncia.ProtocoloNumero} registrada com sucesso");

            return await ObterDenunciaPorIdAsync(denuncia.Id);
        }

        /// <summary>
        /// Gera número de protocolo único para a denúncia
        /// </summary>
        private async Task<string> GerarProtocoloAsync(int calendarioId, int ufId)
        {
            var calendario = await _context.Calendarios
                .FirstOrDefaultAsync(c => c.Id == calendarioId);

            var uf = await _context.Ufs
                .FirstOrDefaultAsync(u => u.Id == ufId);

            var sequencial = await _context.Denuncias
                .Where(d => d.CalendarioId == calendarioId && d.UfId == ufId)
                .CountAsync() + 1;

            // Formato: DEN-ANO-UF-SEQUENCIAL
            return $"DEN-{calendario.Ano}-{uf.Sigla}-{sequencial:D5}";
        }

        #endregion

        #region Encaminhamento e Análise

        /// <summary>
        /// Encaminha denúncia para relator
        /// </summary>
        public async Task<bool> EncaminharParaRelatorAsync(int denunciaId, EncaminharDenunciaDTO dto)
        {
            var denuncia = await _context.Denuncias
                .Include(d => d.EncaminhamentoDenuncia)
                .FirstOrDefaultAsync(d => d.Id == denunciaId);

            if (denuncia == null)
                throw new Exception("Denúncia não encontrada");

            // Validar se pode ser encaminhada
            if (denuncia.Situacao != SituacaoDenuncia.Registrada && 
                denuncia.Situacao != SituacaoDenuncia.EmAnalise)
            {
                throw new Exception($"Denúncia não pode ser encaminhada no status {denuncia.Situacao}");
            }

            // Verificar se o relator é membro da comissão
            var membroComissao = await _context.MembrosComissao
                .Include(m => m.Comissao)
                .FirstOrDefaultAsync(m => 
                    m.Id == dto.RelatorId && 
                    m.Comissao.CalendarioId == denuncia.CalendarioId &&
                    m.Ativo);

            if (membroComissao == null)
                throw new Exception("Relator deve ser membro ativo da comissão eleitoral");

            // Criar ou atualizar encaminhamento
            if (denuncia.EncaminhamentoDenuncia == null)
            {
                denuncia.EncaminhamentoDenuncia = new EncaminhamentoDenuncia
                {
                    DenunciaId = denunciaId,
                    DataCriacao = DateTime.Now
                };
            }

            denuncia.EncaminhamentoDenuncia.RelatorId = dto.RelatorId;
            denuncia.EncaminhamentoDenuncia.DataEncaminhamento = DateTime.Now;
            denuncia.EncaminhamentoDenuncia.ObservacaoEncaminhamento = dto.Observacao;
            denuncia.EncaminhamentoDenuncia.PrazoAnalise = dto.PrazoAnalise ?? DateTime.Now.AddDays(10);
            denuncia.EncaminhamentoDenuncia.Prioridade = dto.Prioridade;

            // Atualizar situação
            denuncia.Situacao = SituacaoDenuncia.EmRelatoria;
            
            // Registrar mudança de situação
            denuncia.HistoricoSituacoes.Add(new DenunciaSituacao
            {
                DenunciaId = denunciaId,
                Situacao = SituacaoDenuncia.EmRelatoria,
                DataAlteracao = DateTime.Now,
                Observacao = $"Encaminhada para relator {membroComissao.Profissional?.Nome}",
                UsuarioId = dto.UsuarioEncaminhamentoId
            });

            await _context.SaveChangesAsync();

            // Notificar relator
            await NotificarRelatorDesignadoAsync(denuncia, membroComissao);

            // Registrar histórico
            await RegistrarHistoricoDenunciaAsync(
                denunciaId, 
                $"Denúncia encaminhada para relator {membroComissao.Profissional?.Nome}", 
                dto.UsuarioEncaminhamentoId);

            _logger.LogInformation($"Denúncia {denuncia.ProtocoloNumero} encaminhada para relator");

            return true;
        }

        /// <summary>
        /// Admite ou inadmite uma denúncia
        /// </summary>
        public async Task<bool> AdmitirInadmitirDenunciaAsync(int denunciaId, AdmitirDenunciaDTO dto)
        {
            var denuncia = await _context.Denuncias
                .Include(d => d.EncaminhamentoDenuncia)
                .FirstOrDefaultAsync(d => d.Id == denunciaId);

            if (denuncia == null)
                throw new Exception("Denúncia não encontrada");

            if (denuncia.Situacao != SituacaoDenuncia.EmRelatoria)
                throw new Exception("Denúncia deve estar em relatoria para ser admitida/inadmitida");

            if (dto.Admitir)
            {
                // Admitir denúncia
                denuncia.Situacao = SituacaoDenuncia.Admitida;
                
                var denunciaAdmitida = new DenunciaAdmitida
                {
                    DenunciaId = denunciaId,
                    DataAdmissao = DateTime.Now,
                    Parecer = dto.Parecer,
                    RelatorId = denuncia.EncaminhamentoDenuncia.RelatorId,
                    PrazoDefesa = dto.PrazoDefesa ?? DateTime.Now.AddDays(15)
                };

                _context.DenunciasAdmitidas.Add(denunciaAdmitida);

                // Agendar notificação de defesa
                BackgroundJob.Enqueue(() => NotificarPrazoDefesaAsync(denunciaId));
            }
            else
            {
                // Inadmitir denúncia
                denuncia.Situacao = SituacaoDenuncia.Inadmitida;
                
                var denunciaInadmitida = new DenunciaInadmitida
                {
                    DenunciaId = denunciaId,
                    DataInadmissao = DateTime.Now,
                    Motivo = dto.MotivoInadmissao,
                    Parecer = dto.Parecer,
                    RelatorId = denuncia.EncaminhamentoDenuncia.RelatorId
                };

                _context.DenunciasInadmitidas.Add(denunciaInadmitida);
            }

            // Anexar documentos do parecer
            if (dto.Arquivos != null && dto.Arquivos.Any())
            {
                foreach (var arquivo in dto.Arquivos)
                {
                    var caminhoArquivo = await _arquivoService.SalvarArquivoAsync(
                        arquivo.ConteudoArquivo,
                        arquivo.NomeArquivo,
                        dto.Admitir ? "denuncias/admitidas" : "denuncias/inadmitidas");

                    if (dto.Admitir)
                    {
                        _context.ArquivosDenunciaAdmitida.Add(new ArquivoDenunciaAdmitida
                        {
                            DenunciaId = denunciaId,
                            NomeArquivo = arquivo.NomeArquivo,
                            CaminhoArquivo = caminhoArquivo,
                            TamanhoBytes = arquivo.ConteudoArquivo.Length,
                            DataUpload = DateTime.Now
                        });
                    }
                    else
                    {
                        _context.ArquivosDenunciaInadmitida.Add(new ArquivoDenunciaInadmitida
                        {
                            DenunciaId = denunciaId,
                            NomeArquivo = arquivo.NomeArquivo,
                            CaminhoArquivo = caminhoArquivo,
                            TamanhoBytes = arquivo.ConteudoArquivo.Length,
                            DataUpload = DateTime.Now
                        });
                    }
                }
            }

            // Registrar mudança de situação
            denuncia.HistoricoSituacoes.Add(new DenunciaSituacao
            {
                DenunciaId = denunciaId,
                Situacao = denuncia.Situacao,
                DataAlteracao = DateTime.Now,
                Observacao = dto.Admitir ? "Denúncia admitida" : $"Denúncia inadmitida: {dto.MotivoInadmissao}",
                UsuarioId = dto.UsuarioId
            });

            await _context.SaveChangesAsync();

            // Notificar partes interessadas
            BackgroundJob.Enqueue(() => EnviarEmailAdmissaoInadmissaoAsync(denunciaId, dto.Admitir));

            // Registrar histórico
            await RegistrarHistoricoDenunciaAsync(
                denunciaId,
                dto.Admitir ? "Denúncia admitida" : "Denúncia inadmitida",
                dto.UsuarioId);

            _logger.LogInformation($"Denúncia {denuncia.ProtocoloNumero} {(dto.Admitir ? "admitida" : "inadmitida")}");

            return true;
        }

        #endregion

        #region Defesa e Contraditório

        /// <summary>
        /// Registra defesa para uma denúncia
        /// </summary>
        public async Task<bool> RegistrarDefesaAsync(int denunciaId, RegistrarDefesaDTO dto)
        {
            var denuncia = await _context.Denuncias
                .Include(d => d.DenunciaAdmitida)
                .FirstOrDefaultAsync(d => d.Id == denunciaId);

            if (denuncia == null)
                throw new Exception("Denúncia não encontrada");

            if (denuncia.Situacao != SituacaoDenuncia.Admitida)
                throw new Exception("Denúncia deve estar admitida para receber defesa");

            // Verificar prazo
            if (denuncia.DenunciaAdmitida.PrazoDefesa < DateTime.Now && !dto.ForaDoPrazo)
                throw new Exception("Prazo para defesa expirado");

            // Criar defesa
            var defesa = new DenunciaDefesa
            {
                DenunciaId = denunciaId,
                Argumentacao = dto.Argumentacao,
                DataApresentacao = DateTime.Now,
                ApresentadaPor = dto.ApresentadaPorId,
                ForaDoPrazo = dto.ForaDoPrazo
            };

            // Anexar documentos da defesa
            if (dto.Arquivos != null && dto.Arquivos.Any())
            {
                foreach (var arquivo in dto.Arquivos)
                {
                    var caminhoArquivo = await _arquivoService.SalvarArquivoAsync(
                        arquivo.ConteudoArquivo,
                        arquivo.NomeArquivo,
                        "denuncias/defesas");

                    defesa.ArquivosDefesa.Add(new ArquivoDenunciaDefesa
                    {
                        DenunciaDefesa = defesa,
                        NomeArquivo = arquivo.NomeArquivo,
                        CaminhoArquivo = caminhoArquivo,
                        TipoArquivo = arquivo.TipoArquivo,
                        TamanhoBytes = arquivo.ConteudoArquivo.Length,
                        DataUpload = DateTime.Now
                    });
                }
            }

            _context.DenunciasDefesas.Add(defesa);

            // Atualizar situação
            denuncia.Situacao = SituacaoDenuncia.ComDefesa;
            
            // Registrar mudança de situação
            denuncia.HistoricoSituacoes.Add(new DenunciaSituacao
            {
                DenunciaId = denunciaId,
                Situacao = SituacaoDenuncia.ComDefesa,
                DataAlteracao = DateTime.Now,
                Observacao = "Defesa apresentada",
                UsuarioId = dto.UsuarioRegistroId
            });

            await _context.SaveChangesAsync();

            // Notificar comissão
            await NotificarDefesaApresentadaAsync(denuncia);

            // Registrar histórico
            await RegistrarHistoricoDenunciaAsync(
                denunciaId,
                "Defesa apresentada",
                dto.UsuarioRegistroId);

            _logger.LogInformation($"Defesa registrada para denúncia {denuncia.ProtocoloNumero}");

            return true;
        }

        #endregion

        #region Consultas e Listagens

        /// <summary>
        /// Obtém denúncia por ID com todas as informações
        /// </summary>
        public async Task<DenunciaDTO> ObterDenunciaPorIdAsync(int id)
        {
            var denuncia = await _context.Denuncias
                .Include(d => d.Calendario)
                .Include(d => d.Uf)
                .Include(d => d.Denunciante)
                .Include(d => d.DenunciaChapa)
                    .ThenInclude(dc => dc.Chapa)
                .Include(d => d.DenunciaMembroChapa)
                    .ThenInclude(dmc => dmc.MembroChapa)
                        .ThenInclude(mc => mc.Profissional)
                .Include(d => d.DenunciaMembroComissao)
                    .ThenInclude(dmc => dmc.MembroComissao)
                        .ThenInclude(mc => mc.Profissional)
                .Include(d => d.DenunciaOutro)
                .Include(d => d.Testemunhas)
                .Include(d => d.ArquivosDenuncia)
                .Include(d => d.HistoricoSituacoes)
                .Include(d => d.EncaminhamentoDenuncia)
                    .ThenInclude(e => e.Relator)
                .Include(d => d.DenunciaAdmitida)
                .Include(d => d.DenunciaInadmitida)
                .Include(d => d.Defesas)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (denuncia == null)
                throw new Exception("Denúncia não encontrada");

            return MapearParaDTO(denuncia);
        }

        /// <summary>
        /// Lista denúncias com filtros
        /// </summary>
        public async Task<ListaDenunciasPaginadaDTO> ListarDenunciasAsync(FiltroDenunciasDTO filtro)
        {
            var query = _context.Denuncias
                .Include(d => d.Calendario)
                .Include(d => d.Uf)
                .Include(d => d.Denunciante)
                .AsQueryable();

            // Aplicar filtros
            if (filtro.CalendarioId.HasValue)
                query = query.Where(d => d.CalendarioId == filtro.CalendarioId.Value);

            if (filtro.UfId.HasValue)
                query = query.Where(d => d.UfId == filtro.UfId.Value);

            if (!string.IsNullOrEmpty(filtro.Situacao))
            {
                if (Enum.TryParse<SituacaoDenuncia>(filtro.Situacao, out var situacao))
                    query = query.Where(d => d.Situacao == situacao);
            }

            if (!string.IsNullOrEmpty(filtro.TipoDenuncia))
            {
                if (Enum.TryParse<TipoDenuncia>(filtro.TipoDenuncia, out var tipo))
                    query = query.Where(d => d.TipoDenuncia == tipo);
            }

            if (filtro.DataInicio.HasValue)
                query = query.Where(d => d.DataRegistro >= filtro.DataInicio.Value);

            if (filtro.DataFim.HasValue)
                query = query.Where(d => d.DataRegistro <= filtro.DataFim.Value);

            if (!string.IsNullOrEmpty(filtro.TextoBusca))
            {
                query = query.Where(d =>
                    d.ProtocoloNumero.Contains(filtro.TextoBusca) ||
                    d.Descricao.Contains(filtro.TextoBusca));
            }

            // Paginação
            var totalItens = await query.CountAsync();
            var totalPaginas = (int)Math.Ceiling(totalItens / (double)filtro.ItensPorPagina);

            var denuncias = await query
                .OrderByDescending(d => d.DataRegistro)
                .Skip((filtro.Pagina - 1) * filtro.ItensPorPagina)
                .Take(filtro.ItensPorPagina)
                .ToListAsync();

            return new ListaDenunciasPaginadaDTO
            {
                Denuncias = denuncias.Select(d => MapearParaDTO(d)).ToList(),
                PaginaAtual = filtro.Pagina,
                TotalPaginas = totalPaginas,
                TotalItens = totalItens,
                ItensPorPagina = filtro.ItensPorPagina
            };
        }

        /// <summary>
        /// Obtém denúncias por chapa
        /// </summary>
        public async Task<List<DenunciaDTO>> ObterDenunciasPorChapaAsync(int chapaId)
        {
            var denuncias = await _context.Denuncias
                .Include(d => d.DenunciaChapa)
                .Include(d => d.DenunciaMembroChapa)
                    .ThenInclude(dmc => dmc.MembroChapa)
                .Where(d => 
                    d.DenunciaChapa.ChapaId == chapaId ||
                    d.DenunciaMembroChapa.MembroChapa.ChapaId == chapaId)
                .ToListAsync();

            return denuncias.Select(d => MapearParaDTO(d)).ToList();
        }

        #endregion

        #region Estatísticas e Relatórios

        /// <summary>
        /// Obtém estatísticas de denúncias
        /// </summary>
        public async Task<EstatisticasDenunciasDTO> ObterEstatisticasAsync(int calendarioId)
        {
            var denuncias = await _context.Denuncias
                .Where(d => d.CalendarioId == calendarioId)
                .ToListAsync();

            return new EstatisticasDenunciasDTO
            {
                TotalDenuncias = denuncias.Count,
                DenunciasRegistradas = denuncias.Count(d => d.Situacao == SituacaoDenuncia.Registrada),
                DenunciasEmAnalise = denuncias.Count(d => d.Situacao == SituacaoDenuncia.EmAnalise),
                DenunciasEmRelatoria = denuncias.Count(d => d.Situacao == SituacaoDenuncia.EmRelatoria),
                DenunciasAdmitidas = denuncias.Count(d => d.Situacao == SituacaoDenuncia.Admitida),
                DenunciasInadmitidas = denuncias.Count(d => d.Situacao == SituacaoDenuncia.Inadmitida),
                DenunciasComDefesa = denuncias.Count(d => d.Situacao == SituacaoDenuncia.ComDefesa),
                DenunciasJulgadas = denuncias.Count(d => d.Situacao == SituacaoDenuncia.Julgada),
                DenunciasPorTipo = denuncias
                    .GroupBy(d => d.TipoDenuncia)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                DenunciasPorUF = denuncias
                    .GroupBy(d => d.UfId)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count())
            };
        }

        #endregion

        #region Métodos Auxiliares

        private async Task ValidarDenunciadoAsync(RegistrarDenunciaDTO dto)
        {
            switch (dto.TipoDenunciado)
            {
                case TipoDenunciado.Chapa:
                    if (!dto.ChapaId.HasValue)
                        throw new Exception("Chapa denunciada deve ser informada");
                    
                    var chapaExiste = await _context.ChapasEleicao
                        .AnyAsync(c => c.Id == dto.ChapaId.Value);
                    
                    if (!chapaExiste)
                        throw new Exception("Chapa denunciada não encontrada");
                    break;

                case TipoDenunciado.MembroChapa:
                    if (!dto.MembroChapaId.HasValue)
                        throw new Exception("Membro de chapa denunciado deve ser informado");
                    
                    var membroExiste = await _context.MembrosChapa
                        .AnyAsync(m => m.Id == dto.MembroChapaId.Value);
                    
                    if (!membroExiste)
                        throw new Exception("Membro de chapa denunciado não encontrado");
                    break;

                case TipoDenunciado.MembroComissao:
                    if (!dto.MembroComissaoId.HasValue)
                        throw new Exception("Membro de comissão denunciado deve ser informado");
                    
                    var membroComissaoExiste = await _context.MembrosComissao
                        .AnyAsync(m => m.Id == dto.MembroComissaoId.Value);
                    
                    if (!membroComissaoExiste)
                        throw new Exception("Membro de comissão denunciado não encontrado");
                    break;

                case TipoDenunciado.Outro:
                    if (string.IsNullOrEmpty(dto.DenunciadoOutroNome))
                        throw new Exception("Nome do denunciado deve ser informado");
                    break;
            }
        }

        private async Task RegistrarHistoricoDenunciaAsync(int denunciaId, string descricao, int usuarioId)
        {
            var historico = new HistoricoDenuncia
            {
                DenunciaId = denunciaId,
                Descricao = descricao,
                DataAlteracao = DateTime.Now,
                UsuarioId = usuarioId
            };

            _context.HistoricosDenuncia.Add(historico);
            await _context.SaveChangesAsync();
        }

        private async Task NotificarNovaDenunciaAsync(Denuncia denuncia)
        {
            // Notificar comissão eleitoral da UF
            var comissao = await _comissaoService.ObterComissaoPorUFAsync(denuncia.UfId, denuncia.CalendarioId);
            
            if (comissao != null)
            {
                await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
                {
                    Tipo = TipoNotificacao.DenunciaRecebida,
                    Titulo = "Nova Denúncia Recebida",
                    Mensagem = $"Nova denúncia protocolo {denuncia.ProtocoloNumero} recebida",
                    DenunciaId = denuncia.Id,
                    DestinatariosIds = comissao.Membros.Select(m => m.ProfissionalId).ToList()
                });
            }
        }

        private async Task NotificarRelatorDesignadoAsync(Denuncia denuncia, MembroComissao relator)
        {
            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = new List<string> { relator.Profissional?.Email },
                Assunto = "Designação como Relator de Denúncia",
                TemplateId = "RelatorDesignado",
                ParametrosTemplate = new Dictionary<string, string>
                {
                    ["NomeRelator"] = relator.Profissional?.Nome,
                    ["ProtocoloDenuncia"] = denuncia.ProtocoloNumero,
                    ["PrazoAnalise"] = denuncia.EncaminhamentoDenuncia.PrazoAnalise.ToString("dd/MM/yyyy"),
                    ["LinkDenuncia"] = $"/denuncias/{denuncia.Id}"
                }
            });
        }

        private async Task NotificarDefesaApresentadaAsync(Denuncia denuncia)
        {
            await _notificationService.EnviarNotificacaoAsync(new NotificacaoModel
            {
                Tipo = TipoNotificacao.DefesaApresentada,
                Titulo = "Defesa Apresentada",
                Mensagem = $"Defesa apresentada para denúncia {denuncia.ProtocoloNumero}",
                DenunciaId = denuncia.Id
            });
        }

        [BackgroundJob]
        public async Task EnviarEmailConfirmacaoDenunciaAsync(int denunciaId)
        {
            var denuncia = await _context.Denuncias
                .Include(d => d.Denunciante)
                .FirstOrDefaultAsync(d => d.Id == denunciaId);

            if (denuncia == null || denuncia.DenuncianteAnonimo)
                return;

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = new List<string> { denuncia.Denunciante?.Email },
                Assunto = "Confirmação de Registro de Denúncia",
                TemplateId = "ConfirmacaoDenuncia",
                ParametrosTemplate = new Dictionary<string, string>
                {
                    ["NomeDenunciante"] = denuncia.Denunciante?.Nome,
                    ["ProtocoloDenuncia"] = denuncia.ProtocoloNumero,
                    ["DataRegistro"] = denuncia.DataRegistro.ToString("dd/MM/yyyy HH:mm"),
                    ["LinkAcompanhamento"] = $"/denuncias/acompanhar/{denuncia.ProtocoloNumero}"
                }
            });
        }

        [BackgroundJob]
        public async Task NotificarPrazoDefesaAsync(int denunciaId)
        {
            // Implementar notificação de prazo de defesa
            await Task.CompletedTask;
        }

        [BackgroundJob]
        public async Task EnviarEmailAdmissaoInadmissaoAsync(int denunciaId, bool admitida)
        {
            // Implementar email de admissão/inadmissão
            await Task.CompletedTask;
        }

        private DenunciaDTO MapearParaDTO(Denuncia denuncia)
        {
            return new DenunciaDTO
            {
                Id = denuncia.Id,
                ProtocoloNumero = denuncia.ProtocoloNumero,
                CalendarioId = denuncia.CalendarioId,
                UfId = denuncia.UfId,
                UfNome = denuncia.Uf?.Nome,
                TipoDenuncia = denuncia.TipoDenuncia.ToString(),
                Situacao = denuncia.Situacao.ToString(),
                Descricao = denuncia.Descricao,
                DataOcorrencia = denuncia.DataOcorrencia,
                LocalOcorrencia = denuncia.LocalOcorrencia,
                DataRegistro = denuncia.DataRegistro,
                DenuncianteId = denuncia.DenuncianteId,
                DenuncianteNome = denuncia.Denunciante?.Nome,
                DenuncianteAnonimo = denuncia.DenuncianteAnonimo,
                Urgente = denuncia.Urgente,
                Sigilosa = denuncia.Sigilosa,
                ForaDoPrazo = denuncia.ForaDoPrazo,
                
                // Denunciados
                DenunciadoChapa = denuncia.DenunciaChapa != null ? new DenunciadoChapaDTO
                {
                    ChapaId = denuncia.DenunciaChapa.ChapaId,
                    NumeroChapa = denuncia.DenunciaChapa.Chapa?.NumeroChapa,
                    NomeChapa = denuncia.DenunciaChapa.Chapa?.Nome
                } : null,
                
                DenunciadoMembroChapa = denuncia.DenunciaMembroChapa != null ? new DenunciadoMembroChapaDTO
                {
                    MembroChapaId = denuncia.DenunciaMembroChapa.MembroChapaId,
                    NomeMembro = denuncia.DenunciaMembroChapa.MembroChapa?.Profissional?.Nome
                } : null,
                
                DenunciadoMembroComissao = denuncia.DenunciaMembroComissao != null ? new DenunciadoMembroComissaoDTO
                {
                    MembroComissaoId = denuncia.DenunciaMembroComissao.MembroComissaoId,
                    NomeMembro = denuncia.DenunciaMembroComissao.MembroComissao?.Profissional?.Nome
                } : null,
                
                DenunciadoOutro = denuncia.DenunciaOutro != null ? new DenunciadoOutroDTO
                {
                    Nome = denuncia.DenunciaOutro.Nome,
                    Cpf = denuncia.DenunciaOutro.Cpf,
                    Descricao = denuncia.DenunciaOutro.Descricao
                } : null,
                
                // Listas
                Testemunhas = denuncia.Testemunhas?.Select(t => new TestemunhaDenunciaDTO
                {
                    Id = t.Id,
                    Nome = t.Nome,
                    Cpf = t.Cpf,
                    Email = t.Email,
                    Telefone = t.Telefone,
                    Depoimento = t.Depoimento
                }).ToList(),
                
                Arquivos = denuncia.ArquivosDenuncia?.Select(a => new ArquivoDenunciaDTO
                {
                    Id = a.Id,
                    NomeArquivo = a.NomeArquivo,
                    CaminhoArquivo = a.CaminhoArquivo,
                    TipoArquivo = a.TipoArquivo,
                    TamanhoBytes = a.TamanhoBytes,
                    DataUpload = a.DataUpload
                }).ToList(),
                
                // Encaminhamento
                Encaminhamento = denuncia.EncaminhamentoDenuncia != null ? new EncaminhamentoDenunciaDTO
                {
                    RelatorId = denuncia.EncaminhamentoDenuncia.RelatorId,
                    NomeRelator = denuncia.EncaminhamentoDenuncia.Relator?.Profissional?.Nome,
                    DataEncaminhamento = denuncia.EncaminhamentoDenuncia.DataEncaminhamento,
                    PrazoAnalise = denuncia.EncaminhamentoDenuncia.PrazoAnalise,
                    Prioridade = denuncia.EncaminhamentoDenuncia.Prioridade
                } : null
            };
        }

        #endregion
    }
}