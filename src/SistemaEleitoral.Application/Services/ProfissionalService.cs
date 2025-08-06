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
    /// Serviço responsável pela gestão de profissionais CAU
    /// </summary>
    public class ProfissionalService : IProfissionalService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProfissionalService> _logger;
        private readonly IValidacaoElegibilidadeService _validacaoService;
        private readonly ICorporativoService _corporativoService;

        public ProfissionalService(
            ApplicationDbContext context,
            ILogger<ProfissionalService> logger,
            IValidacaoElegibilidadeService validacaoService,
            ICorporativoService corporativoService)
        {
            _context = context;
            _logger = logger;
            _validacaoService = validacaoService;
            _corporativoService = corporativoService;
        }

        /// <summary>
        /// Busca profissional por registro CAU
        /// </summary>
        public async Task<Profissional> BuscarPorRegistroCAUAsync(string registroCAU)
        {
            _logger.LogInformation("Buscando profissional com registro CAU {RegistroCAU}", registroCAU);

            var profissional = await _context.Profissionais
                .Include(p => p.Uf)
                .FirstOrDefaultAsync(p => p.RegistroCAU == registroCAU && p.Ativo);

            if (profissional == null)
            {
                // Tentar buscar no sistema corporativo
                profissional = await BuscarNoCorporativoAsync(registroCAU);
                
                if (profissional != null)
                {
                    // Salvar no banco local para cache
                    _context.Profissionais.Add(profissional);
                    await _context.SaveChangesAsync();
                }
            }

            return profissional;
        }

        /// <summary>
        /// Busca profissional por CPF
        /// </summary>
        public async Task<Profissional> BuscarPorCPFAsync(string cpf)
        {
            _logger.LogInformation("Buscando profissional com CPF {CPF}", cpf);

            var profissional = await _context.Profissionais
                .Include(p => p.Uf)
                .FirstOrDefaultAsync(p => p.CPF == cpf && p.Ativo);

            if (profissional == null)
            {
                // Tentar buscar no sistema corporativo
                profissional = await _corporativoService.BuscarProfissionalPorCPFAsync(cpf);
                
                if (profissional != null)
                {
                    _context.Profissionais.Add(profissional);
                    await _context.SaveChangesAsync();
                }
            }

            return profissional;
        }

        /// <summary>
        /// Lista profissionais com filtros
        /// </summary>
        public async Task<List<Profissional>> ListarProfissionaisAsync(FiltroProfissionalDTO filtro)
        {
            var query = _context.Profissionais
                .Include(p => p.Uf)
                .Where(p => p.Ativo);

            if (!string.IsNullOrEmpty(filtro.Nome))
            {
                query = query.Where(p => p.NomeCompleto.Contains(filtro.Nome));
            }

            if (filtro.UfId.HasValue)
            {
                query = query.Where(p => p.UfId == filtro.UfId.Value);
            }

            if (filtro.StatusRegistro.HasValue)
            {
                query = query.Where(p => p.StatusRegistro == filtro.StatusRegistro.Value);
            }

            if (filtro.PodeVotar.HasValue)
            {
                query = query.Where(p => p.PodeVotar == filtro.PodeVotar.Value);
            }

            if (filtro.EhElegivel.HasValue)
            {
                query = query.Where(p => p.EhElegivel == filtro.EhElegivel.Value);
            }

            return await query
                .OrderBy(p => p.NomeCompleto)
                .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
                .Take(filtro.TamanhoPagina)
                .ToListAsync();
        }

        /// <summary>
        /// Atualiza dados do profissional
        /// </summary>
        public async Task<Profissional> AtualizarDadosAsync(int id, AtualizarProfissionalDTO dto)
        {
            _logger.LogInformation("Atualizando dados do profissional {Id}", id);

            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == id);

            if (profissional == null)
            {
                throw new InvalidOperationException($"Profissional {id} não encontrado");
            }

            // Atualizar dados permitidos
            profissional.Email = dto.Email ?? profissional.Email;
            profissional.Telefone = dto.Telefone ?? profissional.Telefone;
            profissional.Celular = dto.Celular ?? profissional.Celular;
            profissional.Endereco = dto.Endereco ?? profissional.Endereco;
            profissional.Cidade = dto.Cidade ?? profissional.Cidade;
            profissional.CEP = dto.CEP ?? profissional.CEP;
            profissional.DataAtualizacao = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Dados do profissional {Id} atualizados com sucesso", id);

            return profissional;
        }

        /// <summary>
        /// Verifica elegibilidade do profissional
        /// </summary>
        public async Task<ValidacaoElegibilidadeResultDTO> VerificarElegibilidadeAsync(int profissionalId, int calendarioId)
        {
            _logger.LogInformation("Verificando elegibilidade do profissional {ProfissionalId} para calendário {CalendarioId}", 
                profissionalId, calendarioId);

            var profissional = await _context.Profissionais
                .Include(p => p.Uf)
                .FirstOrDefaultAsync(p => p.Id == profissionalId);

            if (profissional == null)
            {
                throw new InvalidOperationException($"Profissional {profissionalId} não encontrado");
            }

            // Usar o serviço de validação de elegibilidade
            return await _validacaoService.ValidarElegibilidadeAsync(profissionalId, calendarioId);
        }

        /// <summary>
        /// Sincroniza dados com sistema corporativo
        /// </summary>
        public async Task<Profissional> SincronizarComCorporativoAsync(string registroCAU)
        {
            _logger.LogInformation("Sincronizando profissional {RegistroCAU} com sistema corporativo", registroCAU);

            var profissionalCorporativo = await _corporativoService.BuscarProfissionalPorRegistroAsync(registroCAU);
            
            if (profissionalCorporativo == null)
            {
                throw new InvalidOperationException($"Profissional {registroCAU} não encontrado no sistema corporativo");
            }

            var profissionalLocal = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.RegistroCAU == registroCAU);

            if (profissionalLocal == null)
            {
                // Criar novo
                profissionalLocal = profissionalCorporativo;
                _context.Profissionais.Add(profissionalLocal);
            }
            else
            {
                // Atualizar existente
                profissionalLocal.NomeCompleto = profissionalCorporativo.NomeCompleto;
                profissionalLocal.Email = profissionalCorporativo.Email;
                profissionalLocal.StatusRegistro = profissionalCorporativo.StatusRegistro;
                profissionalLocal.PodeVotar = profissionalCorporativo.PodeVotar;
                profissionalLocal.EhElegivel = profissionalCorporativo.EhElegivel;
                profissionalLocal.DataAtualizacao = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Profissional {RegistroCAU} sincronizado com sucesso", registroCAU);

            return profissionalLocal;
        }

        /// <summary>
        /// Obtém histórico do profissional
        /// </summary>
        public async Task<HistoricoProfissionalDTO> ObterHistoricoAsync(int profissionalId)
        {
            var profissional = await _context.Profissionais
                .Include(p => p.MembrosChapa)
                    .ThenInclude(m => m.Chapa)
                        .ThenInclude(c => c.Calendario)
                .Include(p => p.Denuncias)
                .Include(p => p.VotosEmitidos)
                .FirstOrDefaultAsync(p => p.Id == profissionalId);

            if (profissional == null)
            {
                throw new InvalidOperationException($"Profissional {profissionalId} não encontrado");
            }

            var historico = new HistoricoProfissionalDTO
            {
                ProfissionalId = profissionalId,
                NomeProfissional = profissional.NomeCompleto,
                RegistroCAU = profissional.RegistroCAU,
                ParticipacoesChapas = profissional.MembrosChapa.Select(m => new ParticipacaoChapaDTO
                {
                    ChapaId = m.ChapaId,
                    NomeChapa = m.Chapa.Nome,
                    Ano = m.Chapa.Calendario.Ano,
                    Cargo = m.Cargo,
                    Status = m.Status.ToString()
                }).ToList(),
                DenunciasRecebidas = profissional.Denuncias.Count(),
                EleicoesVotadas = profissional.VotosEmitidos.Select(v => v.SessaoVotacao.CalendarioId).Distinct().Count(),
                DataPrimeiraParticipacao = profissional.MembrosChapa.Min(m => m.DataCriacao),
                DataUltimaParticipacao = profissional.MembrosChapa.Max(m => m.DataCriacao)
            };

            return historico;
        }

        /// <summary>
        /// Obtém pendências do profissional
        /// </summary>
        public async Task<List<PendenciaProfissionalDTO>> ObterPendenciasAsync(int profissionalId)
        {
            var pendencias = new List<PendenciaProfissionalDTO>();

            // Verificar situação financeira
            var situacaoFinanceira = await _corporativoService.VerificarSituacaoFinanceiraAsync(profissionalId);
            if (!situacaoFinanceira.Regular)
            {
                pendencias.Add(new PendenciaProfissionalDTO
                {
                    Tipo = "Financeira",
                    Descricao = "Débitos pendentes com o CAU",
                    Impeditiva = true,
                    DataDeteccao = DateTime.Now
                });
            }

            // Verificar situação ética
            var situacaoEtica = await _corporativoService.VerificarSituacaoEticaAsync(profissionalId);
            if (!situacaoEtica.Regular)
            {
                pendencias.Add(new PendenciaProfissionalDTO
                {
                    Tipo = "Ética",
                    Descricao = situacaoEtica.Descricao,
                    Impeditiva = situacaoEtica.Impeditiva,
                    DataDeteccao = DateTime.Now
                });
            }

            // Verificar registro profissional
            var profissional = await _context.Profissionais.FindAsync(profissionalId);
            if (profissional?.StatusRegistro != StatusRegistroProfissional.Ativo)
            {
                pendencias.Add(new PendenciaProfissionalDTO
                {
                    Tipo = "Registro",
                    Descricao = "Registro profissional não está ativo",
                    Impeditiva = true,
                    DataDeteccao = DateTime.Now
                });
            }

            return pendencias;
        }

        #region Métodos Privados

        private async Task<Profissional> BuscarNoCorporativoAsync(string registroCAU)
        {
            try
            {
                return await _corporativoService.BuscarProfissionalPorRegistroAsync(registroCAU);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar profissional {RegistroCAU} no sistema corporativo", registroCAU);
                return null;
            }
        }

        #endregion
    }
}