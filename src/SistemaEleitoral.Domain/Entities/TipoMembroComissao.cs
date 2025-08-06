using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities
{
    [Table("tb_tipos_membro_comissao", Schema = "public")]
    public class TipoMembroComissao : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        [Column("nome")]
        public string Nome { get; set; } = string.Empty;

        [MaxLength(500)]
        [Column("descricao")]
        public string? Descricao { get; set; }

        [Column("ordem")]
        public int Ordem { get; set; }

        [Column("permite_voto")]
        public bool PermiteVoto { get; set; } = true;

        [Column("obrigatorio")]
        public bool Obrigatorio { get; set; } = false;

        [Column("quantidade_minima")]
        public int QuantidadeMinima { get; set; } = 1;

        [Column("quantidade_maxima")]
        public int? QuantidadeMaxima { get; set; }

        // Navegação
        public virtual ICollection<MembroComissao> MembrosComissao { get; set; } = new List<MembroComissao>();
    }
}