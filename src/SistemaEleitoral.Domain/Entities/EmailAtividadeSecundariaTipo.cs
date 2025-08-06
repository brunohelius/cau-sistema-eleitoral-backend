using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_EMAIL_ATIVIDADE_SECUNDARIA_TIPO", Schema = "eleitoral")]
public class EmailAtividadeSecundariaTipo
{
    [Key]
    [Column("ID_EMAIL_ATIVIDADE_SECUNDARIA_TIPO")]
    public int Id { get; set; }

    [Required]
    [Column("DS_EMAIL_ATIVIDADE_SECUNDARIA_TIPO")]
    [StringLength(100)]
    public string Descricao { get; set; } = string.Empty;

    // Navegação
    public virtual ICollection<EmailAtividadeSecundaria> EmailsAtividadeSecundaria { get; set; } = new List<EmailAtividadeSecundaria>();
}