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
    /// Service responsável por validar elegibilidade de profissionais
    /// </summary>
    public class ValidacaoElegibilidadeService : IValidacaoElegibilidadeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ValidacaoElegibilidadeService> _logger;
        private readonly ICorporativoService _corporativoService;
        
        // Constantes de validação
        private const int ANOS_MINIMOS_REGISTRO = 2;
        private const decimal VALOR_MAXIMO_DEBITO = 0.01M;
        
        public ValidacaoElegibilidadeService(
            ApplicationDbContext context,
            ILogger<ValidacaoElegibilidadeService> logger,
            ICorporativoService corporativoService)
        {
            _context = context;
            _logger = logger;
            _corporativoService = corporativoService;
        }

        /// <summary>
        /// Valida elegibilidade básica de um profissional
        /// </summary>
        public async Task<ValidacaoElegibilidadeResult> ValidarElegibilidadeAsync(int profissionalId)
        {
            var resultado = new ValidacaoElegibilidadeResult
            {
                IsElegivel = true,
                Restricoes = new List<string>(),
                DataValidacao = DateTime.Now,
                ValidacoesDetalhadas = new Dictionary<string, bool>()
            };

            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == profissionalId);

            if (profissional == null)
            {
                resultado.IsElegivel = false;
                resultado.Restricoes.Add("Profissional não encontrado");
                resultado.MensagemConsolidada = "Profissional não encontrado no sistema";
                return resultado;
            }

            // Verificar registro ativo
            var registroAtivo = await VerificarRegistroAtivoAsync(profissionalId);
            resultado.ValidacoesDetalhadas["RegistroAtivo"] = registroAtivo;
            if (!registroAtivo)
            {
                resultado.IsElegivel = false;
                resultado.Restricoes.Add("Registro profissional inativo ou cancelado");
            }

            // Verificar adimplência
            var adimplente = await VerificarAdimplenciaAsync(profissionalId);
            resultado.ValidacoesDetalhadas["Adimplente"] = adimplente;
            if (!adimplente)
            {
                resultado.IsElegivel = false;
                resultado.Restricoes.Add("Profissional inadimplente com o CAU");
            }

            // Verificar penalizações éticas
            var semPenalizacoes = await VerificarPenalizacoesEticasAsync(profissionalId);
            resultado.ValidacoesDetalhadas["SemPenalizacoesEticas"] = semPenalizacoes;
            if (!semPenalizacoes)
            {
                resultado.IsElegivel = false;
                resultado.Restricoes.Add("Profissional com penalizações éticas impeditivas");
            }

            // Verificar tempo de registro
            var tempoRegistroValido = await VerificarTempoRegistroAsync(profissionalId, ANOS_MINIMOS_REGISTRO);
            resultado.ValidacoesDetalhadas["TempoRegistroMinimo"] = tempoRegistroValido;
            if (!tempoRegistroValido)
            {
                resultado.IsElegivel = false;
                resultado.Restricoes.Add($"Tempo de registro inferior a {ANOS_MINIMOS_REGISTRO} anos");
            }

            // Consolidar mensagem
            if (resultado.IsElegivel)
            {
                resultado.MensagemConsolidada = "Profissional elegível para participar de chapas eleitorais";
            }
            else
            {
                resultado.MensagemConsolidada = $"Profissional inelegível: {string.Join("; ", resultado.Restricoes)}";
            }

            _logger.LogInformation($"Validação de elegibilidade para profissional {profissionalId}: {resultado.IsElegivel}");

            return resultado;
        }

        /// <summary>
        /// Valida elegibilidade completa com regras específicas da eleição
        /// </summary>
        public async Task<ValidacaoElegibilidadeResult> ValidarElegibilidadeCompletoAsync(int profissionalId, int eleicaoId)
        {
            // Começar com validação básica
            var resultado = await ValidarElegibilidadeAsync(profissionalId);

            // Adicionar validações específicas da eleição
            var eleicao = await _context.Eleicoes
                .Include(e => e.ParametrosConselheiro)
                .FirstOrDefaultAsync(e => e.Id == eleicaoId);

            if (eleicao == null)
            {
                resultado.IsElegivel = false;
                resultado.Restricoes.Add("Eleição não encontrada");
                return resultado;
            }

            // Verificar se já foi eleito recentemente (impedimento de reeleição)
            if (eleicao.ParametrosConselheiro?.ImpedirReeleicao == true)
            {
                var foiEleitoRecentemente = await VerificarReeleicaoAsync(profissionalId, eleicaoId);
                resultado.ValidacoesDetalhadas["PodeReeleger"] = !foiEleitoRecentemente;
                
                if (foiEleitoRecentemente)
                {
                    resultado.IsElegivel = false;
                    resultado.Restricoes.Add("Profissional impedido de reeleição conforme regras da eleição");
                }
            }

            // Verificar idade mínima se aplicável
            if (eleicao.ParametrosConselheiro?.IdadeMinima > 0)
            {
                var profissional = await _context.Profissionais
                    .FirstOrDefaultAsync(p => p.Id == profissionalId);

                if (profissional.DataNascimento.HasValue)
                {
                    var idade = DateTime.Now.Year - profissional.DataNascimento.Value.Year;
                    if (DateTime.Now.DayOfYear < profissional.DataNascimento.Value.DayOfYear)
                        idade--;

                    resultado.ValidacoesDetalhadas["IdadeMinima"] = idade >= eleicao.ParametrosConselheiro.IdadeMinima;
                    
                    if (idade < eleicao.ParametrosConselheiro.IdadeMinima)
                    {
                        resultado.IsElegivel = false;
                        resultado.Restricoes.Add($"Idade inferior à mínima exigida ({eleicao.ParametrosConselheiro.IdadeMinima} anos)");
                    }
                }
            }

            // Verificar conflitos de interesse
            var temConflito = await VerificarConflitoInteresseAsync(profissionalId, eleicaoId);
            resultado.ValidacoesDetalhadas["SemConflitoInteresse"] = !temConflito;
            
            if (temConflito)
            {
                resultado.IsElegivel = false;
                resultado.Restricoes.Add("Profissional possui conflito de interesse para esta eleição");
            }

            // Atualizar mensagem consolidada
            if (!resultado.IsElegivel)
            {
                resultado.MensagemConsolidada = $"Profissional inelegível para eleição {eleicao.Nome}: {string.Join("; ", resultado.Restricoes)}";
            }

            return resultado;
        }

        /// <summary>
        /// Verifica adimplência do profissional
        /// </summary>
        public async Task<bool> VerificarAdimplenciaAsync(int profissionalId)
        {
            try
            {
                // Consultar sistema corporativo para verificar adimplência
                var situacaoFinanceira = await _corporativoService.ConsultarSituacaoFinanceiraAsync(profissionalId);
                
                if (situacaoFinanceira == null)
                {
                    _logger.LogWarning($"Não foi possível consultar situação financeira do profissional {profissionalId}");
                    return false;
                }

                // Verificar se tem débitos
                if (situacaoFinanceira.ValorDebito > VALOR_MAXIMO_DEBITO)
                {
                    _logger.LogInformation($"Profissional {profissionalId} possui débito de {situacaoFinanceira.ValorDebito:C}");
                    return false;
                }

                // Verificar se tem parcelamentos em atraso
                if (situacaoFinanceira.TemParcelamentoAtraso)
                {
                    _logger.LogInformation($"Profissional {profissionalId} possui parcelamento em atraso");
                    return false;
                }

                return situacaoFinanceira.Adimplente;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar adimplência do profissional {profissionalId}");
                return false;
            }
        }

        /// <summary>
        /// Verifica penalizações éticas do profissional
        /// </summary>
        public async Task<bool> VerificarPenalizacoesEticasAsync(int profissionalId)
        {
            // Verificar penalizações no banco local
            var penalizacoes = await _context.PenalizacoesEticas
                .Where(p => 
                    p.ProfissionalId == profissionalId &&
                    p.Ativa &&
                    (p.TipoPenalizacao == TipoPenalizacao.Suspensao ||
                     p.TipoPenalizacao == TipoPenalizacao.Cassacao ||
                     p.TipoPenalizacao == TipoPenalizacao.InabilitacaoTemporaria))
                .ToListAsync();

            if (penalizacoes.Any())
            {
                _logger.LogInformation($"Profissional {profissionalId} possui {penalizacoes.Count} penalizações éticas impeditivas");
                return false;
            }

            // Verificar se penalizações temporárias ainda estão vigentes
            var penalizacoesTemporarias = await _context.PenalizacoesEticas
                .Where(p => 
                    p.ProfissionalId == profissionalId &&
                    p.TipoPenalizacao == TipoPenalizacao.InabilitacaoTemporaria &&
                    p.DataFim > DateTime.Now)
                .AnyAsync();

            if (penalizacoesTemporarias)
            {
                _logger.LogInformation($"Profissional {profissionalId} possui inabilitação temporária vigente");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifica se o registro profissional está ativo
        /// </summary>
        public async Task<bool> VerificarRegistroAtivoAsync(int profissionalId)
        {
            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == profissionalId);

            if (profissional == null)
                return false;

            // Verificar status do registro
            if (profissional.StatusRegistro != StatusRegistroProfissional.Ativo)
            {
                _logger.LogInformation($"Profissional {profissionalId} com registro {profissional.StatusRegistro}");
                return false;
            }

            // Verificar se registro não está vencido
            if (profissional.DataValidadeRegistro.HasValue && profissional.DataValidadeRegistro.Value < DateTime.Now)
            {
                _logger.LogInformation($"Registro do profissional {profissionalId} vencido em {profissional.DataValidadeRegistro:dd/MM/yyyy}");
                return false;
            }

            // Consultar sistema corporativo para confirmação
            try
            {
                var registroAtivo = await _corporativoService.VerificarRegistroAtivoAsync(profissionalId);
                return registroAtivo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar registro do profissional {profissionalId} no sistema corporativo");
                // Em caso de erro, confiar no banco local
                return true;
            }
        }

        /// <summary>
        /// Verifica tempo de registro do profissional
        /// </summary>
        public async Task<bool> VerificarTempoRegistroAsync(int profissionalId, int anosMinimos)
        {
            var profissional = await _context.Profissionais
                .FirstOrDefaultAsync(p => p.Id == profissionalId);

            if (profissional == null || !profissional.DataRegistro.HasValue)
                return false;

            var tempoRegistro = DateTime.Now.Subtract(profissional.DataRegistro.Value).TotalDays / 365.25;
            
            var temTempoMinimo = tempoRegistro >= anosMinimos;
            
            if (!temTempoMinimo)
            {
                _logger.LogInformation($"Profissional {profissionalId} com {tempoRegistro:F1} anos de registro (mínimo: {anosMinimos})");
            }

            return temTempoMinimo;
        }

        /// <summary>
        /// Obtém todas as restrições de elegibilidade
        /// </summary>
        public async Task<List<RestricaoElegibilidade>> ObterRestricoesAsync(int profissionalId)
        {
            var restricoes = new List<RestricaoElegibilidade>();

            // Verificar adimplência
            if (!await VerificarAdimplenciaAsync(profissionalId))
            {
                restricoes.Add(new RestricaoElegibilidade
                {
                    Tipo = TipoRestricao.Inadimplencia,
                    Descricao = "Profissional inadimplente com o CAU",
                    Impeditiva = true,
                    DataInicio = DateTime.Now
                });
            }

            // Verificar penalizações éticas
            var penalizacoes = await _context.PenalizacoesEticas
                .Where(p => p.ProfissionalId == profissionalId && p.Ativa)
                .ToListAsync();

            foreach (var penalizacao in penalizacoes)
            {
                restricoes.Add(new RestricaoElegibilidade
                {
                    Tipo = TipoRestricao.PenalizacaoEtica,
                    Descricao = $"Penalização ética: {penalizacao.Descricao}",
                    DataInicio = penalizacao.DataInicio,
                    DataFim = penalizacao.DataFim,
                    Impeditiva = penalizacao.TipoPenalizacao != TipoPenalizacao.Advertencia,
                    Observacao = penalizacao.NumeroProcesso
                });
            }

            // Verificar registro
            if (!await VerificarRegistroAtivoAsync(profissionalId))
            {
                restricoes.Add(new RestricaoElegibilidade
                {
                    Tipo = TipoRestricao.RegistroInativo,
                    Descricao = "Registro profissional inativo ou cancelado",
                    Impeditiva = true,
                    DataInicio = DateTime.Now
                });
            }

            // Verificar tempo de registro
            if (!await VerificarTempoRegistroAsync(profissionalId, ANOS_MINIMOS_REGISTRO))
            {
                restricoes.Add(new RestricaoElegibilidade
                {
                    Tipo = TipoRestricao.TempoRegistroInsuficiente,
                    Descricao = $"Tempo de registro inferior a {ANOS_MINIMOS_REGISTRO} anos",
                    Impeditiva = true,
                    DataInicio = DateTime.Now
                });
            }

            // Verificar processos judiciais
            var processosJudiciais = await _context.ProcessosJudiciais
                .Where(p => 
                    p.ProfissionalId == profissionalId &&
                    p.Status == StatusProcessoJudicial.EmAndamento &&
                    p.TipoProcesso == TipoProcessoJudicial.Criminal)
                .ToListAsync();

            foreach (var processo in processosJudiciais)
            {
                restricoes.Add(new RestricaoElegibilidade
                {
                    Tipo = TipoRestricao.ProcessoJudicial,
                    Descricao = $"Processo judicial criminal em andamento: {processo.NumeroProcesso}",
                    Impeditiva = true,
                    DataInicio = processo.DataInicio,
                    Observacao = processo.Descricao
                });
            }

            return restricoes;
        }

        #region Métodos Auxiliares Privados

        private async Task<bool> VerificarReeleicaoAsync(int profissionalId, int eleicaoId)
        {
            var eleicao = await _context.Eleicoes
                .Include(e => e.ParametrosConselheiro)
                .FirstOrDefaultAsync(e => e.Id == eleicaoId);

            if (eleicao?.ParametrosConselheiro == null)
                return false;

            var anosImpedimento = eleicao.ParametrosConselheiro.AnosImpedimentoReeleicao ?? 4;
            var dataLimite = DateTime.Now.AddYears(-anosImpedimento);

            // Verificar se foi eleito no período de impedimento
            var foiEleito = await _context.MembrosChapa
                .Include(m => m.Chapa)
                    .ThenInclude(c => c.Calendario)
                .AnyAsync(m => 
                    m.ProfissionalId == profissionalId &&
                    m.Chapa.Status == StatusChapa.Eleita &&
                    m.Chapa.Calendario.DataCriacao > dataLimite);

            return foiEleito;
        }

        private async Task<bool> VerificarConflitoInteresseAsync(int profissionalId, int eleicaoId)
        {
            // Verificar se é funcionário do CAU
            var funcionarioCAU = await _context.FuncionariosCAU
                .AnyAsync(f => f.ProfissionalId == profissionalId && f.Ativo);

            if (funcionarioCAU)
            {
                _logger.LogInformation($"Profissional {profissionalId} é funcionário do CAU - conflito de interesse");
                return true;
            }

            // Verificar se tem contratos ativos com o CAU
            var temContrato = await _context.ContratosCAU
                .AnyAsync(c => 
                    c.ProfissionalId == profissionalId &&
                    c.Status == StatusContrato.Ativo &&
                    c.DataFim > DateTime.Now);

            if (temContrato)
            {
                _logger.LogInformation($"Profissional {profissionalId} possui contrato ativo com o CAU - conflito de interesse");
                return true;
            }

            // Verificar parentesco com membros da comissão eleitoral
            var temParentesco = await VerificarParentescoComissaoAsync(profissionalId, eleicaoId);
            
            if (temParentesco)
            {
                _logger.LogInformation($"Profissional {profissionalId} possui parentesco com membro da comissão - conflito de interesse");
                return true;
            }

            return false;
        }

        private async Task<bool> VerificarParentescoComissaoAsync(int profissionalId, int eleicaoId)
        {
            // Implementação simplificada - em produção, verificaria relações de parentesco
            // cadastradas no sistema
            return false;
        }

        #endregion
    }
}