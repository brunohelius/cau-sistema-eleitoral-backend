using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaEleitoral.Domain.Entities;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Interfaces.Services
{
    /// <summary>
    /// Interface para o serviço de Votação
    /// </summary>
    public interface IVotacaoService
    {
        // Abertura e Fechamento
        Task<bool> AbrirVotacaoAsync(AbrirVotacaoDTO dto);
        Task<bool> FecharVotacaoAsync(FecharVotacaoDTO dto);
        
        // Registro de Votos
        Task<ComprovanteVotoDTO> RegistrarVotoAsync(RegistrarVotoDTO dto);
        
        // Apuração
        Task<ResultadoApuracaoDTO> IniciarApuracaoAsync(int sessaoVotacaoId);
        Task<BoletimUrnaDTO> GerarBoletimUrnaAsync(int resultadoApuracaoId);
        
        // Consultas
        Task<StatusVotacaoDTO> ObterStatusVotacaoAsync(int calendarioId, int ufId);
        Task<bool> VerificarComprovanteAsync(string protocolo, string codigoVerificacao);
        Task<bool> VerificarSeEleitorVotouAsync(int eleitorId, int sessaoVotacaoId);
        
        // Estatísticas
        Task<EstatisticasVotacaoDTO> ObterEstatisticasAsync(int sessaoVotacaoId);
        Task<ParticipacaoVotacaoDTO> ObterParticipacaoAsync(int calendarioId);
        
        // Segundo Turno
        Task<bool> ConfigurarSegundoTurnoAsync(ConfigurarSegundoTurnoDTO dto);
    }
    
    /// <summary>
    /// DTO para abrir votação
    /// </summary>
    public class AbrirVotacaoDTO
    {
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public string UfSigla { get; set; }
        public int Turno { get; set; }
        public DateTime? DataHoraAberturaProgramada { get; set; }
        public int UsuarioAberturaId { get; set; }
    }
    
    /// <summary>
    /// DTO para fechar votação
    /// </summary>
    public class FecharVotacaoDTO
    {
        public int CalendarioId { get; set; }
        public int UfId { get; set; }
        public bool IniciarApuracao { get; set; }
        public int UsuarioFechamentoId { get; set; }
    }
    
    /// <summary>
    /// DTO para registrar voto
    /// </summary>
    public class RegistrarVotoDTO
    {
        public int SessaoVotacaoId { get; set; }
        public int EleitorId { get; set; }
        public int? ChapaId { get; set; }
        public TipoVoto TipoVoto { get; set; }
        public string IpOrigem { get; set; }
        public string UserAgent { get; set; }
    }
    
    /// <summary>
    /// DTO para comprovante de voto
    /// </summary>
    public class ComprovanteVotoDTO
    {
        public string ProtocoloComprovante { get; set; }
        public DateTime DataHoraVoto { get; set; }
        public string HashComprovante { get; set; }
        public string CodigoVerificacao { get; set; }
        public string MensagemConfirmacao { get; set; }
        public string QRCode { get; set; }
    }
    
    /// <summary>
    /// DTO para status da votação
    /// </summary>
    public class StatusVotacaoDTO
    {
        public int? SessaoVotacaoId { get; set; }
        public string Status { get; set; }
        public DateTime? DataAbertura { get; set; }
        public DateTime? DataFechamento { get; set; }
        public bool PodeVotar { get; set; }
        public int TotalEleitores { get; set; }
        public int TotalVotantes { get; set; }
        public double PercentualParticipacao { get; set; }
        public string ProximaEtapa { get; set; }
    }
    
    /// <summary>
    /// DTO para resultado de apuração
    /// </summary>
    public class ResultadoApuracaoDTO
    {
        public int Id { get; set; }
        public int SessaoVotacaoId { get; set; }
        public DateTime DataApuracao { get; set; }
        public int TotalVotosValidos { get; set; }
        public int TotalVotosBrancos { get; set; }
        public int TotalVotosNulos { get; set; }
        public int TotalGeralVotos { get; set; }
        public bool PrecisaSegundoTurno { get; set; }
        public int? ChapaVencedoraId { get; set; }
        public string NomeChapaVencedora { get; set; }
        public List<ResultadoChapaDTO> ResultadosChapas { get; set; }
        public string HashResultado { get; set; }
    }
    
    /// <summary>
    /// DTO para resultado de chapa
    /// </summary>
    public class ResultadoChapaDTO
    {
        public int ChapaId { get; set; }
        public string NumeroChapa { get; set; }
        public string NomeChapa { get; set; }
        public int TotalVotos { get; set; }
        public double PercentualVotos { get; set; }
        public int Posicao { get; set; }
        public bool ClassificadaSegundoTurno { get; set; }
    }
    
    /// <summary>
    /// DTO para estatísticas de votação
    /// </summary>
    public class EstatisticasVotacaoDTO
    {
        public int SessaoVotacaoId { get; set; }
        public int TotalEleitores { get; set; }
        public int TotalVotantes { get; set; }
        public double PercentualParticipacao { get; set; }
        public List<VotosPorHoraDTO> VotosPorHora { get; set; }
        public List<VotosPorUfDTO> VotosPorUf { get; set; }
        public List<ChapaVotacaoDTO> ChapasVotacao { get; set; }
        public DateTime? UltimaAtualizacao { get; set; }
    }
    
    /// <summary>
    /// DTO para votos por hora
    /// </summary>
    public class VotosPorHoraDTO
    {
        public int Hora { get; set; }
        public int QuantidadeVotos { get; set; }
        public double PercentualHora { get; set; }
    }
    
    /// <summary>
    /// DTO para votos por UF
    /// </summary>
    public class VotosPorUfDTO
    {
        public int UfId { get; set; }
        public string UfSigla { get; set; }
        public string UfNome { get; set; }
        public int TotalVotos { get; set; }
        public double PercentualParticipacao { get; set; }
    }
    
    /// <summary>
    /// DTO para chapa em votação
    /// </summary>
    public class ChapaVotacaoDTO
    {
        public int ChapaId { get; set; }
        public string NumeroChapa { get; set; }
        public string NomeChapa { get; set; }
        public int TotalVotos { get; set; }
        public string FotoChapa { get; set; }
        public string Slogan { get; set; }
    }
    
    /// <summary>
    /// DTO para participação na votação
    /// </summary>
    public class ParticipacaoVotacaoDTO
    {
        public int CalendarioId { get; set; }
        public int TotalEleitoresGeral { get; set; }
        public int TotalVotantesGeral { get; set; }
        public double PercentualParticipacaoGeral { get; set; }
        public List<ParticipacaoUfDTO> ParticipacaoPorUf { get; set; }
    }
    
    /// <summary>
    /// DTO para participação por UF
    /// </summary>
    public class ParticipacaoUfDTO
    {
        public int UfId { get; set; }
        public string UfSigla { get; set; }
        public string UfNome { get; set; }
        public int TotalEleitores { get; set; }
        public int TotalVotantes { get; set; }
        public double PercentualParticipacao { get; set; }
        public string StatusVotacao { get; set; }
    }
    
    /// <summary>
    /// DTO para configurar segundo turno
    /// </summary>
    public class ConfigurarSegundoTurnoDTO
    {
        public int CalendarioId { get; set; }
        public List<int> ChapasClassificadas { get; set; }
        public DateTime DataSegundoTurno { get; set; }
        public int UsuarioConfiguradorId { get; set; }
    }
    
    /// <summary>
    /// DTO para boletim de urna
    /// </summary>
    public class BoletimUrnaDTO
    {
        public int Id { get; set; }
        public int SessaoVotacaoId { get; set; }
        public string NumeroUrna { get; set; }
        public DateTime DataGeracao { get; set; }
        public string HashBoletim { get; set; }
        public byte[] ConteudoPDF { get; set; }
        public string AssinaturaDigital { get; set; }
    }
    
    /// <summary>
    /// Enumeração de tipos de voto
    /// </summary>
    public enum TipoVoto
    {
        Nominal = 1,
        Branco = 2,
        Nulo = 3
    }
    
    /// <summary>
    /// Enumeração de status de sessão de votação
    /// </summary>
    public enum StatusSessaoVotacao
    {
        Preparando = 1,
        Aberta = 2,
        Fechada = 3,
        EmApuracao = 4,
        Apurada = 5,
        Homologada = 6,
        Cancelada = 7
    }
    
    /// <summary>
    /// Enumeração de status de urna
    /// </summary>
    public enum StatusUrna
    {
        Preparando = 1,
        Ativa = 2,
        Fechada = 3,
        Apurada = 4,
        Auditada = 5
    }
    
    /// <summary>
    /// Enumeração de status de apuração
    /// </summary>
    public enum StatusApuracao
    {
        NaoIniciada = 1,
        EmAndamento = 2,
        Concluida = 3,
        Homologada = 4,
        Contestada = 5,
        Anulada = 6
    }
}