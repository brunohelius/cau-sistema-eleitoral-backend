using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_STATUS_JULGAMENTO_IMPUGNACAO", Schema = "eleitoral")]
public class StatusJulgamentoImpugnacao
{
    [Key]
    [Column("ID_STATUS_JULGAMENTO_IMPUGNACAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_STATUS_JULGAMENTO_IMPUGNACAO")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}