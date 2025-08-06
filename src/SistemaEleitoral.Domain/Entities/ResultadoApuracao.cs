using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Entities
{
    public class ResultadoApuracao
    {
        public int Id { get; set; }
        public int CalendarioId { get; set; }
        public int? UfId { get; set; }
        public DateTime DataApuracao { get; set; }
        public int TotalEleitores { get; set; }
        public int TotalVotantes { get; set; }
        public int TotalAbstencoes { get; set; }
        public int VotosBrancos { get; set; }
        public int VotosNulos { get; set; }
        public int VotosValidos { get; set; }
        public decimal PercentualParticipacao { get; set; }
        public StatusApuracao Status { get; set; }
        public int? ChapaVencedoraId { get; set; }
        public bool NecessitaSegundoTurno { get; set; }
        public DateTime? DataHomologacao { get; set; }
        public int? HomologadoPorId { get; set; }
        
        public virtual Calendario Calendario { get; set; }
        public virtual ChapaEleicao? ChapaVencedora { get; set; }
        public virtual Profissional? HomologadoPor { get; set; }
        public virtual ICollection<ResultadoChapaApuracao> ResultadosChapas { get; set; }
    }

    public class ResultadoChapaApuracao
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

    public class ResultadoDivulgacao
    {
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        public DateTime DataDivulgacao { get; set; }
        public string CanalDivulgacao { get; set; }
        public string ConteudoDivulgado { get; set; }
        public int DivulgadoPorId { get; set; }
        
        public virtual ResultadoApuracao ResultadoApuracao { get; set; }
        public virtual Profissional DivulgadoPor { get; set; }
    }

    public class HistoricoResultado
    {
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        public string Acao { get; set; }
        public string Descricao { get; set; }
        public DateTime DataAcao { get; set; }
        public int ResponsavelId { get; set; }
        
        public virtual ResultadoApuracao ResultadoApuracao { get; set; }
        public virtual Profissional Responsavel { get; set; }
    }

    public class RelatorioEstatistico
    {
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        public string TipoRelatorio { get; set; }
        public DateTime DataGeracao { get; set; }
        public string ConteudoRelatorio { get; set; }
        public int GeradoPorId { get; set; }
        
        public virtual ResultadoApuracao ResultadoApuracao { get; set; }
        public virtual Profissional GeradoPor { get; set; }
    }
}