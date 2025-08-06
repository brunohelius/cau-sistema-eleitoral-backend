using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_UF", Schema = "eleitoral")]
public class Uf
{
    [Key]
    [Column("ID_UF")]
    public int Id { get; set; }

    [Required]
    [Column("SG_UF")]
    [StringLength(50)]
    public string SgUf { get; set; } = string.Empty;
}