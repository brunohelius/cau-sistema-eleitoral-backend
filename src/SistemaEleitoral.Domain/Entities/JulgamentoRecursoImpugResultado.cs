using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa o julgamento de recurso de impugnação de resultado
    /// </summary>
    [Table("TB_JULGAMENTO_RECURSO_IMPUG_RESULTADO", Schema = "eleitoral")]
    public class JulgamentoRecursoImpugResultado
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Column("ID_IMPUGNACAO_RESULTADO")]
        public int ImpugnacaoResultadoId { get; set; }

        [Required]
        [Column("DS_PARECER")]
        public string Parecer { get; set; } = string.Empty;

        [Column("FL_DEFERIDO")]
        public bool Deferido { get; set; }

        [Column("DT_JULGAMENTO")]
        public DateTime DataJulgamento { get; set; }

        [Column("ID_PROFISSIONAL_JULGADOR")]
        public int ProfissionalJulgadorId { get; set; }

        [Column("ID_STATUS_JULGAMENTO")]
        public int StatusJulgamentoId { get; set; }

        [Required]
        [Column("NM_ARQUIVO_DECISAO")]
        [MaxLength(200)]
        public string? NomeArquivoDecisao { get; set; }

        [Column("NM_FIS_ARQUIVO_DECISAO")]
        [MaxLength(200)]
        public string? NomeArquivoFisicoDecisao { get; set; }

        [Column("FL_DECISAO_FINAL")]
        public bool DecisaoFinal { get; set; }

        // Navigation Properties
        public virtual ImpugnacaoResultado ImpugnacaoResultado { get; set; } = null!;
        public virtual Profissional ProfissionalJulgador { get; set; } = null!;
        public virtual StatusJulgamentoImpugnacao StatusJulgamento { get; set; } = null!;

        // Business Methods
        public void FinalizarJulgamento(bool deferido, string parecer, bool decisaoFinal = false)
        {
            Deferido = deferido;
            Parecer = parecer;
            DataJulgamento = DateTime.Now;
            DecisaoFinal = decisaoFinal;
            StatusJulgamentoId = deferido ? 2 : 3; // 2 = Deferido, 3 = Indeferido
        }

        public bool PodeRecorrerSegundaInstancia()
        {
            // Permite recurso em segunda instância se não for decisão final
            return !DecisaoFinal && DateTime.Now.Subtract(DataJulgamento).TotalDays <= 5;
        }

        public string ObterInstancia()
        {
            return DecisaoFinal ? "Segunda Instância" : "Primeira Instância";
        }

        public string ObterResultado()
        {
            return Deferido ? "Deferido" : "Indeferido";
        }
    }
}