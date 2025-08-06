using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_STATUS_JULGAMENTO_ALEGACAO_RESULTADO", Schema = "eleitoral")]
public class StatusJulgamentoAlegacaoResultado
{
    [Key]
    [Column("ID_STATUS_JULGAMENTO_ALEGACAO_RESULTADO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_STATUS_JULGAMENTO_ALEGACAO_RESULTADO")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}