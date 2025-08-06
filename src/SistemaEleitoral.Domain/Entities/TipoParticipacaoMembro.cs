using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_TIPO_PARTICIPACAO", Schema = "eleitoral")]
public class TipoParticipacaoMembro
{
    [Key]
    [Column("ID_TIPO_PARTICIPACAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_TIPO_PARTICIPACAO")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}