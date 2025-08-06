using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_DOCUMENTO_COMISSAO_MEMBRO", Schema = "eleitoral")]
public class DocumentoComissaoMembro
{
    [Key]
    [Column("ID_DOCUMENTO_COMISSAO_MEMBRO")]
    public int Id { get; set; }

    [Column("ID_INF_COMISSAO_MEMBRO")]
    public int InformacaoComissaoMembroId { get; set; }

    [Column("NM_DOCUMENTO")]
    [StringLength(200)]
    public string? NomeDocumento { get; set; }

    [Column("NM_FIS_DOCUMENTO")]
    [StringLength(200)]
    public string? NomeFisicoDocumento { get; set; }

    [Column("DT_CADASTRO")]
    public DateTime? DataCadastro { get; set; }

    // Navegação
    public virtual InformacaoComissaoMembro InformacaoComissaoMembro { get; set; } = null!;

    // Propriedades transientes (não mapeadas)
    [NotMapped]
    public byte[]? Arquivo { get; set; }
}