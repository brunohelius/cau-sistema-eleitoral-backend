using System;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Arquivos relacionados aos recursos de denúncia
    /// </summary>
    public class ArquivoRecursoDenuncia : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID do recurso
        /// </summary>
        public int RecursoDenunciaId { get; set; }

        /// <summary>
        /// Nome original do arquivo
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string NomeOriginal { get; set; }

        /// <summary>
        /// Nome do arquivo no sistema
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string NomeArquivo { get; set; }

        /// <summary>
        /// Caminho completo do arquivo
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string CaminhoArquivo { get; set; }

        /// <summary>
        /// Tipo MIME
        /// </summary>
        [MaxLength(100)]
        public string TipoMime { get; set; }

        /// <summary>
        /// Tamanho em bytes
        /// </summary>
        public long TamanhoBytes { get; set; }

        /// <summary>
        /// Tipo do documento (recurso, contra-razão, etc.)
        /// </summary>
        [MaxLength(50)]
        public string TipoDocumento { get; set; }

        /// <summary>
        /// Descrição do arquivo
        /// </summary>
        [MaxLength(500)]
        public string Descricao { get; set; }

        /// <summary>
        /// Hash MD5 para integridade
        /// </summary>
        [MaxLength(32)]
        public string HashMd5 { get; set; }

        /// <summary>
        /// Indica se está ativo
        /// </summary>
        public bool Ativo { get; set; } = true;

        // Navigation Properties
        /// <summary>
        /// Recurso relacionado
        /// </summary>
        public virtual RecursoDenuncia RecursoDenuncia { get; set; }
    }
}