using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_LEI", Schema = "eleitoral")]
public class Lei
{
    [Key]
    [Column("ID_LEI")]
    public int Id { get; set; }

    [Required]
    [Column("DS_LEI")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}