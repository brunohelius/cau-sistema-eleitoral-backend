using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de membros de chapa
    /// </summary>
    public interface IMembroChapaService
    {
        Task<MembroChapaDetalhadoDTO> ObterMembroAsync(int membroId);
        Task<List<MembroChapaDetalhadoDTO>> ObterMembrosPorChapaAsync(int chapaId);
        Task<bool> ConvidarMembroAsync(ConviteMembroDTO dto);
        Task<bool> ConfirmarConviteAsync(int membroId, int profissionalId, string tokenConfirmacao);
        Task<bool> RecusarConviteAsync(int membroId, int profissionalId, string motivo);
        Task<bool> SubstituirMembroAsync(SubstituirMembroDTO dto);
        Task<bool> AlterarCargoMembroAsync(int membroId, string novoCargo, int usuarioId);
        Task<bool> AlterarTipoParticipacaoAsync(int membroId, TipoParticipacaoMembro novoTipo, int usuarioId);
        Task<ValidacaoMembroResult> ValidarMembroAsync(int profissionalId, int chapaId);
        Task<bool> VerificarPendenciasMembroAsync(int membroId);
        Task<List<PendenciaMembroDTO>> ObterPendenciasMembroAsync(int membroId);
    }
    
    /// <summary>
    /// DTO detalhado do membro da chapa
    /// </summary>
    public class MembroChapaDetalhadoDTO
    {
        public int Id { get; set; }
        public int ChapaId { get; set; }
        public int ProfissionalId { get; set; }
        public ProfissionalResumoDTO Profissional { get; set; }
        public TipoParticipacaoMembro TipoParticipacao { get; set; }
        public string Cargo { get; set; }
        public StatusMembroChapa Status { get; set; }
        public DateTime DataInclusao { get; set; }
        public DateTime? DataConfirmacao { get; set; }
        public DateTime? DataSaida { get; set; }
        public string MotivoSaida { get; set; }
        public bool IsResponsavel { get; set; }
        public string CurriculoResumido { get; set; }
        public string PropostasIndividuais { get; set; }
        public List<PendenciaMembroDTO> Pendencias { get; set; }
        public ValidacaoElegibilidadeResult Elegibilidade { get; set; }
    }
    
    /// <summary>
    /// DTO resumo do profissional
    /// </summary>
    public class ProfissionalResumoDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Cpf { get; set; }
        public string RegistroProfissional { get; set; }
        public string Email { get; set; }
        public string Telefone { get; set; }
        public string UfRegistro { get; set; }
        public string FotoUrl { get; set; }
        public string Genero { get; set; }
        public string Etnia { get; set; }
        public bool IsPcD { get; set; }
        public bool IsLGBTQI { get; set; }
        public DateTime DataRegistro { get; set; }
    }
    
    /// <summary>
    /// DTO para convite de membro
    /// </summary>
    public class ConviteMembroDTO
    {
        public int ChapaId { get; set; }
        public int ProfissionalId { get; set; }
        public TipoParticipacaoMembro TipoParticipacao { get; set; }
        public string Cargo { get; set; }
        public string MensagemConvite { get; set; }
        public int UsuarioConviteId { get; set; }
    }
    
    /// <summary>
    /// DTO para substituição de membro
    /// </summary>
    public class SubstituirMembroDTO
    {
        public int MembroAtualId { get; set; }
        public int NovoProfissionalId { get; set; }
        public string MotivoSubstituicao { get; set; }
        public bool ManterCargo { get; set; }
        public bool ManterTipoParticipacao { get; set; }
        public int UsuarioSubstituicaoId { get; set; }
    }
    
    /// <summary>
    /// Resultado da validação do membro
    /// </summary>
    public class ValidacaoMembroResult
    {
        public bool IsValido { get; set; }
        public bool IsElegivel { get; set; }
        public bool JaEstaNaChapa { get; set; }
        public bool JaEstaEmOutraChapa { get; set; }
        public List<string> Restricoes { get; set; }
        public string MensagemValidacao { get; set; }
    }
    
    /// <summary>
    /// DTO para pendência do membro
    /// </summary>
    public class PendenciaMembroDTO
    {
        public int Id { get; set; }
        public TipoPendenciaMembro Tipo { get; set; }
        public string Descricao { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataResolucao { get; set; }
        public bool Resolvida { get; set; }
        public bool Impeditiva { get; set; }
        public string Observacao { get; set; }
    }
    
    /// <summary>
    /// Tipos de participação do membro
    /// </summary>
    public enum TipoParticipacaoMembro
    {
        Titular,
        Suplente
    }
    
    /// <summary>
    /// Tipos de pendência do membro
    /// </summary>
    public enum TipoPendenciaMembro
    {
        DocumentacaoPendente,
        AssinaturaPendente,
        DadosIncompletos,
        ValidacaoElegibilidade,
        ConfirmacaoEmail,
        AceiteTermos,
        FotoPerfil,
        CurriculoResumido,
        DeclaracaoElegibilidade,
        Outro
    }
}