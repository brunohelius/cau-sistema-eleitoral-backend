using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_SUBSTITUICAO_JULGAMENTO_FINAL", Schema = "eleitoral")]
public class SubstituicaoJulgamentoFinal
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [Column("JUSTIFICATIVA", TypeName = "text")]
    public string Justificativa { get; set; } = string.Empty;

    [Column("DT_CADASTRO")]
    public DateTime DataCadastro { get; set; }

    [Column("NM_ARQUIVO")]
    [StringLength(200)]
    public string? NomeArquivo { get; set; }

    [Column("NM_FIS_ARQUIVO")]
    [StringLength(200)]
    public string? NomeArquivoFisico { get; set; }

    [Column("ID_PROFISSIONAL_INCLUSAO")]
    public int ProfissionalId { get; set; }

    [Column("ID_JULGAMENTO_FINAL")]
    public int JulgamentoFinalId { get; set; }

    // Navegação
    public virtual Profissional Profissional { get; set; } = null!;
    public virtual JulgamentoFinal JulgamentoFinal { get; set; } = null!;
    public virtual ICollection<MembroSubstituicaoJulgamentoFinal> MembrosSubstituicaoJulgamentoFinal { get; set; } = new List<MembroSubstituicaoJulgamentoFinal>();
    public virtual JulgamentoSegundaInstanciaSubstituicao? JulgamentoSegundaInstanciaSubstituicao { get; set; }
}