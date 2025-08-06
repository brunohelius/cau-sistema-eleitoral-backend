using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_STATUS_VALIDACAO_MEMBRO_CHAPA", Schema = "eleitoral")]
public class StatusValidacaoMembroChapa
{
    [Key]
    [Column("ID_STATUS_VALIDACAO_MEMBRO_CHAPA")]
    public int Id { get; set; }

    [Required]
    [Column("DS_STATUS_VALIDACAO_MEMBRO_CHAPA")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}