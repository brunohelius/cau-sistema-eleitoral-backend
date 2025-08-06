using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_TP_ALTERACAO", Schema = "eleitoral")]
public class TipoAlteracao
{
    [Key]
    [Column("ID_TP_ALTERACAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_TP_ALTERACAO")]
    [StringLength(200)]
    public string Descricao { get; set; } = string.Empty;
}