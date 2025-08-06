using System;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Votos individuais dos membros da comissão no julgamento da denúncia
    /// </summary>
    public class VotoJulgamentoDenuncia : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID do julgamento
        /// </summary>
        public int JulgamentoDenunciaId { get; set; }

        /// <summary>
        /// ID do membro da comissão votante
        /// </summary>
        public int MembroComissaoEleitoralId { get; set; }

        /// <summary>
        /// Tipo do voto (favor, contra, abstenção)
        /// </summary>
        public TipoVoto TipoVoto { get; set; }

        /// <summary>
        /// Fundamentação do voto
        /// </summary>
        public string Fundamentacao { get; set; }

        /// <summary>
        /// Data/hora do voto
        /// </summary>
        public DateTime DataVoto { get; set; }

        /// <summary>
        /// Observações do votante
        /// </summary>
        [MaxLength(500)]
        public string Observacoes { get; set; }

        /// <summary>
        /// Indica se houve declaração de impedimento
        /// </summary>
        public bool DeclarouImpedimento { get; set; }

        /// <summary>
        /// Motivo do impedimento (se declarado)
        /// </summary>
        [MaxLength(500)]
        public string MotivoImpedimento { get; set; }

        // Navigation Properties
        /// <summary>
        /// Julgamento relacionado
        /// </summary>
        public virtual JulgamentoDenuncia JulgamentoDenuncia { get; set; }

        /// <summary>
        /// Membro da comissão votante
        /// </summary>
        public virtual ComissaoEleitoral MembroComissaoEleitoral { get; set; }
    }
}