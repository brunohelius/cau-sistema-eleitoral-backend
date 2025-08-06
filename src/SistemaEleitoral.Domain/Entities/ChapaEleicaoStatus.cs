using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_CHAPA_ELEICAO_STATUS", Schema = "eleitoral")]
public class ChapaEleicaoStatus
{
    [Key]
    [Column("ID_CHAPA_ELEICAO_STATUS")]
    public int Id { get; set; }

    [Column("DT_CHAPA_ELEICAO_STATUS")]
    public DateTime Data { get; set; }

    [Column("ID_STATUS_CHAPA")]
    public int StatusChapaId { get; set; }

    [Column("ID_TP_ALTERACAO")]
    public int? TipoAlteracaoId { get; set; }

    [Column("ID_CHAPA_ELEICAO")]
    public int ChapaEleicaoId { get; set; }

    // Navegação
    public virtual StatusChapa StatusChapa { get; set; } = null!;
    public virtual TipoAlteracao? TipoAlteracao { get; set; }
    public virtual ChapaEleicao ChapaEleicao { get; set; } = null!;
}