using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_STATUS_JULGAMENTO_FINAL", Schema = "eleitoral")]
public class StatusJulgamentoFinal
{
    [Key]
    [Column("ID_STATUS_JULGAMENTO_FINAL")]
    public int Id { get; set; }

    [Required]
    [Column("DS_STATUS_JULGAMENTO_FINAL")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}