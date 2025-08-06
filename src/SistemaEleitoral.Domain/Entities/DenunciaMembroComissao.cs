using System;
using SistemaEleitoral.Domain.Common;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Denúncia específica contra membro da comissão eleitoral
    /// </summary>
    public class DenunciaMembroComissaoEleitoral : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID da denúncia principal
        /// </summary>
        public int DenunciaId { get; set; }

        /// <summary>
        /// ID do membro da comissão denunciado
        /// </summary>
        public int MembroComissaoEleitoralId { get; set; }

        /// <summary>
        /// Detalhes específicos da denúncia contra o membro da comissão
        /// </summary>
        public string DetalhesEspecificos { get; set; }

        /// <summary>
        /// Alegações de parcialidade ou conduta inadequada
        /// </summary>
        public string AlegacoesParcialidade { get; set; }

        /// <summary>
        /// Função exercida na comissão
        /// </summary>
        public string FuncaoComissao { get; set; }

        // Navigation Properties
        /// <summary>
        /// Denúncia principal
        /// </summary>
        public virtual Denuncia Denuncia { get; set; }

        /// <summary>
        /// Membro da comissão denunciado
        /// </summary>
        public virtual ComissaoEleitoral MembroComissaoEleitoral { get; set; }
    }
}