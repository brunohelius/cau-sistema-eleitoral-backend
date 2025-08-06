using System;
using System.Collections.Generic;

namespace Eleitoral.Application.DTOs.Apuracao
{
    /// <summary>
    /// DTO para boletim de urna
    /// </summary>
    public class BoletimUrnaDto
    {
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        
        public int NumeroUrna { get; set; }
        public string CodigoIdentificacao { get; set; }
        public string LocalVotacao { get; set; }
        public string Secao { get; set; }
        public string Zona { get; set; }
        
        public int TotalEleitoresUrna { get; set; }
        public int TotalVotantes { get; set; }
        public int VotosBrancos { get; set; }
        public int VotosNulos { get; set; }
        
        public DateTime DataHoraAbertura { get; set; }
        public DateTime DataHoraEncerramento { get; set; }
        public DateTime DataHoraProcessamento { get; set; }
        
        public string Status { get; set; }
        public string HashBoletim { get; set; }
        
        public bool Conferido { get; set; }
        public string ConferidoPor { get; set; }
        public DateTime? DataConferencia { get; set; }
        
        public string ArquivoBoletim { get; set; }
        public string Observacoes { get; set; }
        
        public List<VotoChapaResumoDto> VotosChapas { get; set; }
        
        public BoletimUrnaDto()
        {
            VotosChapas = new List<VotoChapaResumoDto>();
        }
    }
    
    /// <summary>
    /// DTO resumido para voto de chapa
    /// </summary>
    public class VotoChapaResumoDto
    {
        public int ChapaId { get; set; }
        public string NomeChapa { get; set; }
        public int NumeroChapa { get; set; }
        public int QuantidadeVotos { get; set; }
    }
}