using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_STATUS_PARTIC_CHAPA", Schema = "eleitoral")]
public class StatusParticipacaoChapa
{
    [Key]
    [Column("ID_STATUS_PARTIC_CHAPA")]
    public int Id { get; set; }

    [Required]
    [Column("DS_STATUS_PARTIC_CHAPA")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}