using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_JULGAMENTO_SEGUNDA_INSTANCIA_SUBSTITUICAO", Schema = "eleitoral")]
public class JulgamentoSegundaInstanciaSubstituicao
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("ID_SUBSTITUICAO_JULGAMENTO_FINAL")]
    public int SubstituicaoJulgamentoFinalId { get; set; }

    [Required]
    [Column("DS_JULGAMENTO")]
    public string Descricao { get; set; } = string.Empty;

    [Column("DT_JULGAMENTO")]
    public DateTime DataJulgamento { get; set; }

    [Column("NM_ARQUIVO")]
    [StringLength(200)]
    public string? NomeArquivo { get; set; }

    [Column("NM_FIS_ARQUIVO")]
    [StringLength(200)]
    public string? NomeArquivoFisico { get; set; }

    // Navegação
    public virtual SubstituicaoJulgamentoFinal SubstituicaoJulgamentoFinal { get; set; } = null!;

    // Propriedades transientes (não mapeadas)
    [NotMapped]
    public byte[]? Arquivo { get; set; }

    [NotMapped]
    public long? Tamanho { get; set; }
}