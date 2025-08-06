using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_JULGAMENTO_FINAL", Schema = "eleitoral")]
public class JulgamentoFinal
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [Column("DS_JULGAMENTO_FINAL")]
    public string Descricao { get; set; } = string.Empty;

    [Column("DT_JULGAMENTO")]
    public DateTime DataJulgamento { get; set; }

    [Column("ID_STATUS_JULGAMENTO_FINAL")]
    public int StatusJulgamentoFinalId { get; set; }

    [Column("NM_ARQUIVO")]
    [StringLength(200)]
    public string? NomeArquivo { get; set; }

    [Column("NM_FIS_ARQUIVO")]
    [StringLength(200)]
    public string? NomeArquivoFisico { get; set; }

    // Navegação
    public virtual StatusJulgamentoFinal StatusJulgamentoFinal { get; set; } = null!;
    public virtual SubstituicaoJulgamentoFinal? SubstituicaoJulgamentoFinal { get; set; }

    // Propriedades transientes (não mapeadas)
    [NotMapped]
    public byte[]? Arquivo { get; set; }

    [NotMapped]
    public long? Tamanho { get; set; }
}