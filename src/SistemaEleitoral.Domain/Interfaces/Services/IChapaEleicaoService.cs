using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de Chapas Eleitorais
    /// </summary>
    public interface IChapaEleicaoService
    {
        // Registro e Criação
        Task<ChapaEleicaoDTO> RegistrarChapaAsync(RegistrarChapaDTO dto);
        
        // Gestão de Membros
        Task<bool> AdicionarMembroAsync(int chapaId, AdicionarMembroChapaDTO dto);
        Task<bool> ConfirmarParticipaçãoMembroAsync(int chapaId, int membroId, int profissionalId);
        Task<bool> RemoverMembroAsync(int chapaId, int membroId, int usuarioId);
        
        // Confirmação e Finalização
        Task<bool> ConfirmarChapaAsync(int chapaId, ConfirmarChapaDTO dto);
        Task<bool> CancelarChapaAsync(int chapaId, string motivo, int usuarioId);
        
        // Consultas
        Task<ChapaEleicaoDTO> ObterChapaPorIdAsync(int id);
        Task<ListaChapasPaginadaDTO> ListarChapasAsync(FiltroChapasDTO filtro);
        Task<List<ChapaEleicaoDTO>> ObterChapasPorCalendarioAsync(int calendarioId);
        Task<List<ChapaEleicaoDTO>> ObterChapasPorUFAsync(int ufId);
        
        // Validações
        Task<ValidacaoChapaResult> ValidarChapaAsync(int chapaId);
        Task<bool> VerificarProfissionalEmChapaAsync(int profissionalId, int calendarioId);
        
        // Documentação
        Task<bool> AnexarDocumentoAsync(int chapaId, AnexarDocumentoDTO dto);
        Task<bool> RemoverDocumentoAsync(int chapaId, int documentoId);
        
        // Estatísticas
        Task<EstatisticasChapaDTO> ObterEstatisticasAsync(int calendarioId);
    }
    
    /// <summary>
    /// DTO para informações da chapa eleitoral
    /// </summary>
    public class ChapaEleicaoDTO
    {
        public int Id { get; set; }
        public string NumeroChapa { get; set; }
        public string Nome { get; set; }
        public string Slogan { get; set; }
        public string Status { get; set; }
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public string UfNome { get; set; }
        public int ResponsavelId { get; set; }
        public DateTime DataRegistro { get; set; }
        public DateTime? DataConfirmacao { get; set; }
        public int QuantidadeMembros { get; set; }
        public int QuantidadeHomens { get; set; }
        public int QuantidadeMulheres { get; set; }
        public int QuantidadeNegros { get; set; }
        public int QuantidadePcD { get; set; }
        public int QuantidadeLGBTQI { get; set; }
        public List<MembroChapaDTO> Membros { get; set; }
        public List<DocumentoChapaDTO> Documentos { get; set; }
        public List<RedeSocialChapaDTO> RedesSociais { get; set; }
    }
    
    /// <summary>
    /// DTO para registrar nova chapa
    /// </summary>
    public class RegistrarChapaDTO
    {
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public int ResponsavelId { get; set; }
        public string NomeChapa { get; set; }
        public string Slogan { get; set; }
        public int UsuarioCriacaoId { get; set; }
    }
    
    /// <summary>
    /// DTO para adicionar membro à chapa
    /// </summary>
    public class AdicionarMembroChapaDTO
    {
        public int ProfissionalId { get; set; }
        public TipoParticipacaoMembro TipoParticipacao { get; set; }
        public string Cargo { get; set; }
        public int UsuarioAdicaoId { get; set; }
    }
    
    /// <summary>
    /// DTO para confirmar chapa
    /// </summary>
    public class ConfirmarChapaDTO
    {
        public int UsuarioConfirmacaoId { get; set; }
        public string IpConfirmacao { get; set; }
        public bool AceitaTermos { get; set; }
    }
    
    /// <summary>
    /// DTO para membro da chapa
    /// </summary>
    public class MembroChapaDTO
    {
        public int Id { get; set; }
        public int ProfissionalId { get; set; }
        public string NomeProfissional { get; set; }
        public string CpfProfissional { get; set; }
        public string RegistroProfissional { get; set; }
        public string TipoParticipacao { get; set; }
        public string Cargo { get; set; }
        public string Status { get; set; }
        public DateTime DataInclusao { get; set; }
        public DateTime? DataConfirmacao { get; set; }
        public bool IsResponsavel { get; set; }
        public string FotoProfissional { get; set; }
        public string CurriculoResumido { get; set; }
    }
    
    /// <summary>
    /// DTO para documento da chapa
    /// </summary>
    public class DocumentoChapaDTO
    {
        public int Id { get; set; }
        public string TipoDocumento { get; set; }
        public string NomeArquivo { get; set; }
        public string CaminhoArquivo { get; set; }
        public long TamanhoBytes { get; set; }
        public DateTime DataUpload { get; set; }
        public bool Ativo { get; set; }
    }
    
    /// <summary>
    /// DTO para rede social da chapa
    /// </summary>
    public class RedeSocialChapaDTO
    {
        public int Id { get; set; }
        public string TipoRede { get; set; }
        public string Url { get; set; }
        public string Usuario { get; set; }
        public bool Ativo { get; set; }
    }
    
    /// <summary>
    /// DTO para filtro de chapas
    /// </summary>
    public class FiltroChapasDTO
    {
        public int? CalendarioId { get; set; }
        public int? UfId { get; set; }
        public string Status { get; set; }
        public string TextoBusca { get; set; }
        public int Pagina { get; set; } = 1;
        public int ItensPorPagina { get; set; } = 20;
    }
    
    /// <summary>
    /// DTO para lista paginada de chapas
    /// </summary>
    public class ListaChapasPaginadaDTO
    {
        public List<ChapaEleicaoDTO> Chapas { get; set; }
        public int PaginaAtual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalItens { get; set; }
        public int ItensPorPagina { get; set; }
    }
    
    /// <summary>
    /// DTO para anexar documento
    /// </summary>
    public class AnexarDocumentoDTO
    {
        public string TipoDocumento { get; set; }
        public string NomeArquivo { get; set; }
        public byte[] ConteudoArquivo { get; set; }
        public int UsuarioUploadId { get; set; }
    }
    
    /// <summary>
    /// DTO para estatísticas de chapas
    /// </summary>
    public class EstatisticasChapaDTO
    {
        public int TotalChapas { get; set; }
        public int ChapasConfirmadas { get; set; }
        public int ChapasEmElaboracao { get; set; }
        public int ChapasImpugnadas { get; set; }
        public int TotalMembros { get; set; }
        public Dictionary<string, int> ChapasPorUF { get; set; }
        public DiversidadeEstatisticasDTO Diversidade { get; set; }
    }
    
    /// <summary>
    /// DTO para estatísticas de diversidade
    /// </summary>
    public class DiversidadeEstatisticasDTO
    {
        public int TotalHomens { get; set; }
        public int TotalMulheres { get; set; }
        public int TotalNegros { get; set; }
        public int TotalPcD { get; set; }
        public int TotalLGBTQI { get; set; }
        public double PercentualMulheres { get; set; }
        public double PercentualNegros { get; set; }
        public double PercentualPcD { get; set; }
        public double PercentualLGBTQI { get; set; }
    }
    
    /// <summary>
    /// Resultado da validação da chapa
    /// </summary>
    public class ValidacaoChapaResult
    {
        public bool IsValida { get; set; }
        public List<string> Erros { get; set; }
        public List<string> Avisos { get; set; }
    }
}