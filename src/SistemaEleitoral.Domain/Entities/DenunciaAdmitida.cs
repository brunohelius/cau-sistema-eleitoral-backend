using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_DENUNCIA_ADMITIDA", Schema = "eleitoral")]
public class DenunciaAdmitida
{
    [Key]
    [Column("ID_DENUNCIA_ADMITIDA")]
    public int Id { get; set; }

    [Column("ID_DENUNCIA")]
    public int DenunciaId { get; set; }

    [Column("ID_MEMBRO_COMISSAO")]
    public int MembroComissaoEleitoralId { get; set; }

    [Column("DT_ADMISSAO")]
    public DateTime DataAdmissao { get; set; }

    [Column("DS_OBSERVACAO")]
    [StringLength(1000)]
    public string? Observacao { get; set; }

    // Navegação
    public virtual Denuncia Denuncia { get; set; } = null!;
    public virtual MembroComissaoEleitoral MembroComissaoEleitoral { get; set; } = null!;
}