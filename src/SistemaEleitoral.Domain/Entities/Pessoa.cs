using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_pessoa", Schema = "public")]
public class Pessoa
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("enderecocorrespondencia")]
    public int? EnderecoCorrespondenciaId { get; set; }

    // Navegação
    public virtual Profissional? Profissional { get; set; }
    public virtual Endereco? EnderecoCorrespondencia { get; set; }
    public virtual ICollection<Conselheiro> Conselheiros { get; set; } = new List<Conselheiro>();
}