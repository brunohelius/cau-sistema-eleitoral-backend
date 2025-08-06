using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_uf_calendario", Schema = "eleitoral")]
public class UfCalendario : BaseEntity
{
    [Column("id_uf_calendario")]
    public override int Id { get; set; }

    [Column("id_calendario")]
    [Required]
    public int CalendarioId { get; set; }

    [Column("co_uf")]
    [Required]
    [StringLength(2)]
    public string CodigoUf { get; set; } = string.Empty;

    [Column("ds_uf")]
    [Required]
    [StringLength(100)]
    public string NomeUf { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey("CalendarioId")]
    public virtual Calendario Calendario { get; set; } = null!;
}