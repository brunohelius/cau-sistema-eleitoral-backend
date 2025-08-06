using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_STATUS_PEDIDO_IMPUGNACAO", Schema = "eleitoral")]
public class StatusPedidoImpugnacao
{
    [Key]
    [Column("ID_STATUS_PEDIDO_IMPUGNACAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_STATUS_PEDIDO_IMPUGNACAO")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}