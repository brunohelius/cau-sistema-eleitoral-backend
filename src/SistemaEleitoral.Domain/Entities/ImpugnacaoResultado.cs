using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa a impugnação de resultado eleitoral
    /// </summary>
    [Table("TB_PEDIDO_IMPUGNACAO_RESULTADO", Schema = "eleitoral")]
    public class ImpugnacaoResultado
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [Column("NARRACAO_FATOS")]
        public string NarracaoFatos { get; set; } = string.Empty;

        [Column("NUMERO")]
        public int Numero { get; set; }

        [Column("DT_CADASTRO")]
        public DateTime DataCadastro { get; set; }

        [Column("ID_CAU_BR")]
        public int? CauBrId { get; set; }

        [Required]
        [Column("NM_ARQUIVO")]
        [MaxLength(200)]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required]
        [Column("NM_FIS_ARQUIVO")]
        [MaxLength(200)]
        public string NomeArquivoFisico { get; set; } = string.Empty;

        [Column("ID_PROFISSIONAL_INCLUSAO")]
        public int ProfissionalId { get; set; }

        [Column("ID_STATUS_IMPUGNACAO_RESULTADO")]
        public int StatusId { get; set; }

        [Column("ID_CALENDARIO")]
        public int CalendarioId { get; set; }

        // Navigation Properties
        public virtual Filial? CauBr { get; set; }
        public virtual Profissional Profissional { get; set; } = null!;
        public virtual StatusImpugnacaoResultado Status { get; set; } = null!;
        public virtual Calendario Calendario { get; set; } = null!;
        public virtual JulgamentoAlegacaoImpugResultado? JulgamentoAlegacao { get; set; }
        public virtual JulgamentoRecursoImpugResultado? JulgamentoRecurso { get; set; }
        public virtual ICollection<AlegacaoImpugnacaoResultado> Alegacoes { get; set; } = new List<AlegacaoImpugnacaoResultado>();
        public virtual ICollection<RecursoImpugnacaoResultado> Recursos { get; set; } = new List<RecursoImpugnacaoResultado>();
        public virtual ICollection<ContrarrazaoImpugnacaoResultado> Contrarrazoes { get; set; } = new List<ContrarrazaoImpugnacaoResultado>();

        // Business Methods
        public string GerarProtocolo()
        {
            return $"IMP-RES/{CalendarioId:D4}/{DateTime.Now.Year}/{Numero:D6}";
        }

        public bool PodeRecorrer()
        {
            return StatusId == (int)StatusImpugnacaoResultadoEnum.Julgada;
        }

        public bool PodeApresentarAlegacao()
        {
            return StatusId == (int)StatusImpugnacaoResultadoEnum.EmAnalise ||
                   StatusId == (int)StatusImpugnacaoResultadoEnum.AguardandoAlegacao;
        }

        public void AtualizarStatus(StatusImpugnacaoResultadoEnum novoStatus)
        {
            StatusId = (int)novoStatus;
        }
    }

    public enum StatusImpugnacaoResultadoEnum
    {
        EmAnalise = 1,
        AguardandoAlegacao = 2,
        AlegacaoRecebida = 3,
        AguardandoJulgamento = 4,
        Julgada = 5,
        EmRecurso = 6,
        RecursoJulgado = 7,
        Arquivada = 8,
        Finalizada = 9
    }
}