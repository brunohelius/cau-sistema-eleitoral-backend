using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_arquivo_calendario", Schema = "eleitoral")]
public class ArquivoCalendario : BaseEntity
{
    [Column("id_arquivo_calendario")]
    public override int Id { get; set; }

    [Column("id_calendario")]
    [Required]
    public int CalendarioId { get; set; }

    [Column("ds_nome_original")]
    [Required]
    [StringLength(500)]
    public string NomeOriginal { get; set; } = string.Empty;

    [Column("ds_nome_fisico")]
    [Required]
    [StringLength(500)]
    public string NomeFisico { get; set; } = string.Empty;

    [Column("ds_caminho")]
    [Required]
    [StringLength(1000)]
    public string Caminho { get; set; } = string.Empty;

    [Column("nu_tamanho")]
    [Required]
    public long Tamanho { get; set; }

    [Column("ds_tipo_mime")]
    [StringLength(100)]
    public string? TipoMime { get; set; }

    [Column("ds_extensao")]
    [StringLength(10)]
    public string? Extensao { get; set; }

    [Column("ds_tipo_arquivo")]
    [StringLength(50)]
    public string? TipoArquivo { get; set; } = "RESOLUCAO";

    // Navigation properties
    [ForeignKey("CalendarioId")]
    public virtual Calendario Calendario { get; set; } = null!;

    // Propriedades computadas
    public string TamanhoFormatado
    {
        get
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = Tamanho;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public bool IsImagemValida => !string.IsNullOrEmpty(TipoMime) && TipoMime.StartsWith("image/");
    public bool IsPdfValido => TipoMime == "application/pdf";
}