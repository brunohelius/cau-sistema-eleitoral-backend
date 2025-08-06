using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_historico_denuncia", Schema = "public")]
public class HistoricoDenuncia : BaseEntity
{
    [Column("denuncia_id")]
    public int DenunciaId { get; set; }
    
    [Column("status_anterior")]
    public string StatusAnterior { get; set; } = string.Empty;
    
    [Column("status_novo")]
    public string StatusNovo { get; set; } = string.Empty;
    
    [Column("observacao")]
    public string? Observacao { get; set; }
    
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    
    [Column("data_ocorrencia")]
    public DateTime DataOcorrencia { get; set; } = DateTime.UtcNow;
    
    // Navigation Properties
    public virtual Denuncia Denuncia { get; set; } = null!;
    public virtual Usuario Usuario { get; set; } = null!;
}