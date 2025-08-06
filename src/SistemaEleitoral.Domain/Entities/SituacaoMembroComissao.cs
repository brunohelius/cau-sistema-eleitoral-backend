using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_STATUS_MEMBRO_COMISSAO", Schema = "eleitoral")]
public class SituacaoMembroComissao
{
    [Key]
    [Column("ID_STATUS_MEMBRO_COMISSAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_STATUS_MEMBRO_COMISSAO")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}