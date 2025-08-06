using System;
using System.Collections.Generic;

namespace Eleitoral.Application.DTOs.Apuracao
{
    /// <summary>
    /// DTO para resultado de apuração
    /// </summary>
    public class ResultadoApuracaoDto
    {
        public int Id { get; set; }
        public int EleicaoId { get; set; }
        public string NomeEleicao { get; set; }
        
        public DateTime InicioApuracao { get; set; }
        public DateTime? FimApuracao { get; set; }
        
        public int TotalEleitores { get; set; }
        public int TotalVotantes { get; set; }
        public int VotosBrancos { get; set; }
        public int VotosNulos { get; set; }
        public int VotosValidos { get; set; }
        
        public decimal PercentualParticipacao { get; set; }
        public decimal PercentualApuracao { get; set; }
        
        public string Status { get; set; }
        
        public bool Auditado { get; set; }
        public DateTime? DataAuditoria { get; set; }
        public string HashApuracao { get; set; }
        
        public List<ResultadoChapaDto> ResultadosChapas { get; set; }
        
        public ResultadoApuracaoDto()
        {
            ResultadosChapas = new List<ResultadoChapaDto>();
        }
    }
    
    /// <summary>
    /// DTO para resultado de chapa
    /// </summary>
    public class ResultadoChapaDto
    {
        public int Id { get; set; }
        public int ChapaId { get; set; }
        public string NomeChapa { get; set; }
        public int NumeroChapa { get; set; }
        
        public int TotalVotos { get; set; }
        public decimal PercentualVotos { get; set; }
        
        public int? Posicao { get; set; }
        public bool Eleita { get; set; }
        
        public List<MembroChapaResumoDto> Membros { get; set; }
        
        public ResultadoChapaDto()
        {
            Membros = new List<MembroChapaResumoDto>();
        }
    }
    
    /// <summary>
    /// DTO resumido para membro de chapa
    /// </summary>
    public class MembroChapaResumoDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Cargo { get; set; }
        public string Foto { get; set; }
    }
}