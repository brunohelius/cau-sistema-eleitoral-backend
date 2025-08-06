using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_MEMBRO_COMISSAO_STATUS", Schema = "eleitoral")]
public class MembroComissaoEleitoralSituacao
{
    [Key]
    [Column("ID_MEMBRO_COMISSAO_STATUS")]
    public int Id { get; set; }

    [Column("ID_STATUS_MEMBRO_COMISSAO")]
    public int SituacaoMembroComissaoId { get; set; }

    [Column("ID_MEMBRO_COMISSAO")]
    public int MembroComissaoEleitoralId { get; set; }

    [Column("DT_STATUS")]
    public DateTime Data { get; set; }

    // Navegação
    public virtual SituacaoMembroComissao SituacaoMembroComissao { get; set; } = null!;
    public virtual MembroComissaoEleitoral MembroComissaoEleitoral { get; set; } = null!;
}