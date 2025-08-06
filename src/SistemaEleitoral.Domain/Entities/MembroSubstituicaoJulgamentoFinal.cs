using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_MEMBRO_SUBSTITUICAO_JULGAMENTO_FINAL", Schema = "eleitoral")]
public class MembroSubstituicaoJulgamentoFinal
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("ID_SUBSTITUICAO_JULGAMENTO_FINAL")]
    public int SubstituicaoJulgamentoFinalId { get; set; }

    [Column("ID_MEMBRO_COMISSAO_TITULAR")]
    public int MembroComissaoEleitoralTitularId { get; set; }

    [Column("ID_MEMBRO_COMISSAO_SUBSTITUTO")]
    public int MembroComissaoEleitoralSubstitutoId { get; set; }

    [Column("DS_JUSTIFICATIVA")]
    [StringLength(1000)]
    public string? Justificativa { get; set; }

    // Navegação
    public virtual SubstituicaoJulgamentoFinal SubstituicaoJulgamentoFinal { get; set; } = null!;
    public virtual MembroComissaoEleitoral MembroComissaoEleitoralTitular { get; set; } = null!;
    public virtual MembroComissaoEleitoral MembroComissaoEleitoralSubstituto { get; set; } = null!;
}