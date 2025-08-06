using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_STATUS_RECURSO_DENUNCIA", Schema = "eleitoral")]
public class StatusRecursoDenuncia
{
    [Key]
    [Column("ID_STATUS_RECURSO_DENUNCIA")]
    public int Id { get; set; }

    [Required]
    [Column("DS_STATUS_RECURSO_DENUNCIA")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}