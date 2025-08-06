using System;
using System.ComponentModel.DataAnnotations;
using SistemaEleitoral.Domain.Common;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Arquivos anexados às denúncias
    /// </summary>
    public class ArquivoDenuncia : AuditableEntity
    {
        public int Id { get; set; }

        /// <summary>
        /// ID da denúncia
        /// </summary>
        public int DenunciaId { get; set; }

        /// <summary>
        /// Nome original do arquivo
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string NomeOriginal { get; set; }

        /// <summary>
        /// Nome do arquivo no sistema de arquivos
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
        /// Tipo MIME do arquivo
        /// </summary>
        [MaxLength(100)]
        public string TipoMime { get; set; }

        /// <summary>
        /// Tamanho do arquivo em bytes
        /// </summary>
        public long TamanhoBytes { get; set; }

        /// <summary>
        /// Descrição/observações do arquivo
        /// </summary>
        [MaxLength(500)]
        public string Descricao { get; set; }

        /// <summary>
        /// Tipo do documento (prova, defesa, alegação, etc.)
        /// </summary>
        [MaxLength(50)]
        public string TipoDocumento { get; set; }

        /// <summary>
        /// Hash MD5 do arquivo para integridade
        /// </summary>
        [MaxLength(32)]
        public string HashMd5 { get; set; }

        /// <summary>
        /// Indica se o arquivo está ativo
        /// </summary>
        public bool Ativo { get; set; } = true;

        // Navigation Properties
        /// <summary>
        /// Denúncia relacionada
        /// </summary>
        public virtual Denuncia Denuncia { get; set; }
    }
}