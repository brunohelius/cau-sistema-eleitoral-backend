using System;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Histórico de situações/status da denúncia
    /// Permite rastreabilidade completa do workflow
    /// </summary>
    public class DenunciaSituacao : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID da denúncia
        /// </summary>
        public int DenunciaId { get; set; }

        /// <summary>
        /// Status anterior da denúncia
        /// </summary>
        public StatusDenuncia? StatusAnterior { get; set; }

        /// <summary>
        /// Status atual da denúncia
        /// </summary>
        public StatusDenuncia StatusAtual { get; set; }

        /// <summary>
        /// Data/hora da mudança de status
        /// </summary>
        public DateTime DataMudanca { get; set; }

        /// <summary>
        /// ID do usuário responsável pela mudança
        /// </summary>
        public int UsuarioResponsavelId { get; set; }

        /// <summary>
        /// Justificativa/observação da mudança
        /// </summary>
        [MaxLength(1000)]
        public string Observacao { get; set; }

        /// <summary>
        /// Dados adicionais da transição (JSON)
        /// </summary>
        public string DadosAdicionais { get; set; }

        // Navigation Properties
        /// <summary>
        /// Denúncia relacionada
        /// </summary>
        public virtual Denuncia Denuncia { get; set; }

        /// <summary>
        /// Usuário responsável pela mudança
        /// </summary>
        public virtual Usuario UsuarioResponsavel { get; set; }
    }
}