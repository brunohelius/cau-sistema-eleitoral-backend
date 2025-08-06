using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Entities
{
    public class SolicitacaoRecontagem
    {
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        public int SolicitanteId { get; set; }
        public string Motivo { get; set; }
        public DateTime DataSolicitacao { get; set; }
        public StatusRecontagem Status { get; set; }
        public DateTime? DataConclusao { get; set; }
        
        public virtual ResultadoApuracao ResultadoApuracao { get; set; }
        public virtual Profissional Solicitante { get; set; }
    }

    public class ResultadoRecontagem
    {
        public int Id { get; set; }
        public int SolicitacaoRecontagemId { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataFinalizacao { get; set; }
        public int ResponsavelId { get; set; }
        public int TotalVotosRecontados { get; set; }
        public int TotalVotosBrancosRecontados { get; set; }
        public int TotalVotosNulosRecontados { get; set; }
        public int TotalVotosValidosRecontados { get; set; }
        public bool HouveDivergencia { get; set; }
        public string? DescricaoDivergencias { get; set; }
        
        public virtual SolicitacaoRecontagem SolicitacaoRecontagem { get; set; }
        public virtual Profissional Responsavel { get; set; }
    }

    public class ImpugnacaoResultado
    {
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        public int ImpugnanteId { get; set; }
        public string Motivo { get; set; }
        public string Fundamentacao { get; set; }
        public DateTime DataImpugnacao { get; set; }
        public StatusImpugnacao Status { get; set; }
        
        public virtual ResultadoApuracao ResultadoApuracao { get; set; }
        public virtual Profissional Impugnante { get; set; }
    }
}