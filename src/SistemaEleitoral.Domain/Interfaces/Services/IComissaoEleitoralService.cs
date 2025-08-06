using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de Comissão Eleitoral
    /// </summary>
    public interface IComissaoEleitoralService
    {
        Task<bool> ExisteComissaoParaUFAsync(int ufId, int eleicaoId);
        Task<ComissaoEleitoralDTO> ObterComissaoPorUFAsync(int ufId, int eleicaoId);
        Task<List<ComissaoEleitoralDTO>> ObterComissoesPorEleicaoAsync(int eleicaoId);
        Task<ComissaoEleitoralDTO> CriarComissaoAsync(CriarComissaoDTO dto);
        Task<bool> AdicionarMembroAsync(int comissaoId, AdicionarMembroComissaoEleitoralDTO dto);
        Task<bool> RemoverMembroAsync(int comissaoId, int membroId);
        Task<bool> DefinirCoordenadorAsync(int comissaoId, int membroId);
        Task<bool> ValidarComposicaoAsync(int comissaoId);
        Task<int> CalcularQuantidadeMembrosNecessariosAsync(TipoComissao tipo);
    }
    
    public class ComissaoEleitoralDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public TipoComissao Tipo { get; set; }
        public int EleicaoId { get; set; }
        public int? UfId { get; set; }
        public string UfNome { get; set; }
        public int QuantidadeMembros { get; set; }
        public int QuantidadeMembrosNecessarios { get; set; }
        public bool ComposicaoCompleta { get; set; }
        public List<MembroComissaoEleitoralDTO> Membros { get; set; }
    }
    
    public class CriarComissaoDTO
    {
        public string Nome { get; set; }
        public TipoComissao Tipo { get; set; }
        public int EleicaoId { get; set; }
        public int? UfId { get; set; }
        public int UsuarioCriacaoId { get; set; }
    }
    
    public class AdicionarMembroComissaoEleitoralDTO
    {
        public int ProfissionalId { get; set; }
        public TipoMembroComissaoEleitoral Tipo { get; set; }
        public bool IsCoordenador { get; set; }
        public DateTime DataPosse { get; set; }
        public int UsuarioAdicaoId { get; set; }
    }
    
    public class MembroComissaoEleitoralDTO
    {
        public int Id { get; set; }
        public int ProfissionalId { get; set; }
        public string NomeProfissional { get; set; }
        public string CpfProfissional { get; set; }
        public TipoMembroComissaoEleitoral Tipo { get; set; }
        public bool IsCoordenador { get; set; }
        public DateTime DataPosse { get; set; }
        public DateTime? DataSaida { get; set; }
        public string MotivoSaida { get; set; }
    }
    
    public enum TipoComissao
    {
        Nacional,
        Estadual
    }
    
    public enum TipoMembroComissao
    {
        Titular,
        Suplente
    }
}