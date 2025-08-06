using System;
using SistemaEleitoral.Domain.Common;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Denúncia específica contra membro de chapa eleitoral
    /// </summary>
    public class DenunciaMembroChapa : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID da denúncia principal
        /// </summary>
        public int DenunciaId { get; set; }

        /// <summary>
        /// ID do membro de chapa denunciado
        /// </summary>
        public int MembroChapaId { get; set; }

        /// <summary>
        /// Detalhes específicos da denúncia contra o membro
        /// </summary>
        public string DetalhesEspecificos { get; set; }

        /// <summary>
        /// Condutas alegadas como irregulares
        /// </summary>
        public string CondutasIrregulares { get; set; }

        /// <summary>
        /// Cargo do membro na chapa
        /// </summary>
        public string CargoNaChapa { get; set; }

        // Navigation Properties
        /// <summary>
        /// Denúncia principal
        /// </summary>
        public virtual Denuncia Denuncia { get; set; }

        /// <summary>
        /// Membro de chapa denunciado
        /// </summary>
        public virtual MembroChapa MembroChapa { get; set; }
    }
}