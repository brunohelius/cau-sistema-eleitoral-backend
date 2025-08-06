using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_TIPO_JULGAMENTO", Schema = "eleitoral")]
public class TipoJulgamento
{
    [Key]
    [Column("ID_TIPO_JULGAMENTO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_TIPO_JULGAMENTO")]
    [StringLength(200)]
    public string Descricao { get; set; } = string.Empty;
}