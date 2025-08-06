using System;
using SistemaEleitoral.Domain.Common;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Denúncia específica contra chapa eleitoral
    /// </summary>
    public class DenunciaChapa : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID da denúncia principal
        /// </summary>
        public int DenunciaId { get; set; }

        /// <summary>
        /// ID da chapa eleitoral denunciada
        /// </summary>
        public int ChapaEleicaoId { get; set; }

        /// <summary>
        /// Detalhes específicos da denúncia contra a chapa
        /// </summary>
        public string DetalhesEspecificos { get; set; }

        /// <summary>
        /// Infrações alegadas
        /// </summary>
        public string InfracoesAlegadas { get; set; }

        // Navigation Properties
        /// <summary>
        /// Denúncia principal
        /// </summary>
        public virtual Denuncia Denuncia { get; set; }

        /// <summary>
        /// Chapa eleitoral denunciada
        /// </summary>
        public virtual ChapaEleicao ChapaEleicao { get; set; }
    }
}