using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("TB_CORPO_EMAIL", Schema = "eleitoral")]
public class CorpoEmail
{
    [Key]
    [Column("ID_CORPO_EMAIL")]
    public int Id { get; set; }

    [Required]
    [Column("DS_ASSUNTO")]
    [StringLength(200)]
    public string Assunto { get; set; } = string.Empty;

    [Column("DS_CORPO_EMAIL", TypeName = "text")]
    public string? Descricao { get; set; }

    [Column("ST_ATIVO")]
    public bool Ativo { get; set; }

    [Column("ID_CABECALHO_EMAIL")]
    public int CabecalhoEmailId { get; set; }

    // Navegação
    public virtual CabecalhoEmail CabecalhoEmail { get; set; } = null!;
    public virtual ICollection<EmailAtividadeSecundaria> EmailsAtividadeSecundaria { get; set; } = new List<EmailAtividadeSecundaria>();
}