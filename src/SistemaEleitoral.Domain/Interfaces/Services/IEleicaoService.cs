using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de gestão de eleições
    /// </summary>
    public interface IEleicaoService
    {
        /// <summary>
        /// Cria uma nova eleição
        /// </summary>
        Task<Eleicao> CriarEleicaoAsync(CriarEleicaoDTO dto);

        /// <summary>
        /// Atualiza uma eleição existente
        /// </summary>
        Task<Eleicao> AtualizarEleicaoAsync(int id, AtualizarEleicaoDTO dto);

        /// <summary>
        /// Obtém eleição por ID
        /// </summary>
        Task<Eleicao> ObterEleicaoPorIdAsync(int id);

        /// <summary>
        /// Lista todas as eleições
        /// </summary>
        Task<List<Eleicao>> ListarEleicoesAsync(FiltroEleicoesDTO filtro);

        /// <summary>
        /// Obtém anos com eleições
        /// </summary>
        Task<List<int>> ObterAnosComEleicoesAsync();

        /// <summary>
        /// Altera situação da eleição
        /// </summary>
        Task<Eleicao> AlterarSituacaoEleicaoAsync(int id, AlterarSituacaoEleicaoDTO dto);

        /// <summary>
        /// Exclui logicamente uma eleição
        /// </summary>
        Task<bool> ExcluirEleicaoAsync(int id, int usuarioExclusaoId);

        /// <summary>
        /// Obtém estatísticas da eleição
        /// </summary>
        Task<EstatisticasEleicaoDTO> ObterEstatisticasEleicaoAsync(int eleicaoId);

        /// <summary>
        /// Obtém tipos de processo disponíveis
        /// </summary>
        Task<List<TipoProcesso>> ObterTiposProcessoAsync();
    }

    // DTOs para o serviço de eleições
    public class CriarEleicaoDTO
    {
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public int Ano { get; set; }
        public int TipoProcessoId { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public int UsuarioCriacaoId { get; set; }
    }

    public class AtualizarEleicaoDTO
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public int UsuarioAtualizacaoId { get; set; }
    }

    public class FiltroEleicoesDTO
    {
        public int? Ano { get; set; }
        public Enums.SituacaoEleicao? Situacao { get; set; }
        public string? Nome { get; set; }
    }

    public class AlterarSituacaoEleicaoDTO
    {
        public Enums.SituacaoEleicao NovaSituacao { get; set; }
        public string Motivo { get; set; }
        public int UsuarioAlteracaoId { get; set; }
    }

    public class EstatisticasEleicaoDTO
    {
        public int EleicaoId { get; set; }
        public string NomeEleicao { get; set; }
        public int Ano { get; set; }
        public int TotalCalendarios { get; set; }
        public int TotalChapas { get; set; }
        public int TotalChapasConfirmadas { get; set; }
        public int TotalDenuncias { get; set; }
        public int TotalImpugnacoes { get; set; }
        public int TotalEleitores { get; set; }
        public string Situacao { get; set; }
    }
}