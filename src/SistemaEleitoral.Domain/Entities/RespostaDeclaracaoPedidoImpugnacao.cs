using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_RESPOSTA_DECLARACAO_PEDIDO_IMPUGNACAO", Schema = "eleitoral")]
public class RespostaDeclaracaoPedidoImpugnacao
{
    [Key]
    [Column("ID_RESPOSTA_DECLARACAO_PEDIDO_IMPUGNACAO")]
    public int Id { get; set; }

    [Column("ID_RESPOSTA_DECLARACAO")]
    public int RespostaDeclaracaoId { get; set; }

    [Column("ID_PEDIDO_IMPUGNACAO")]
    public int PedidoImpugnacaoId { get; set; }

    // Navegação
    public virtual RespostaDeclaracao RespostaDeclaracao { get; set; } = null!;
    public virtual PedidoImpugnacao PedidoImpugnacao { get; set; } = null!;
}