using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa recursos contra impugnação de resultado
    /// </summary>
    [Table("TB_RECURSO_IMPUGNACAO_RESULTADO", Schema = "eleitoral")]
    public class RecursoImpugnacaoResultado
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("DS_RECURSO")]
        public string Descricao { get; set; } = string.Empty;

        [Column("DT_CADASTRO")]
        public DateTime DataCadastro { get; set; }

        [Column("ID_IMPUGNACAO_RESULTADO")]
        public int ImpugnacaoResultadoId { get; set; }

        [Column("ID_PROFISSIONAL")]
        public int ProfissionalId { get; set; }

        [Column("ID_TIPO_RECURSO")]
        public int TipoRecursoId { get; set; }

        [Required]
        [Column("NM_ARQUIVO")]
        [MaxLength(200)]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required]
        [Column("NM_FIS_ARQUIVO")]
        [MaxLength(200)]
        public string NomeArquivoFisico { get; set; } = string.Empty;

        [Column("FL_DEFERIDO")]
        public bool? Deferido { get; set; }

        [Column("DS_PARECER")]
        public string? Parecer { get; set; }

        [Column("DT_JULGAMENTO")]
        public DateTime? DataJulgamento { get; set; }

        // Navigation Properties
        public virtual ImpugnacaoResultado ImpugnacaoResultado { get; set; } = null!;
        public virtual Profissional Profissional { get; set; } = null!;
        public virtual TipoRecursoImpugnacaoResultado TipoRecurso { get; set; } = null!;
        public virtual ContrarrazaoRecursoImpugnacaoResultado? Contrarrazao { get; set; }

        // Business Methods
        public bool PodeApresentarContrarrazao()
        {
            return !Deferido.HasValue && Contrarrazao == null;
        }

        public bool EstaJulgado()
        {
            return Deferido.HasValue && DataJulgamento.HasValue;
        }

        public void Julgar(bool deferido, string parecer)
        {
            Deferido = deferido;
            Parecer = parecer;
            DataJulgamento = DateTime.Now;
        }
    }
}