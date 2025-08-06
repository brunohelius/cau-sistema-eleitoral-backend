using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de gestão de atividades do calendário
    /// </summary>
    public interface IAtividadeCalendarioService
    {
        // Atividades Principais
        Task<AtividadePrincipalCalendario> CriarAtividadePrincipalAsync(CriarAtividadePrincipalDTO dto);
        Task<AtividadePrincipalCalendario> AtualizarAtividadePrincipalAsync(int id, AtualizarAtividadePrincipalDTO dto);
        Task<List<AtividadePrincipalCalendario>> ListarAtividadesPrincipaisAsync(int calendarioId, FiltroAtividadesDTO filtro);
        
        // Atividades Secundárias
        Task<AtividadeSecundariaCalendario> CriarAtividadeSecundariaAsync(CriarAtividadeSecundariaDTO dto);
        Task<AtividadeSecundariaCalendario> AtualizarAtividadeSecundariaAsync(int id, AtualizarAtividadeSecundariaDTO dto);
        
        // Validações e Prazos
        Task<ValidacaoPrazosDTO> ValidarPrazosAtividadesAsync(int calendarioId);
        Task<bool> ReordenarAtividadesAsync(ReordenarAtividadesDTO dto);
    }

    // DTOs para Atividades Principais
    public class CriarAtividadePrincipalDTO
    {
        public int CalendarioId { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public TipoAtividadeCalendario TipoAtividade { get; set; }
        public bool Obrigatoria { get; set; }
        public bool PermiteAlteracao { get; set; }
        public int UsuarioCriacaoId { get; set; }
    }

    public class AtualizarAtividadePrincipalDTO
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public bool? Obrigatoria { get; set; }
        public int UsuarioAtualizacaoId { get; set; }
    }

    // DTOs para Atividades Secundárias
    public class CriarAtividadeSecundariaDTO
    {
        public int AtividadePrincipalId { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public TipoAtividadeCalendario TipoAtividade { get; set; }
        public string? Responsavel { get; set; }
        public string? Local { get; set; }
        public int UsuarioCriacaoId { get; set; }
    }

    public class AtualizarAtividadeSecundariaDTO
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string? Responsavel { get; set; }
        public string? Local { get; set; }
        public int UsuarioAtualizacaoId { get; set; }
    }

    // DTOs de Filtro e Validação
    public class FiltroAtividadesDTO
    {
        public TipoAtividadeCalendario? TipoAtividade { get; set; }
        public bool? ApenasObrigatorias { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
    }

    public class ValidacaoPrazosDTO
    {
        public int CalendarioId { get; set; }
        public DateTime DataValidacao { get; set; }
        public List<AtividadeVencidaDTO> AtividadesVencidas { get; set; }
        public List<AtividadeProximaDTO> AtividadesProximas { get; set; }
        public List<ConflitoAtividadeDTO> Conflitos { get; set; }
        public int TotalAtividadesVencidas { get; set; }
        public int TotalAtividadesProximas { get; set; }
        public int TotalConflitos { get; set; }
        public string StatusGeral { get; set; }
    }

    public class AtividadeVencidaDTO
    {
        public int AtividadeId { get; set; }
        public string Nome { get; set; }
        public DateTime DataVencimento { get; set; }
        public int DiasAtraso { get; set; }
    }

    public class AtividadeProximaDTO
    {
        public int AtividadeId { get; set; }
        public string Nome { get; set; }
        public DateTime DataInicio { get; set; }
        public int DiasRestantes { get; set; }
    }

    public class ConflitoAtividadeDTO
    {
        public int AtividadeId1 { get; set; }
        public string NomeAtividade1 { get; set; }
        public int AtividadeId2 { get; set; }
        public string NomeAtividade2 { get; set; }
        public string TipoConflito { get; set; }
    }

    public class ReordenarAtividadesDTO
    {
        public int CalendarioId { get; set; }
        public List<OrdemAtividadeDTO> NovasOrdens { get; set; }
    }

    public class OrdemAtividadeDTO
    {
        public int AtividadeId { get; set; }
        public int NovaOrdem { get; set; }
    }
}