using System;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Denúncia específica contra terceiros (não membros de chapa ou comissão)
    /// </summary>
    public class DenunciaOutro : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID da denúncia principal
        /// </summary>
        public int DenunciaId { get; set; }

        /// <summary>
        /// Nome da pessoa/entidade denunciada
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string NomeDenunciado { get; set; }

        /// <summary>
        /// CPF/CNPJ da pessoa/entidade denunciada
        /// </summary>
        [MaxLength(20)]
        public string CpfCnpjDenunciado { get; set; }

        /// <summary>
        /// Cargo ou função exercida
        /// </summary>
        [MaxLength(100)]
        public string CargoFuncao { get; set; }

        /// <summary>
        /// Instituição/empresa vinculada
        /// </summary>
        [MaxLength(200)]
        public string InstituicaoVinculada { get; set; }

        /// <summary>
        /// Detalhes específicos da denúncia contra terceiros
        /// </summary>
        public string DetalhesEspecificos { get; set; }

        /// <summary>
        /// Relação com o processo eleitoral
        /// </summary>
        public string RelacaoProcessoEleitoral { get; set; }

        /// <summary>
        /// Endereço da pessoa/entidade denunciada
        /// </summary>
        [MaxLength(500)]
        public string Endereco { get; set; }

        /// <summary>
        /// Telefone para contato
        /// </summary>
        [MaxLength(50)]
        public string Telefone { get; set; }

        /// <summary>
        /// Email para contato
        /// </summary>
        [MaxLength(100)]
        public string Email { get; set; }

        // Navigation Properties
        /// <summary>
        /// Denúncia principal
        /// </summary>
        public virtual Denuncia Denuncia { get; set; }
    }
}