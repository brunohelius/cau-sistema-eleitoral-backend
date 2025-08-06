namespace SistemaEleitoral.Domain.Entities
{
    public class VotoChapa
    {
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        public int ChapaId { get; set; }
        public int QuantidadeVotos { get; set; }
        public decimal PercentualVotos { get; set; }
        public int Posicao { get; set; }
        public bool Eleita { get; set; }
        
        public virtual ResultadoApuracao ResultadoApuracao { get; set; }
        public virtual ChapaEleicao Chapa { get; set; }
    }
}