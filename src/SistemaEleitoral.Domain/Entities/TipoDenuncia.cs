using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Tipos de denúncia disponíveis no sistema eleitoral
    /// </summary>
    public class TipoDenuncia : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// Descrição do tipo de denúncia
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Descricao { get; set; }

        /// <summary>
        /// Indica se o tipo está ativo
        /// </summary>
        public bool Ativo { get; set; } = true;

        /// <summary>
        /// Ordem de exibição
        /// </summary>
        public int Ordem { get; set; }

        // Navigation Properties
        /// <summary>
        /// Denúncias deste tipo
        /// </summary>
        public virtual ICollection<Denuncia> Denuncias { get; set; } = new List<Denuncia>();
    }
}