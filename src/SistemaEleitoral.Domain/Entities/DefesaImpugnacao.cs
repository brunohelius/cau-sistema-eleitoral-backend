using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_DEFESA_IMPUGNACAO", Schema = "eleitoral")]
public class DefesaImpugnacao
{
    [Key]
    [Column("ID_DEFESA_IMPUGNACAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_DEFESA_IMPUGNACAO")]
    public string Descricao { get; set; } = string.Empty;

    [Column("DT_CADASTRO")]
    public DateTime DataCadastro { get; set; }

    [Column("ID_PROFISSIONAL_INCLUSAO")]
    public int IdProfissionalInclusao { get; set; }

    [Column("ID_PEDIDO_IMPUGNACAO")]
    public int PedidoImpugnacaoId { get; set; }

    // Navegação
    public virtual PedidoImpugnacao PedidoImpugnacao { get; set; } = null!;
    public virtual ICollection<ArquivoDefesaImpugnacao> Arquivos { get; set; } = new List<ArquivoDefesaImpugnacao>();
}