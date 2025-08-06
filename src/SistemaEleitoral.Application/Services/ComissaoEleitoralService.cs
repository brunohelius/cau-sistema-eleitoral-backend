using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Infrastructure.Data;

namespace SistemaEleitoral.Application.Services
{
    /// <summary>
    /// Service responsável pela gestão de Comissões Eleitorais
    /// </summary>
    public class ComissaoEleitoralService : IComissaoEleitoralService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComissaoEleitoralService> _logger;
        private readonly INotificationService _notificationService;
        
        // Constantes para composição das comissões
        private const int MEMBROS_COMISSAO_NACIONAL = 5;
        private const int MEMBROS_COMISSAO_ESTADUAL = 3;
        
        public ComissaoEleitoralService(
            ApplicationDbContext context,
            ILogger<ComissaoEleitoralService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Verifica se existe comissão para a UF e eleição especificadas
        /// </summary>
        public async Task<bool> ExisteComissaoParaUFAsync(int ufId, int eleicaoId)
        {
            return await _context.ComissoesEleitorais
                .AnyAsync(c => c.UfId == ufId && c.EleicaoId == eleicaoId && c.Ativa);
        }

        /// <summary>
        /// Obtém comissão por UF e eleição
        /// </summary>
        public async Task<ComissaoEleitoralDTO> ObterComissaoPorUFAsync(int ufId, int eleicaoId)
        {
            var comissao = await _context.ComissoesEleitorais
                .Include(c => c.Membros)
                    .ThenInclude(m => m.Profissional)
                .Include(c => c.Uf)
                .Include(c => c.Eleicao)
                .FirstOrDefaultAsync(c => c.UfId == ufId && c.EleicaoId == eleicaoId && c.Ativa);

            if (comissao == null)
                return null;

            return MapearParaDTO(comissao);
        }

        /// <summary>
        /// Obtém todas as comissões de uma eleição
        /// </summary>
        public async Task<List<ComissaoEleitoralDTO>> ObterComissoesPorEleicaoAsync(int eleicaoId)
        {
            var comissoes = await _context.ComissoesEleitorais
                .Include(c => c.Membros)
                    .ThenInclude(m => m.Profissional)
                .Include(c => c.Uf)
                .Include(c => c.Eleicao)
                .Where(c => c.EleicaoId == eleicaoId && c.Ativa)
                .ToListAsync();

            return comissoes.Select(c => MapearParaDTO(c)).ToList();
        }

        /// <summary>
        /// Cria uma nova comissão eleitoral
        /// </summary>
        public async Task<ComissaoEleitoralDTO> CriarComissaoAsync(CriarComissaoDTO dto)
        {
            // Validar se já existe comissão para a UF/eleição
            if (dto.UfId.HasValue)
            {
                var comissaoExistente = await ExisteComissaoParaUFAsync(dto.UfId.Value, dto.EleicaoId);
                if (comissaoExistente)
                    throw new Exception($"Já existe comissão eleitoral para esta UF e eleição");
            }

            var comissao = new ComissaoEleitoral
            {
                Nome = dto.Nome,
                Tipo = dto.Tipo,
                EleicaoId = dto.EleicaoId,
                UfId = dto.UfId,
                DataCriacao = DateTime.Now,
                Ativa = true,
                QuantidadeMembrosNecessarios = CalcularQuantidadeMembrosNecessariosAsync(dto.Tipo).Result
            };

            _context.ComissoesEleitorais.Add(comissao);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Comissão {comissao.Nome} criada com sucesso");

            return MapearParaDTO(comissao);
        }

        /// <summary>
        /// Adiciona um membro à comissão
        /// </summary>
        public async Task<bool> AdicionarMembroAsync(int comissaoId, AdicionarMembroComissaoDTO dto)
        {
            var comissao = await _context.ComissoesEleitorais
                .Include(c => c.Membros)
                .FirstOrDefaultAsync(c => c.Id == comissaoId);

            if (comissao == null)
                throw new Exception("Comissão não encontrada");

            // Validar se o profissional já é membro
            var jaEhMembro = comissao.Membros
                .Any(m => m.ProfissionalId == dto.ProfissionalId && m.Ativo);

            if (jaEhMembro)
                throw new Exception("Profissional já é membro desta comissão");

            // Validar limite de membros
            var membrosAtivos = comissao.Membros.Count(m => m.Ativo);
            if (membrosAtivos >= comissao.QuantidadeMembrosNecessarios)
                throw new Exception($"Comissão já possui o número máximo de membros ({comissao.QuantidadeMembrosNecessarios})");

            // Adicionar membro
            var membro = new MembroComissao
            {
                ComissaoId = comissaoId,
                ProfissionalId = dto.ProfissionalId,
                Tipo = dto.Tipo,
                IsCoordenador = dto.IsCoordenador,
                DataPosse = dto.DataPosse,
                Ativo = true,
                DataInclusao = DateTime.Now
            };

            // Se for coordenador, remover coordenador anterior
            if (dto.IsCoordenador)
            {
                var coordenadorAtual = comissao.Membros
                    .FirstOrDefault(m => m.IsCoordenador && m.Ativo);
                
                if (coordenadorAtual != null)
                {
                    coordenadorAtual.IsCoordenador = false;
                }
            }

            comissao.Membros.Add(membro);
            await _context.SaveChangesAsync();

            // Notificar novo membro
            await NotificarNovoMembroComissaoAsync(membro, comissao);

            _logger.LogInformation($"Membro {dto.ProfissionalId} adicionado à comissão {comissaoId}");

            return true;
        }

        /// <summary>
        /// Remove um membro da comissão
        /// </summary>
        public async Task<bool> RemoverMembroAsync(int comissaoId, int membroId)
        {
            var membro = await _context.MembrosComissao
                .FirstOrDefaultAsync(m => m.Id == membroId && m.ComissaoId == comissaoId);

            if (membro == null)
                throw new Exception("Membro não encontrado");

            membro.Ativo = false;
            membro.DataSaida = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Membro {membroId} removido da comissão {comissaoId}");

            return true;
        }

        /// <summary>
        /// Define o coordenador da comissão
        /// </summary>
        public async Task<bool> DefinirCoordenadorAsync(int comissaoId, int membroId)
        {
            var comissao = await _context.ComissoesEleitorais
                .Include(c => c.Membros)
                .FirstOrDefaultAsync(c => c.Id == comissaoId);

            if (comissao == null)
                throw new Exception("Comissão não encontrada");

            var membro = comissao.Membros
                .FirstOrDefault(m => m.Id == membroId && m.Ativo);

            if (membro == null)
                throw new Exception("Membro não encontrado ou inativo");

            // Remover coordenador anterior
            var coordenadorAtual = comissao.Membros
                .FirstOrDefault(m => m.IsCoordenador && m.Ativo);
            
            if (coordenadorAtual != null)
            {
                coordenadorAtual.IsCoordenador = false;
            }

            // Definir novo coordenador
            membro.IsCoordenador = true;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Membro {membroId} definido como coordenador da comissão {comissaoId}");

            return true;
        }

        /// <summary>
        /// Valida se a composição da comissão está completa
        /// </summary>
        public async Task<bool> ValidarComposicaoAsync(int comissaoId)
        {
            var comissao = await _context.ComissoesEleitorais
                .Include(c => c.Membros)
                .FirstOrDefaultAsync(c => c.Id == comissaoId);

            if (comissao == null)
                return false;

            var membrosAtivos = comissao.Membros.Count(m => m.Ativo);
            var temCoordenador = comissao.Membros.Any(m => m.IsCoordenador && m.Ativo);

            return membrosAtivos == comissao.QuantidadeMembrosNecessarios && temCoordenador;
        }

        /// <summary>
        /// Calcula a quantidade de membros necessários baseado no tipo de comissão
        /// </summary>
        public async Task<int> CalcularQuantidadeMembrosNecessariosAsync(TipoComissao tipo)
        {
            return tipo switch
            {
                TipoComissao.Nacional => MEMBROS_COMISSAO_NACIONAL,
                TipoComissao.Estadual => MEMBROS_COMISSAO_ESTADUAL,
                _ => MEMBROS_COMISSAO_ESTADUAL
            };
        }

        #region Métodos Auxiliares

        private async Task NotificarNovoMembroComissaoAsync(MembroComissao membro, ComissaoEleitoral comissao)
        {
            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == membro.ProfissionalId);

            await _notificationService.EnviarEmailAsync(new EmailModel
            {
                Para = new List<string> { profissional.Email },
                Assunto = "Nomeação para Comissão Eleitoral",
                TemplateId = "NomeacaoComissao",
                ParametrosTemplate = new Dictionary<string, string>
                {
                    ["NomeProfissional"] = profissional.Nome,
                    ["NomeComissao"] = comissao.Nome,
                    ["TipoMembro"] = membro.Tipo.ToString(),
                    ["DataPosse"] = membro.DataPosse.ToString("dd/MM/yyyy"),
                    ["IsCoordenador"] = membro.IsCoordenador ? "Sim" : "Não"
                }
            });
        }

        private ComissaoEleitoralDTO MapearParaDTO(ComissaoEleitoral comissao)
        {
            return new ComissaoEleitoralDTO
            {
                Id = comissao.Id,
                Nome = comissao.Nome,
                Tipo = comissao.Tipo,
                EleicaoId = comissao.EleicaoId,
                UfId = comissao.UfId,
                UfNome = comissao.Uf?.Nome,
                QuantidadeMembros = comissao.Membros.Count(m => m.Ativo),
                QuantidadeMembrosNecessarios = comissao.QuantidadeMembrosNecessarios,
                ComposicaoCompleta = comissao.Membros.Count(m => m.Ativo) == comissao.QuantidadeMembrosNecessarios,
                Membros = comissao.Membros
                    .Where(m => m.Ativo)
                    .Select(m => new MembroComissaoDTO
                    {
                        Id = m.Id,
                        ProfissionalId = m.ProfissionalId,
                        NomeProfissional = m.Profissional?.Nome,
                        CpfProfissional = m.Profissional?.Cpf,
                        Tipo = m.Tipo,
                        IsCoordenador = m.IsCoordenador,
                        DataPosse = m.DataPosse,
                        DataSaida = m.DataSaida,
                        MotivoSaida = m.MotivoSaida
                    }).ToList()
            };
        }

        #endregion
    }
}