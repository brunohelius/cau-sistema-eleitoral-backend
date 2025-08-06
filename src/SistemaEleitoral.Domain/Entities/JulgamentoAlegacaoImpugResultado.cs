using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa o julgamento de alegação de impugnação de resultado
    /// </summary>
    [Table("TB_JULGAMENTO_ALEGACAO_IMPUG_RESULTADO", Schema = "eleitoral")]
    public class JulgamentoAlegacaoImpugResultado
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

        // Navigation Properties
        public virtual ImpugnacaoResultado ImpugnacaoResultado { get; set; } = null!;
        public virtual Profissional ProfissionalJulgador { get; set; } = null!;
        public virtual StatusJulgamentoAlegacaoResultado StatusJulgamento { get; set; } = null!;

        // Business Methods
        public void FinalizarJulgamento(bool deferido, string parecer)
        {
            Deferido = deferido;
            Parecer = parecer;
            DataJulgamento = DateTime.Now;
            StatusJulgamentoId = deferido ? 2 : 3; // 2 = Deferido, 3 = Indeferido
        }

        public bool PodeRecorrer()
        {
            // Permite recurso dentro de 5 dias úteis após o julgamento
            var diasUteis = 0;
            var dataLimite = DataJulgamento;
            
            while (diasUteis < 5)
            {
                dataLimite = dataLimite.AddDays(1);
                if (dataLimite.DayOfWeek != DayOfWeek.Saturday && 
                    dataLimite.DayOfWeek != DayOfWeek.Sunday)
                {
                    diasUteis++;
                }
            }
            
            return DateTime.Now <= dataLimite;
        }

        public string ObterResultado()
        {
            return Deferido ? "Deferido" : "Indeferido";
        }
    }
}