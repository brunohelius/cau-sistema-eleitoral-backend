using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_JULGAMENTO_IMPUGNACAO", Schema = "eleitoral")]
public class JulgamentoImpugnacao
{
    [Key]
    [Column("ID_JULGAMENTO_IMPUGNACAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_JULGAMENTO_IMPUGNACAO")]
    public string Descricao { get; set; } = string.Empty;

    [Required]
    [Column("NM_ARQUIVO")]
    [StringLength(200)]
    public string NomeArquivo { get; set; } = string.Empty;

    [Required]
    [Column("NM_FIS_ARQUIVO")]
    [StringLength(200)]
    public string NomeArquivoFisico { get; set; } = string.Empty;

    [Column("DT_CADASTRO")]
    public DateTime DataCadastro { get; set; }

    [Column("ID_PEDIDO_IMPUGNACAO")]
    public int PedidoImpugnacaoId { get; set; }

    [Column("ID_STATUS_JULGAMENTO_IMPUGNACAO")]
    public int StatusJulgamentoImpugnacaoId { get; set; }

    [Column("ID_USUARIO_INCLUSAO")]
    public int UsuarioId { get; set; }

    // Navegação
    public virtual PedidoImpugnacao PedidoImpugnacao { get; set; } = null!;
    public virtual StatusJulgamentoImpugnacao StatusJulgamentoImpugnacao { get; set; } = null!;
    public virtual Usuario Usuario { get; set; } = null!;
    public virtual ICollection<RecursoImpugnacao> RecursosImpugnacao { get; set; } = new List<RecursoImpugnacao>();

    // Propriedades transientes (não mapeadas)
    [NotMapped]
    public byte[]? Arquivo { get; set; }

    [NotMapped]
    public long? Tamanho { get; set; }
}