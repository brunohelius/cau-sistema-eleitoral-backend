using System;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;

namespace SistemaEleitoral.Domain.Entities
{
    public class DocumentoEleitoral : AuditableEntity
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public TipoDocumentoEleitoral TipoDocumento { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string CaminhoArquivo { get; set; }
        public string NomeArquivo { get; set; }
        public string ContentType { get; set; }
        public long TamanhoBytes { get; set; }
        public string HashArquivo { get; set; }
        public int? EleicaoId { get; set; }
        public int? ChapaEleicaoId { get; set; }
        public int? DenunciaId { get; set; }
        public int? ImpugnacaoId { get; set; }
        public int? JulgamentoId { get; set; }
        public int? ProfissionalId { get; set; }
        public DateTime DataUpload { get; set; }
        public DateTime? DataAssinatura { get; set; }
        public string AssinadoPor { get; set; }
        public string AssinaturaDigital { get; set; }
        public bool Publico { get; set; }
        public string QRCodeData { get; set; }
        public string UrlPublica { get; set; }
        public DateTime? DataExpiracao { get; set; }
        
        // Navigation Properties
        public virtual Eleicao Eleicao { get; set; }
        public virtual ChapaEleicao ChapaEleicao { get; set; }
        public virtual Denuncia Denuncia { get; set; }
        public virtual Impugnacao Impugnacao { get; set; }
        public virtual Julgamento Julgamento { get; set; }
        public virtual Profissional Profissional { get; set; }

        // Business Methods
        public void AssinarDigitalmente(string assinante, string assinaturaDigital)
        {
            DataAssinatura = DateTime.UtcNow;
            AssinadoPor = assinante;
            AssinaturaDigital = assinaturaDigital;
        }

        public void GerarQRCode(string baseUrl)
        {
            var validationUrl = $"{baseUrl}/documento/validar/{Codigo}";
            QRCodeData = validationUrl;
        }

        public void TornarPublico(int diasExpiracao = 30)
        {
            Publico = true;
            DataExpiracao = DateTime.UtcNow.AddDays(diasExpiracao);
            UrlPublica = $"/documentos/publico/{Codigo}";
        }

        public void RevogarAcessoPublico()
        {
            Publico = false;
            DataExpiracao = null;
            UrlPublica = null;
        }

        public bool EstaExpirado()
        {
            return DataExpiracao.HasValue && DateTime.UtcNow > DataExpiracao.Value;
        }

        public string GerarCodigo()
        {
            var tipoAbrev = TipoDocumento switch
            {
                TipoDocumentoEleitoral.Diploma => "DIP",
                TipoDocumentoEleitoral.TermoPosse => "POS",
                TipoDocumentoEleitoral.AtaReuniao => "ATA",
                TipoDocumentoEleitoral.Decisao => "DEC",
                TipoDocumentoEleitoral.Recurso => "REC",
                TipoDocumentoEleitoral.Impugnacao => "IMP",
                TipoDocumentoEleitoral.Denuncia => "DEN",
                TipoDocumentoEleitoral.Notificacao => "NOT",
                TipoDocumentoEleitoral.Edital => "EDI",
                TipoDocumentoEleitoral.Relatorio => "REL",
                _ => "DOC"
            };

            return $"{tipoAbrev}{DateTime.Now:yyyyMMdd}{Id:D6}";
        }
    }

    public class ArquivoUpload : AuditableEntity
    {
        public int Id { get; set; }
        public string NomeOriginal { get; set; }
        public string NomeArmazenado { get; set; }
        public string CaminhoRelativo { get; set; }
        public string ContentType { get; set; }
        public long TamanhoBytes { get; set; }
        public string HashMD5 { get; set; }
        public string HashSHA256 { get; set; }
        public int? UsuarioId { get; set; }
        public string TipoEntidade { get; set; }
        public int? EntidadeId { get; set; }
        public DateTime DataUpload { get; set; }
        public bool Temporario { get; set; }
        public DateTime? DataExpiracao { get; set; }
        public bool Processado { get; set; }
        public string StatusProcessamento { get; set; }
        
        // Navigation Properties
        public virtual Usuario Usuario { get; set; }

        // Business Methods
        public void MarcarComoProcessado()
        {
            Processado = true;
            StatusProcessamento = "Processado com sucesso";
        }

        public void MarcarComoErro(string erro)
        {
            Processado = false;
            StatusProcessamento = $"Erro: {erro}";
        }

        public bool DeveSerRemovido()
        {
            return Temporario && DataExpiracao.HasValue && DateTime.UtcNow > DataExpiracao.Value;
        }
    }
}