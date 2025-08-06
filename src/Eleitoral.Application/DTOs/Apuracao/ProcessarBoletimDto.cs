using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Eleitoral.Application.DTOs.Apuracao
{
    /// <summary>
    /// DTO para processar boletim de urna
    /// </summary>
    public class ProcessarBoletimDto
    {
        [Required]
        public int ResultadoApuracaoId { get; set; }
        
        [Required]
        public int NumeroUrna { get; set; }
        
        [Required]
        public string CodigoIdentificacao { get; set; }
        
        [Required]
        public string LocalVotacao { get; set; }
        
        [Required]
        public string Secao { get; set; }
        
        [Required]
        public string Zona { get; set; }
        
        [Required]
        public int TotalEleitoresUrna { get; set; }
        
        [Required]
        public int TotalVotantes { get; set; }
        
        [Required]
        public int VotosBrancos { get; set; }
        
        [Required]
        public int VotosNulos { get; set; }
        
        [Required]
        public DateTime DataHoraAbertura { get; set; }
        
        [Required]
        public DateTime DataHoraEncerramento { get; set; }
        
        [Required]
        public int TotalUrnasEleicao { get; set; }
        
        public string ArquivoBoletim { get; set; }
        
        public List<VotoChapaDto> VotosChapas { get; set; }
        
        public ProcessarBoletimDto()
        {
            VotosChapas = new List<VotoChapaDto>();
        }
    }
    
    /// <summary>
    /// DTO para voto de chapa
    /// </summary>
    public class VotoChapaDto
    {
        [Required]
        public int ChapaId { get; set; }
        
        [Required]
        [Range(0, int.MaxValue)]
        public int QuantidadeVotos { get; set; }
    }
}