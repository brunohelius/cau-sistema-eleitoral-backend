using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Entities
{
    public class Voto
    {
        public int Id { get; set; }
        public int SessaoVotacaoId { get; set; }
        public int EleitorId { get; set; }
        public int? ChapaId { get; set; }
        public TipoVoto TipoVoto { get; set; }
        public DateTime DataVoto { get; set; }
        public string HashVoto { get; set; }
        public string IpOrigem { get; set; }
        public string? UserAgent { get; set; }
        
        public virtual SessaoVotacao SessaoVotacao { get; set; }
        public virtual Profissional Eleitor { get; set; }
        public virtual ChapaEleicao? Chapa { get; set; }
    }

    public class SessaoVotacao
    {
        public int Id { get; set; }
        public int CalendarioId { get; set; }
        public int? UfId { get; set; }
        public DateTime DataAbertura { get; set; }
        public DateTime DataFechamento { get; set; }
        public bool Ativa { get; set; }
        public int? AbertaPorId { get; set; }
        public int? FechadaPorId { get; set; }
        public DateTime? DataFechamentoReal { get; set; }
        
        public virtual Calendario Calendario { get; set; }
        public virtual Profissional? AbertaPor { get; set; }
        public virtual Profissional? FechadaPor { get; set; }
        public virtual ICollection<Voto> Votos { get; set; }
    }

    public class ComprovanteVotacao
    {
        public int Id { get; set; }
        public int VotoId { get; set; }
        public string ProtocoloComprovante { get; set; }
        public string CodigoVerificacao { get; set; }
        public DateTime DataEmissao { get; set; }
        public string HashComprovante { get; set; }
        
        public virtual Voto Voto { get; set; }
    }
}