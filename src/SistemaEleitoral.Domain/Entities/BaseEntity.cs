using System.ComponentModel.DataAnnotations;

namespace SistemaEleitoral.Domain.Entities;

public abstract class BaseEntity
{
    [Key]
    public virtual int Id { get; set; }
    
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    
    public DateTime? DataAtualizacao { get; set; }
    
    public bool Ativo { get; set; } = true;
}