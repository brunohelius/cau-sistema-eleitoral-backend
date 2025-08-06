using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_INF_COMISSAO_MEMBRO", Schema = "eleitoral")]
public class InformacaoComissaoMembro
{
    [Key]
    [Column("ID_INF_COMISSAO_MEMBRO")]
    public int Id { get; set; }

    [Column("ST_MAJORITARIO")]
    public bool SituacaoMajoritario { get; set; }

    [Column("ST_CONSELHEIRO")]
    public bool SituacaoConselheiro { get; set; }

    [Column("TP_OPCAO")]
    public int? TipoOpcao { get; set; }

    [Column("QTDE_MINIMA")]
    public int? QuantidadeMinima { get; set; }

    [Column("QTDE_MAXIMA")]
    public int? QuantidadeMaxima { get; set; }

    [Column("ST_CONCLUIDO")]
    public bool SituacaoConcluido { get; set; }

    [Column("ID_ATIV_SECUNDARIA")]
    public int AtividadeSecundariaId { get; set; }

    // Navegação
    public virtual AtividadeSecundariaCalendario AtividadeSecundaria { get; set; } = null!;
    public virtual DocumentoComissaoMembro? DocumentoComissaoMembro { get; set; }
    public virtual ICollection<MembroComissaoEleitoral> MembrosComissao { get; set; } = new List<MembroComissaoEleitoral>();
}