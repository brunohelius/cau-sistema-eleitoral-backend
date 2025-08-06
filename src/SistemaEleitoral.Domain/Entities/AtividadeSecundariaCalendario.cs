using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_atividade_secundaria_calendario", Schema = "eleitoral")]
public class AtividadeSecundariaCalendario : BaseEntity
{
    [Column("id_atividade_secundaria_calendario")]
    public override int Id { get; set; }

    [Column("id_atividade_principal_calendario")]
    [Required]
    public int AtividadePrincipalCalendarioId { get; set; }

    [Column("nu_ordem")]
    [Required]
    public int Ordem { get; set; }

    [Column("ds_nome")]
    [Required]
    [StringLength(500)]
    public string Nome { get; set; } = string.Empty;

    [Column("ds_descricao")]
    [StringLength(4000)]
    public string? Descricao { get; set; }

    [Column("st_obrigatorio")]
    public bool Obrigatorio { get; set; } = false;

    [Column("dt_inicio")]
    public DateTime? DataInicio { get; set; }

    [Column("dt_fim")]
    public DateTime? DataFim { get; set; }

    // Navigation properties
    [ForeignKey("AtividadePrincipalCalendarioId")]
    public virtual AtividadePrincipalCalendario AtividadePrincipal { get; set; } = null!;

    public virtual ICollection<PrazoCalendario> Prazos { get; set; } = new List<PrazoCalendario>();

    // Propriedades computadas
    public bool IsVigente(DateTime? data = null)
    {
        var dataReferencia = data ?? DateTime.Now.Date;
        
        if (DataInicio.HasValue && DataFim.HasValue)
        {
            return dataReferencia >= DataInicio.Value.Date && dataReferencia <= DataFim.Value.Date;
        }
        
        return true; // Se não tem datas específicas, considera o período da atividade principal
    }
}