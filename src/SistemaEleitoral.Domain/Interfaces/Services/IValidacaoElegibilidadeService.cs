using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de validação de elegibilidade
    /// </summary>
    public interface IValidacaoElegibilidadeService
    {
        Task<ValidacaoElegibilidadeResult> ValidarElegibilidadeAsync(int profissionalId);
        Task<ValidacaoElegibilidadeResult> ValidarElegibilidadeCompletoAsync(int profissionalId, int eleicaoId);
        Task<bool> VerificarAdimplenciaAsync(int profissionalId);
        Task<bool> VerificarPenalizacoesEticasAsync(int profissionalId);
        Task<bool> VerificarRegistroAtivoAsync(int profissionalId);
        Task<bool> VerificarTempoRegistroAsync(int profissionalId, int anosMinimos);
        Task<List<RestricaoElegibilidade>> ObterRestricoesAsync(int profissionalId);
    }
    
    /// <summary>
    /// Resultado da validação de elegibilidade
    /// </summary>
    public class ValidacaoElegibilidadeResult
    {
        public bool IsElegivel { get; set; }
        public List<string> Restricoes { get; set; }
        public DateTime DataValidacao { get; set; }
        public Dictionary<string, bool> ValidacoesDetalhadas { get; set; }
        public string MensagemConsolidada { get; set; }
    }
    
    /// <summary>
    /// Restrição de elegibilidade
    /// </summary>
    public class RestricaoElegibilidade
    {
        public TipoRestricao Tipo { get; set; }
        public string Descricao { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public bool Impeditiva { get; set; }
        public string Observacao { get; set; }
    }
    
    /// <summary>
    /// Tipos de restrição de elegibilidade
    /// </summary>
    public enum TipoRestricao
    {
        Inadimplencia,
        PenalizacaoEtica,
        RegistroInativo,
        TempoRegistroInsuficiente,
        ProcessoJudicial,
        SuspensaoRegistro,
        CancelamentoRegistro,
        ImpedimentoLegal,
        ConflitoProfissional,
        Outro
    }
}