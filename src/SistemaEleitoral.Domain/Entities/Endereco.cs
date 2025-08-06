using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_enderecos", Schema = "public")]
public class Endereco
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("uf")]
    [StringLength(2)]
    public string Uf { get; set; } = string.Empty;

    [Column("pessoa_id")]
    public int PessoaId { get; set; }

    // Navegação
    public virtual Pessoa Pessoa { get; set; } = null!;
}