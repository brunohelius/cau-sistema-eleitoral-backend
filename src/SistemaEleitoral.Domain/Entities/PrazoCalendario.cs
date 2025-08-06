using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_prazo_calendario", Schema = "eleitoral")]
public class PrazoCalendario : BaseEntity
{
    [Column("id_prazo_calendario")]
    public override int Id { get; set; }

    [Column("id_atividade_principal_calendario")]
    public int? AtividadePrincipalCalendarioId { get; set; }

    [Column("id_atividade_secundaria_calendario")]
    public int? AtividadeSecundariaCalendarioId { get; set; }

    [Column("ds_nome")]
    [Required]
    [StringLength(500)]
    public string Nome { get; set; } = string.Empty;

    [Column("ds_descricao")]
    [StringLength(4000)]
    public string? Descricao { get; set; }

    [Column("dt_inicio")]
    [Required]
    public DateTime DataInicio { get; set; }

    [Column("dt_fim")]
    [Required]
    public DateTime DataFim { get; set; }

    [Column("st_obrigatorio")]
    public bool Obrigatorio { get; set; } = false;

    [Column("nu_dias_alerta")]
    public int? DiasAlerta { get; set; }

    [Column("st_alerta_enviado")]
    public bool AlertaEnviado { get; set; } = false;

    // Navigation properties
    [ForeignKey("AtividadePrincipalCalendarioId")]
    public virtual AtividadePrincipalCalendario? AtividadePrincipal { get; set; }

    [ForeignKey("AtividadeSecundariaCalendarioId")]
    public virtual AtividadeSecundariaCalendario? AtividadeSecundaria { get; set; }

    // Propriedades computadas
    public bool IsVigente(DateTime? data = null)
    {
        var dataReferencia = data ?? DateTime.Now.Date;
        return dataReferencia >= DataInicio.Date && dataReferencia <= DataFim.Date;
    }

    public bool IsVencido(DateTime? data = null)
    {
        var dataReferencia = data ?? DateTime.Now.Date;
        return dataReferencia > DataFim.Date;
    }

    public bool DeveEnviarAlerta(DateTime? data = null)
    {
        if (!DiasAlerta.HasValue || AlertaEnviado)
            return false;

        var dataReferencia = data ?? DateTime.Now.Date;
        var dataLimiteAlerta = DataFim.AddDays(-DiasAlerta.Value);
        
        return dataReferencia >= dataLimiteAlerta && dataReferencia <= DataFim;
    }

    public int DiasRestantes(DateTime? data = null)
    {
        var dataReferencia = data ?? DateTime.Now.Date;
        return (DataFim.Date - dataReferencia).Days;
    }
}