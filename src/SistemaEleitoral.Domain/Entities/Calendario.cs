using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_calendario", Schema = "eleitoral")]
public class Calendario : BaseEntity
{
    [Column("id_calendario")]
    public override int Id { get; set; }

    [Column("st_ies")]
    [Required]
    public bool SituacaoIES { get; set; }

    [Column("dt_ini_vigencia")]
    [Required]
    public DateTime DataInicioVigencia { get; set; }

    [Column("dt_fim_vigencia")]
    [Required]
    public DateTime DataFimVigencia { get; set; }

    [Column("nu_idade_ini")]
    [Required]
    [Range(0, 999)]
    public int IdadeInicio { get; set; }

    [Column("nu_idade_fim")]
    [Required]
    [Range(0, 999)]
    public int IdadeFim { get; set; }

    [Column("dt_ini_mandato")]
    [Required]
    public DateTime DataInicioMandato { get; set; }

    [Column("dt_fim_mandato")]
    [Required]
    public DateTime DataFimMandato { get; set; }

    [Column("id_situacao_vigente")]
    public int? IdSituacaoVigente { get; set; }

    [Column("st_excluido")]
    public bool Excluido { get; set; } = false;

    [Column("nu_ano")]
    public int? Ano { get; set; }

    [Column("ds_observacao")]
    [StringLength(4000)]
    public string? Observacao { get; set; }

    // Navigation properties
    public virtual ICollection<CalendarioSituacao> Situacoes { get; set; } = new List<CalendarioSituacao>();
    public virtual ICollection<AtividadePrincipalCalendario> AtividadesPrincipais { get; set; } = new List<AtividadePrincipalCalendario>();
    public virtual ICollection<UfCalendario> UfsCalendario { get; set; } = new List<UfCalendario>();
    public virtual ICollection<ArquivoCalendario> Arquivos { get; set; } = new List<ArquivoCalendario>();

    // Propriedades computadas
    public bool IsVigente(DateTime? data = null)
    {
        var dataReferencia = data ?? DateTime.Now.Date;
        return dataReferencia >= DataInicioVigencia.Date && dataReferencia <= DataFimVigencia.Date;
    }

    public bool IsMandatoVigente(DateTime? data = null)
    {
        var dataReferencia = data ?? DateTime.Now.Date;
        return dataReferencia >= DataInicioMandato.Date && dataReferencia <= DataFimMandato.Date;
    }

    public CalendarioStatus Status
    {
        get
        {
            return IdSituacaoVigente switch
            {
                1 => CalendarioStatus.EmPreenchimento,
                2 => CalendarioStatus.Concluido,
                3 => CalendarioStatus.Inativado,
                _ => CalendarioStatus.EmPreenchimento
            };
        }
    }
}

public enum CalendarioStatus
{
    EmPreenchimento = 1,
    Concluido = 2,
    Inativado = 3
}