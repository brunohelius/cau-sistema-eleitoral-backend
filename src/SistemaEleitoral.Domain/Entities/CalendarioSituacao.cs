using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_calendario_situacao", Schema = "eleitoral")]
public class CalendarioSituacao : BaseEntity
{
    [Column("id_calendario_situacao")]
    public override int Id { get; set; }

    [Column("id_calendario")]
    [Required]
    public int CalendarioId { get; set; }

    [Column("id_situacao_calendario")]
    [Required]
    public int SituacaoCalendarioId { get; set; }

    [Column("dt_situacao")]
    [Required]
    public DateTime DataSituacao { get; set; } = DateTime.UtcNow;

    [Column("id_usuario")]
    public int? UsuarioId { get; set; }

    [Column("ds_observacao")]
    [StringLength(4000)]
    public string? Observacao { get; set; }

    // Navigation properties
    [ForeignKey("CalendarioId")]
    public virtual Calendario Calendario { get; set; } = null!;
}