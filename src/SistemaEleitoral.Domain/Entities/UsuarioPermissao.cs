using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_usuario_permissoes", Schema = "public")]
public class UsuarioPermissao : BaseEntity
{
    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("permissao_id")]
    public int PermissaoId { get; set; }
    
    [Column("data_expiracao")]
    public DateTime? DataExpiracao { get; set; }

    // Navegação
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual Permissao Permissao { get; set; } = null!;
}