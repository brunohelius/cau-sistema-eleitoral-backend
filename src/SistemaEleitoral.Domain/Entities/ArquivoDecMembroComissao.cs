using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_ARQUIVO_DEC_MEMBRO_COMISSAO", Schema = "eleitoral")]
public class ArquivoDecMembroComissao
{
    [Key]
    [Column("ID_ARQUIVO_DEC_MEMBRO_COMISSAO")]
    public int Id { get; set; }

    [Column("ID_MEMBRO_COMISSAO")]
    public int MembroComissaoEleitoralId { get; set; }

    [Column("NM_ARQUIVO")]
    [StringLength(200)]
    public string? NomeArquivo { get; set; }

    [Column("NM_FIS_ARQUIVO")]
    [StringLength(200)]
    public string? NomeFisicoArquivo { get; set; }

    [Column("DT_UPLOAD")]
    public DateTime? DataUpload { get; set; }

    // Navegação
    public virtual MembroComissaoEleitoral MembroComissaoEleitoral { get; set; } = null!;

    // Propriedades transientes (não mapeadas)
    [NotMapped]
    public byte[]? Arquivo { get; set; }

    [NotMapped]
    public long? Tamanho { get; set; }
}