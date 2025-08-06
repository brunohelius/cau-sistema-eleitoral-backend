using System;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Encaminhamentos da denúncia entre instâncias e órgãos
    /// </summary>
    public class EncaminhamentoDenuncia : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID da denúncia
        /// </summary>
        public int DenunciaId { get; set; }

        /// <summary>
        /// Órgão/instância de origem
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string OrgaoOrigem { get; set; }

        /// <summary>
        /// Órgão/instância de destino
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string OrgaoDestino { get; set; }

        /// <summary>
        /// Data do encaminhamento
        /// </summary>
        public DateTime DataEncaminhamento { get; set; }

        /// <summary>
        /// Motivo do encaminhamento
        /// </summary>
        [Required]
        public string MotivoEncaminhamento { get; set; }

        /// <summary>
        /// Observações do encaminhamento
        /// </summary>
        public string Observacoes { get; set; }

        /// <summary>
        /// ID do usuário responsável pelo encaminhamento
        /// </summary>
        public int UsuarioEncaminhamentoId { get; set; }

        /// <summary>
        /// Data de recebimento pelo órgão destino
        /// </summary>
        public DateTime? DataRecebimento { get; set; }

        /// <summary>
        /// ID do usuário que recebeu
        /// </summary>
        public int? UsuarioRecebimentoId { get; set; }

        /// <summary>
        /// Status do encaminhamento
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Enviado";

        /// <summary>
        /// Parecer sobre o encaminhamento
        /// </summary>
        public string Parecer { get; set; }

        // Navigation Properties
        /// <summary>
        /// Denúncia relacionada
        /// </summary>
        public virtual Denuncia Denuncia { get; set; }

        /// <summary>
        /// Usuário responsável pelo encaminhamento
        /// </summary>
        public virtual Usuario UsuarioEncaminhamento { get; set; }

        /// <summary>
        /// Usuário que recebeu o encaminhamento
        /// </summary>
        public virtual Usuario UsuarioRecebimento { get; set; }
    }
}