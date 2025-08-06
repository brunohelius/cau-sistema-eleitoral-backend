using System;

namespace Eleitoral.Application.DTOs.Apuracao
{
    /// <summary>
    /// DTO para log de apuração
    /// </summary>
    public class LogApuracaoDto
    {
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        
        public DateTime DataHora { get; set; }
        public string Descricao { get; set; }
        public string Tipo { get; set; }
        
        public string Usuario { get; set; }
        public string IpOrigem { get; set; }
        
        public string DadosAnteriores { get; set; }
        public string DadosNovos { get; set; }
        public string Observacoes { get; set; }
    }
}