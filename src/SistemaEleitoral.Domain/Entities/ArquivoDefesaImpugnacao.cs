using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_ARQUIVO_DEFESA_IMPUGNACAO", Schema = "eleitoral")]
public class ArquivoDefesaImpugnacao
{
    [Key]
    [Column("ID_ARQUIVO_DEFESA_IMPUGNACAO")]
    public int Id { get; set; }

    [Required]
    [Column("NM_ARQUIVO")]
    [StringLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [Column("NM_FIS_ARQUIVO")]
    [StringLength(200)]
    public string NomeFisico { get; set; } = string.Empty;

    [Column("ID_DEFESA_IMPUGNACAO")]
    public int DefesaImpugnacaoId { get; set; }

    // Navegação
    public virtual DefesaImpugnacao DefesaImpugnacao { get; set; } = null!;

    // Propriedades transientes (não mapeadas)
    [NotMapped]
    public byte[]? Arquivo { get; set; }

    [NotMapped]
    public long? Tamanho { get; set; }
}