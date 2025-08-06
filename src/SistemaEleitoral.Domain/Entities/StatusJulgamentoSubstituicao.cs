using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_STATUS_JULGAMENTO_SUBSTITUICAO", Schema = "eleitoral")]
public class StatusJulgamentoSubstituicao
{
    [Key]
    [Column("ID_STATUS_JULGAMENTO_SUBSTITUICAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_STATUS_JULGAMENTO_SUBSTITUICAO")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}