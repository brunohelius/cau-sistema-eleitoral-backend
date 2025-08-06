using System;
using System.Collections.Generic;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa um tipo de processo eleitoral
    /// </summary>
    public class TipoProcesso : BaseEntity
    {
        /// <summary>
        /// Nome do tipo de processo
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Descrição do tipo de processo
        /// </summary>
        public string? Descricao { get; set; }

        /// <summary>
        /// Código do tipo de processo
        /// </summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>
        /// Indica se o tipo de processo está ativo
        /// </summary>
        public bool Ativo { get; set; } = true;

        /// <summary>
        /// Ordem de exibição
        /// </summary>
        public int Ordem { get; set; }

        /// <summary>
        /// Data de criação
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime? DataAtualizacao { get; set; }

        // Relacionamentos
        /// <summary>
        /// Eleições deste tipo de processo
        /// </summary>
        public virtual ICollection<Eleicao> Eleicoes { get; set; } = new List<Eleicao>();
    }
}