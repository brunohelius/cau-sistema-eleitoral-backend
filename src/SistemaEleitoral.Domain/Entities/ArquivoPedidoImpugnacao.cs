using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_ARQUIVO_PEDIDO_IMPUGNACAO", Schema = "eleitoral")]
public class ArquivoPedidoImpugnacao
{
    [Key]
    [Column("ID_ARQUIVO_PEDIDO_IMPUGNACAO")]
    public int Id { get; set; }

    [Required]
    [Column("NM_ARQUIVO")]
    [StringLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [Column("NM_FIS_ARQUIVO")]
    [StringLength(200)]
    public string NomeFisico { get; set; } = string.Empty;

    [Column("ID_PEDIDO_IMPUGNACAO")]
    public int PedidoImpugnacaoId { get; set; }

    // Navegação
    public virtual PedidoImpugnacao PedidoImpugnacao { get; set; } = null!;

    // Propriedades transientes (não mapeadas)
    [NotMapped]
    public byte[]? Arquivo { get; set; }

    [NotMapped]
    public long? Tamanho { get; set; }
}