using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de Calendário Eleitoral
    /// </summary>
    public interface ICalendarioService
    {
        // Consultas Básicas
        Task<List<int>> ObterAnosComCalendariosAsync();
        Task<List<int>> ObterAnosCalendariosConcluidosAsync();
        Task<CalendarioDTO> ObterPorIdAsync(int id);
        
        // Validações Temporais (CRÍTICO)
        Task<bool> ValidarPeriodoParaAcaoAsync(int calendarioId, TipoAtividadeCalendario tipoAtividade);
        Task<bool> ValidarPeriodoUFAsync(int calendarioId, string uf, TipoAtividadeCalendario tipoAtividade);
        Task<PeriodoCalendarioDTO> ObterPeriodoAtualAsync(int calendarioId);
        
        // Criação e Duplicação
        Task<CalendarioDTO> CriarCalendarioAsync(CriarCalendarioDTO dto);
        Task<CalendarioDTO> DuplicarCalendarioAsync(int calendarioOrigemId, int novoAno, int usuarioId);
        
        // Gestão de Estados
        Task<bool> AlterarSituacaoAsync(int calendarioId, SituacaoCalendario novaSituacao, int usuarioId);
    }
    
    /// <summary>
    /// DTO para informações do calendário
    /// </summary>
    public class CalendarioDTO
    {
        public int Id { get; set; }
        public int Ano { get; set; }
        public int EleicaoId { get; set; }
        public string EleicaoNome { get; set; }
        public string Situacao { get; set; }
        public string NumeroProcesso { get; set; }
        public string LinkResolucao { get; set; }
        public int ProgressoPercentual { get; set; }
        public List<string> Ufs { get; set; }
        public List<AtividadePrincipalDTO> AtividadesPrincipais { get; set; }
    }
    
    /// <summary>
    /// DTO para criação de calendário
    /// </summary>
    public class CriarCalendarioDTO
    {
        public int Ano { get; set; }
        public int EleicaoId { get; set; }
        public int TipoProcessoId { get; set; }
        public string NumeroProcesso { get; set; }
        public string LinkResolucao { get; set; }
        public List<int> Ufs { get; set; }
        public bool CriarEstruturaPadrao { get; set; }
        public int UsuarioCriacaoId { get; set; }
    }
    
    /// <summary>
    /// DTO para período do calendário
    /// </summary>
    public class PeriodoCalendarioDTO
    {
        public int CalendarioId { get; set; }
        public string AtividadeAtual { get; set; }
        public DateTime? DataInicioAtividade { get; set; }
        public DateTime? DataFimAtividade { get; set; }
        public int DiasRestantes { get; set; }
        public bool PermiteRegistroChapa { get; set; }
        public bool PermiteImpugnacao { get; set; }
        public bool PermiteVotacao { get; set; }
    }
    
    /// <summary>
    /// DTO para atividade principal
    /// </summary>
    public class AtividadePrincipalDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int Ordem { get; set; }
        public bool Ativo { get; set; }
    }
}