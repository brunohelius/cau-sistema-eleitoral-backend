using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_RECURSO_IMPUGNACAO", Schema = "eleitoral")]
public class RecursoImpugnacao
{
    [Key]
    [Column("ID_RECURSO_IMPUGNACAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_RECURSO_IMPUGNACAO")]
    public string Descricao { get; set; } = string.Empty;

    [Column("DT_CADASTRO")]
    public DateTime DataCadastro { get; set; }

    [Column("ID_JULGAMENTO_IMPUGNACAO")]
    public int JulgamentoImpugnacaoId { get; set; }

    [Column("ID_PROFISSIONAL_INCLUSAO")]
    public int ProfissionalId { get; set; }

    [Column("NM_ARQUIVO")]
    [StringLength(200)]
    public string? NomeArquivo { get; set; }

    [Column("NM_FIS_ARQUIVO")]
    [StringLength(200)]
    public string? NomeArquivoFisico { get; set; }

    // Navegação
    public virtual JulgamentoImpugnacao JulgamentoImpugnacao { get; set; } = null!;
    public virtual Profissional Profissional { get; set; } = null!;

    // Propriedades transientes (não mapeadas)
    [NotMapped]
    public byte[]? Arquivo { get; set; }

    [NotMapped]
    public long? Tamanho { get; set; }
}