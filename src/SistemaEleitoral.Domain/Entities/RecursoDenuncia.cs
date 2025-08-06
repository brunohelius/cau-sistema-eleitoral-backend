using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Recursos interpostos contra decisões de denúncia
    /// </summary>
    public class RecursoDenuncia : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID da denúncia
        /// </summary>
        public int DenunciaId { get; set; }

        /// <summary>
        /// ID do julgamento que está sendo recorrido
        /// </summary>
        public int JulgamentoDenunciaId { get; set; }

        /// <summary>
        /// Protocolo do recurso
        /// </summary>
        [MaxLength(50)]
        public string Protocolo { get; set; }

        /// <summary>
        /// Status do recurso
        /// </summary>
        public StatusRecurso Status { get; set; } = StatusRecurso.Protocolado;

        /// <summary>
        /// Data de interposição do recurso
        /// </summary>
        public DateTime DataInterposicao { get; set; }

        /// <summary>
        /// Prazo para contra-razões
        /// </summary>
        public DateTime? PrazoContraRazoes { get; set; }

        /// <summary>
        /// Data de recebimento das contra-razões
        /// </summary>
        public DateTime? DataRecebimentoContraRazoes { get; set; }

        /// <summary>
        /// Fundamentação do recurso
        /// </summary>
        [Required]
        public string Fundamentacao { get; set; }

        /// <summary>
        /// Pedido específico do recurso
        /// </summary>
        public string Pedido { get; set; }

        /// <summary>
        /// Contra-razões apresentadas
        /// </summary>
        public string ContraRazoes { get; set; }

        /// <summary>
        /// Data do julgamento do recurso
        /// </summary>
        public DateTime? DataJulgamento { get; set; }

        /// <summary>
        /// Decisão do recurso
        /// </summary>
        public string DecisaoRecurso { get; set; }

        /// <summary>
        /// Resultado do recurso (provido, não provido, etc.)
        /// </summary>
        public string ResultadoRecurso { get; set; }

        /// <summary>
        /// Observações sobre o recurso
        /// </summary>
        public string Observacoes { get; set; }

        /// <summary>
        /// Relator designado para o recurso
        /// </summary>
        public int? RelatorRecursoId { get; set; }

        // Navigation Properties
        /// <summary>
        /// Denúncia relacionada
        /// </summary>
        public virtual Denuncia Denuncia { get; set; }

        /// <summary>
        /// Julgamento que está sendo recorrido
        /// </summary>
        public virtual JulgamentoDenuncia JulgamentoDenuncia { get; set; }

        /// <summary>
        /// Relator do recurso
        /// </summary>
        public virtual ComissaoEleitoral RelatorRecurso { get; set; }

        /// <summary>
        /// Arquivos do recurso
        /// </summary>
        public virtual ICollection<ArquivoRecursoDenuncia> Arquivos { get; set; } = new List<ArquivoRecursoDenuncia>();

        /// <summary>
        /// Julgamento do recurso (segunda instância)
        /// </summary>
        public virtual JulgamentoDenuncia JulgamentoRecurso { get; set; }
    }
}