using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_SITUACAO_ELEICAO", Schema = "eleitoral")]
public class SituacaoEleicao
{
    [Key]
    [Column("ID_SITUACAO_ELEICAO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_SITUACAO_ELEICAO")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;
}