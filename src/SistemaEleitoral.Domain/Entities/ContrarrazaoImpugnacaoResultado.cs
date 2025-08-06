using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa contrarrazões de impugnação de resultado
    /// </summary>
    [Table("TB_CONTRARRAZAO_IMPUGNACAO_RESULTADO", Schema = "eleitoral")]
    public class ContrarrazaoImpugnacaoResultado
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("DS_CONTRARRAZAO")]
        public string Descricao { get; set; } = string.Empty;

        [Column("DT_CADASTRO")]
        public DateTime DataCadastro { get; set; }

        [Column("ID_RECURSO_IMPUGNACAO_RESULTADO")]
        public int RecursoImpugnacaoResultadoId { get; set; }

        [Column("ID_PROFISSIONAL")]
        public int ProfissionalId { get; set; }

        [Column("ID_CHAPA_ELEICAO")]
        public int? ChapaEleicaoId { get; set; }

        [Required]
        [Column("NM_ARQUIVO")]
        [MaxLength(200)]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required]
        [Column("NM_FIS_ARQUIVO")]
        [MaxLength(200)]
        public string NomeArquivoFisico { get; set; } = string.Empty;

        // Navigation Properties
        public virtual RecursoImpugnacaoResultado RecursoImpugnacaoResultado { get; set; } = null!;
        public virtual Profissional Profissional { get; set; } = null!;
        public virtual ChapaEleicao? ChapaEleicao { get; set; }

        // Business Methods
        public bool DentroDoPrazo()
        {
            // Verifica se está dentro do prazo para apresentar contrarrazão (5 dias úteis)
            var diasUteis = 0;
            var dataAtual = DataCadastro;
            
            while (diasUteis < 5 && dataAtual < DateTime.Now)
            {
                dataAtual = dataAtual.AddDays(1);
                if (dataAtual.DayOfWeek != DayOfWeek.Saturday && 
                    dataAtual.DayOfWeek != DayOfWeek.Sunday)
                {
                    diasUteis++;
                }
            }
            
            return DateTime.Now <= dataAtual;
        }
    }
}