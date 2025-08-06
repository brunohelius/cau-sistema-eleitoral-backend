using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_RESPOSTA_DECLARACAO", Schema = "eleitoral")]
public class RespostaDeclaracao
{
    [Key]
    [Column("ID_RESPOSTA_DECLARACAO")]
    public int Id { get; set; }

    [Column("ID_DECLARACAO")]
    public int DeclaracaoId { get; set; }

    [Column("ID_MEMBRO_CHAPA")]
    public int? MembroChapaId { get; set; }

    [Column("ID_ITEM_DECLARACAO")]
    public int ItemDeclaracaoId { get; set; }

    [Column("ST_RESPOSTA")]
    public bool StatusResposta { get; set; }

    [Column("DT_RESPOSTA")]
    public DateTime? DataResposta { get; set; }

    // Navegação
    public virtual Declaracao Declaracao { get; set; } = null!;
    public virtual MembroChapa? MembroChapa { get; set; }
    public virtual ItemDeclaracao ItemDeclaracao { get; set; } = null!;
}