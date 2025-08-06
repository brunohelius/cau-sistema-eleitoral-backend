using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_permissoes", Schema = "public")]
public class Permissao : BaseEntity
{
    [Column("numero")]
    public int Numero { get; set; }

    [Required]
    [Column("descricao")]
    public string Descricao { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    [Column("nome")]
    public string Nome { get; set; } = string.Empty;
    
    [MaxLength(50)]
    [Column("codigo")]
    public string Codigo { get; set; } = string.Empty;
    
    public virtual ICollection<UsuarioPermissao> UsuarioPermissoes { get; set; } = new List<UsuarioPermissao>();
    public virtual ICollection<RolePermissao> RolePermissoes { get; set; } = new List<RolePermissao>();
}