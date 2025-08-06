using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_EMAIL_ATIVIDADE_SECUNDARIA", Schema = "eleitoral")]
public class EmailAtividadeSecundaria
{
    [Key]
    [Column("ID_EMAIL_ATIVIDADE_SECUNDARIA")]
    public int Id { get; set; }

    [Column("ID_CORPO_EMAIL")]
    public int CorpoEmailId { get; set; }

    [Column("ID_ATIV_SECUNDARIA")]
    public int AtividadeSecundariaId { get; set; }

    [Column("ID_EMAIL_ATIVIDADE_SECUNDARIA_TIPO")]
    public int EmailAtividadeSecundariaTipoId { get; set; }

    // Navegação
    public virtual CorpoEmail CorpoEmail { get; set; } = null!;
    public virtual AtividadeSecundariaCalendario AtividadeSecundaria { get; set; } = null!;
    public virtual EmailAtividadeSecundariaTipo EmailAtividadeSecundariaTipo { get; set; } = null!;
}