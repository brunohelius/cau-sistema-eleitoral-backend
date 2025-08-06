using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_DECLARACAO", Schema = "eleitoral")]
public class Declaracao
{
    [Key]
    [Column("ID_DECLARACAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_DECLARACAO")]
    [StringLength(200)]
    public string Descricao { get; set; } = string.Empty;

    [Column("ST_OBRIGATORIA")]
    public bool Obrigatoria { get; set; }

    [Column("ST_ATIVA")]
    public bool Ativa { get; set; }

    // Navegação
    public virtual ICollection<ItemDeclaracao> ItensDeclaracao { get; set; } = new List<ItemDeclaracao>();
    public virtual ICollection<RespostaDeclaracao> RespostasDeclaracao { get; set; } = new List<RespostaDeclaracao>();
}