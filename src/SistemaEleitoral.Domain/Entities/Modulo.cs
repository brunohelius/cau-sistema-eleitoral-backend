using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_MODULO", Schema = "portal")]
public class Modulo
{
    [Key]
    [Column("ID_MODULO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_MODULO")]
    public string Descricao { get; set; } = string.Empty;
}