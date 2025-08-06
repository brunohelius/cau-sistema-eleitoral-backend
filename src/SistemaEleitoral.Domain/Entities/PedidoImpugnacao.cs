using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_PEDIDO_IMPUGNACAO", Schema = "eleitoral")]
public class PedidoImpugnacao
{
    [Key]
    [Column("ID_PEDIDO_IMPUGNACAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_PEDIDO_IMPUGNACAO")]
    public string Descricao { get; set; } = string.Empty;

    [Column("NM_PROTOCOLO")]
    public int NumeroProtocolo { get; set; }

    [Column("DT_CADASTRO")]
    public DateTime DataCadastro { get; set; }

    [Column("ID_PROFISSIONAL_INCLUSAO")]
    public int ProfissionalId { get; set; }

    [Column("ID_STATUS_PEDIDO_IMPUGNACAO")]
    public int StatusPedidoImpugnacaoId { get; set; }

    [Column("ID_MEMBRO_CHAPA")]
    public int MembroChapaId { get; set; }

    // Navegação
    public virtual Profissional Profissional { get; set; } = null!;
    public virtual StatusPedidoImpugnacao StatusPedidoImpugnacao { get; set; } = null!;
    public virtual MembroChapa MembroChapa { get; set; } = null!;
    public virtual ICollection<ArquivoPedidoImpugnacao> ArquivosPedidoImpugnacao { get; set; } = new List<ArquivoPedidoImpugnacao>();
    public virtual ICollection<RespostaDeclaracaoPedidoImpugnacao> RespostasDeclaracaoPedidoImpugnacao { get; set; } = new List<RespostaDeclaracaoPedidoImpugnacao>();
    public virtual DefesaImpugnacao? DefesaImpugnacao { get; set; }
    public virtual SubstituicaoImpugnacao? SubstituicaoImpugnacao { get; set; }
    public virtual JulgamentoImpugnacao? JulgamentoImpugnacao { get; set; }
}