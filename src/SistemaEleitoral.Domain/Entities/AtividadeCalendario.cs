using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities
{
    [Table("tb_atividades_calendario", Schema = "public")]
    public class AtividadeCalendario : BaseEntity
    {
        [Required]
        [MaxLength(200)]
        [Column("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("descricao")]
        public string? Descricao { get; set; }

        [Column("calendario_id")]
        public int CalendarioId { get; set; }
        public virtual Calendario Calendario { get; set; } = null!;

        [Column("data_inicio")]
        public DateTime DataInicio { get; set; }

        [Column("data_fim")]
        public DateTime DataFim { get; set; }

        [Column("tipo_atividade")]
        public int TipoAtividade { get; set; }

        [Column("obrigatoria")]
        public bool Obrigatoria { get; set; } = true;

        [Column("concluida")]
        public bool Concluida { get; set; } = false;

        [Column("data_conclusao")]
        public DateTime? DataConclusao { get; set; }

        [MaxLength(100)]
        [Column("responsavel")]
        public string? Responsavel { get; set; }

        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Pendente";

        [Column("ordem")]
        public int Ordem { get; set; }

        [Column("permite_prorrogacao")]
        public bool PermiteProrrogacao { get; set; } = false;

        [Column("dias_prorrogacao")]
        public int? DiasProrrogacao { get; set; }

        [Column("notificar_antes_dias")]
        public int? NotificarAntesDias { get; set; }

        [MaxLength(500)]
        [Column("observacoes")]
        public string? Observacoes { get; set; }
    }
}