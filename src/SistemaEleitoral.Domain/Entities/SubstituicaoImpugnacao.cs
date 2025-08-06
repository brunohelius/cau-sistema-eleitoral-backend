using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_SUBSTITUICAO_IMPUGNACAO", Schema = "eleitoral")]
public class SubstituicaoImpugnacao
{
    [Key]
    [Column("ID_SUBSTITUICAO_IMPUGNACAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_SUBSTITUICAO_IMPUGNACAO")]
    public string Descricao { get; set; } = string.Empty;

    [Column("DT_CADASTRO")]
    public DateTime DataCadastro { get; set; }

    [Column("ID_PEDIDO_IMPUGNACAO")]
    public int PedidoImpugnacaoId { get; set; }

    [Column("ID_PROFISSIONAL_INCLUSAO")]
    public int ProfissionalId { get; set; }

    // Navegação
    public virtual PedidoImpugnacao PedidoImpugnacao { get; set; } = null!;
    public virtual Profissional Profissional { get; set; } = null!;
}