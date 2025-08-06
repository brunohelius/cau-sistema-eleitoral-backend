using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_CABECALHO_EMAIL_UF", Schema = "eleitoral")]
public class CabecalhoEmailUf
{
    [Key]
    [Column("ID_CABECALHO_EMAIL_UF")]
    public int Id { get; set; }

    [Column("ID_CABECALHO_EMAIL")]
    public int CabecalhoEmailId { get; set; }

    [Column("ID_UF")]
    public int UfId { get; set; }

    // Navegação
    public virtual CabecalhoEmail CabecalhoEmail { get; set; } = null!;
    public virtual Uf Uf { get; set; } = null!;
}