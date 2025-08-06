using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa alegações sobre impugnação de resultado
    /// </summary>
    [Table("TB_ALEGACAO_IMPUGNACAO_RESULTADO", Schema = "eleitoral")]
    public class AlegacaoImpugnacaoResultado
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("DS_ALEGACAO")]
        public string Descricao { get; set; } = string.Empty;

        [Column("DT_CADASTRO")]
        public DateTime DataCadastro { get; set; }

        [Column("ID_IMPUGNACAO_RESULTADO")]
        public int ImpugnacaoResultadoId { get; set; }

        [Column("ID_PROFISSIONAL")]
        public int ProfissionalId { get; set; }

        [Column("ID_CHAPA_ELEICAO")]
        public int? ChapaEleicaoId { get; set; }

        [Required]
        [Column("NM_ARQUIVO")]
        [MaxLength(200)]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required]
        [Column("NM_FIS_ARQUIVO")]
        [MaxLength(200)]
        public string NomeArquivoFisico { get; set; } = string.Empty;

        [Column("FL_IMPUGNANTE")]
        public bool IsImpugnante { get; set; }

        // Navigation Properties
        public virtual ImpugnacaoResultado ImpugnacaoResultado { get; set; } = null!;
        public virtual Profissional Profissional { get; set; } = null!;
        public virtual ChapaEleicao? ChapaEleicao { get; set; }

        // Business Methods
        public bool PodeEditar()
        {
            // Pode editar até 24 horas após o cadastro
            return DateTime.Now.Subtract(DataCadastro).TotalHours <= 24;
        }

        public string ObterTipoAlegante()
        {
            return IsImpugnante ? "Impugnante" : "Impugnado";
        }
    }
}