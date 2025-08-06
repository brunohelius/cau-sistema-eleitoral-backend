using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_TIPO_SENTENCA_JULGAMENTO", Schema = "eleitoral")]
public class TipoSentencaJulgamento
{
    [Key]
    [Column("ID_TIPO_SENTENCA_JULGAMENTO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_TIPO_SENTENCA_JULGAMENTO")]
    [StringLength(200)]
    public string Descricao { get; set; } = string.Empty;
}