using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_CABECALHO_EMAIL", Schema = "eleitoral")]
public class CabecalhoEmail
{
    [Key]
    [Column("ID_CABECALHO_EMAIL")]
    public int Id { get; set; }

    [Required]
    [Column("DS_TITULO")]
    [StringLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [Column("NM_FIGURA_CABECALHO")]
    [StringLength(200)]
    public string? NomeImagemCabecalho { get; set; }

    [Column("NM_FIS_FIGURA_CABECALHO")]
    [StringLength(200)]
    public string? NomeImagemFisicaCabecalho { get; set; }

    [Column("DS_CABECALHO", TypeName = "text")]
    public string? TextoCabecalho { get; set; }

    [Column("ST_CABECALHO_ATIVO")]
    public bool IsCabecalhoAtivo { get; set; }

    [Column("NM_FIGURA_RODAPE")]
    [StringLength(200)]
    public string? NomeImagemRodape { get; set; }

    [Column("NM_FIS_FIGURA_RODAPE")]
    [StringLength(200)]
    public string? NomeImagemFisicaRodape { get; set; }

    [Column("DS_RODAPE", TypeName = "text")]
    public string? TextoRodape { get; set; }

    [Column("ST_RODAPE_ATIVO")]
    public bool IsRodapeAtivo { get; set; }

    // Navegação
    public virtual ICollection<CabecalhoEmailUf> CabecalhoEmailUfs { get; set; } = new List<CabecalhoEmailUf>();
    public virtual ICollection<CorpoEmail> CorpoEmails { get; set; } = new List<CorpoEmail>();

    // Propriedades transientes (não mapeadas)
    [NotMapped]
    public string? ImagemCabecalho { get; set; }

    [NotMapped]
    public string? ImagemRodape { get; set; }

    [NotMapped]
    public bool Ativo => IsCabecalhoAtivo || IsRodapeAtivo;
}