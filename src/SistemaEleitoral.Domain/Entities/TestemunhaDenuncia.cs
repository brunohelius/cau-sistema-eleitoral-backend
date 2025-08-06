using System;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Testemunhas relacionadas às denúncias
    /// </summary>
    public class TestemunhaDenuncia : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID da denúncia
        /// </summary>
        public int DenunciaId { get; set; }

        /// <summary>
        /// Nome completo da testemunha
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string NomeCompleto { get; set; }

        /// <summary>
        /// CPF da testemunha
        /// </summary>
        [MaxLength(14)]
        public string Cpf { get; set; }

        /// <summary>
        /// Email para contato
        /// </summary>
        [MaxLength(100)]
        public string Email { get; set; }

        /// <summary>
        /// Telefone para contato
        /// </summary>
        [MaxLength(20)]
        public string Telefone { get; set; }

        /// <summary>
        /// Endereço da testemunha
        /// </summary>
        [MaxLength(500)]
        public string Endereco { get; set; }

        /// <summary>
        /// Profissão da testemunha
        /// </summary>
        [MaxLength(100)]
        public string Profissao { get; set; }

        /// <summary>
        /// Relação da testemunha com os fatos
        /// </summary>
        [MaxLength(500)]
        public string RelacaoComFatos { get; set; }

        /// <summary>
        /// Resumo do que a testemunha pode atestar
        /// </summary>
        public string ResumoTestemunho { get; set; }

        /// <summary>
        /// Indica se a testemunha foi notificada
        /// </summary>
        public bool Notificada { get; set; }

        /// <summary>
        /// Data da notificação
        /// </summary>
        public DateTime? DataNotificacao { get; set; }

        /// <summary>
        /// Indica se a testemunha compareceu
        /// </summary>
        public bool Compareceu { get; set; }

        /// <summary>
        /// Data do comparecimento
        /// </summary>
        public DateTime? DataComparecimento { get; set; }

        /// <summary>
        /// Observações sobre o testemunho
        /// </summary>
        public string Observacoes { get; set; }

        // Navigation Properties
        /// <summary>
        /// Denúncia relacionada
        /// </summary>
        public virtual Denuncia Denuncia { get; set; }
    }
}