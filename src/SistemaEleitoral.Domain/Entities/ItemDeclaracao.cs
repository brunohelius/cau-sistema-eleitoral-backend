using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_ITEM_DECLARACAO", Schema = "eleitoral")]
public class ItemDeclaracao
{
    [Key]
    [Column("ID_ITEM_DECLARACAO")]
    public int Id { get; set; }

    [Column("ID_DECLARACAO")]
    public int DeclaracaoId { get; set; }

    [Required]
    [Column("DS_ITEM_DECLARACAO")]
    [StringLength(500)]
    public string Descricao { get; set; } = string.Empty;

    [Column("NR_ORDEM")]
    public int OrdemExibicao { get; set; }

    // Navegação
    public virtual Declaracao Declaracao { get; set; } = null!;
    public virtual ICollection<RespostaDeclaracao> RespostasDeclaracao { get; set; } = new List<RespostaDeclaracao>();
}