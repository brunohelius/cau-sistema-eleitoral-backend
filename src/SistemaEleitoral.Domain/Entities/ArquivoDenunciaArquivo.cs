using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_ARQUIVO_DENUNCIA", Schema = "eleitoral")]
public class ArquivoDenunciaArquivo
{
    [Key]
    [Column("ID_ARQUIVO_DENUNCIA")]
    public int Id { get; set; }

    [Required]
    [Column("NM_ARQUIVO")]
    [StringLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required]
    [Column("NM_FIS_ARQUIVO")]
    [StringLength(200)]
    public string NomeFisico { get; set; } = string.Empty;

    [Column("ID_DENUNCIA")]
    public int DenunciaId { get; set; }

    // Navegação
    public virtual Denuncia Denuncia { get; set; } = null!;

    // Propriedades transientes (não mapeadas)
    [NotMapped]
    public byte[]? Arquivo { get; set; }

    [NotMapped]
    public long? Tamanho { get; set; }
}