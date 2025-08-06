using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaEleitoral.Domain.Entities
{
    [Table("tb_chapas_documentos", Schema = "public")]
    public class ChapaDocumento : BaseEntity
    {
        [Column("chapa_eleicao_id")]
        public int ChapaEleicaoId { get; set; }
        public virtual ChapaEleicao ChapaEleicao { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        [Column("tipo_documento")]
        public string TipoDocumento { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        [Column("nome_arquivo")]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        [Column("caminho_arquivo")]
        public string CaminhoArquivo { get; set; } = string.Empty;

        [Column("tamanho_arquivo")]
        public long TamanhoArquivo { get; set; }

        [MaxLength(100)]
        [Column("mime_type")]
        public string? MimeType { get; set; }

        [Column("data_upload")]
        public DateTime DataUpload { get; set; }

        [MaxLength(100)]
        [Column("usuario_upload")]
        public string? UsuarioUpload { get; set; }

        [Column("validado")]
        public bool Validado { get; set; } = false;

        [Column("data_validacao")]
        public DateTime? DataValidacao { get; set; }

        [MaxLength(100)]
        [Column("usuario_validacao")]
        public string? UsuarioValidacao { get; set; }

        [MaxLength(500)]
        [Column("observacoes")]
        public string? Observacoes { get; set; }
    }
}