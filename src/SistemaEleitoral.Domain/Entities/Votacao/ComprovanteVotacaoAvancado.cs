using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using QRCoder;

namespace SistemaEleitoral.Domain.Entities.Votacao
{
    /// <summary>
    /// Comprovante de votação criptograficamente seguro
    /// </summary>
    public class ComprovanteVotacaoAvancado : BaseEntity
    {
        // Propriedades Básicas
        public int Id { get; set; }
        public int VotoId { get; set; }
        public string ProtocoloComprovante { get; private set; }
        public string CodigoVerificacao { get; private set; }
        public DateTime DataEmissao { get; set; }
        public DateTime DataExpiracao { get; set; }
        
        // Segurança
        public string HashComprovante { get; private set; }
        public string HashVoto { get; set; }
        public string AssinaturaDigital { get; private set; }
        public string ChavePublica { get; set; }
        
        // Dados do Comprovante
        public string NomeEleicao { get; set; }
        public string UfEleicao { get; set; }
        public DateTime DataHoraVoto { get; set; }
        public string TipoVoto { get; set; }
        public string NumeroSessao { get; set; }
        
        // Controle de Verificação
        public int QuantidadeVerificacoes { get; set; }
        public DateTime? UltimaVerificacao { get; set; }
        public bool Valido { get; set; }
        public bool Invalidado { get; set; }
        public DateTime? DataInvalidacao { get; set; }
        public string MotivoInvalidacao { get; set; }
        
        // Formatos de Saída
        public string ConteudoJson { get; private set; }
        public string ConteudoHtml { get; private set; }
        public string ConteudoTexto { get; private set; }
        public byte[] QrCode { get; private set; }
        
        // Navegação
        public virtual VotoEleitoral Voto { get; set; }
        public virtual ICollection<LogVerificacaoComprovante> LogsVerificacao { get; set; }
        
        // Construtor
        public ComprovanteVotacaoAvancado()
        {
            DataEmissao = DateTime.UtcNow;
            DataExpiracao = DateTime.UtcNow.AddYears(5);
            Valido = true;
            Invalidado = false;
            QuantidadeVerificacoes = 0;
            LogsVerificacao = new List<LogVerificacaoComprovante>();
        }
        
        // Métodos de Negócio
        
        /// <summary>
        /// Gera um novo comprovante de votação
        /// </summary>
        public static ComprovanteVotacaoAvancado GerarComprovante(
            VotoEleitoral voto,
            string nomeEleicao,
            string ufEleicao,
            string numeroSessao)
        {
            var comprovante = new ComprovanteVotacaoAvancado
            {
                VotoId = voto.Id,
                HashVoto = voto.HashVoto,
                NomeEleicao = nomeEleicao,
                UfEleicao = ufEleicao,
                DataHoraVoto = voto.DataHoraVoto,
                TipoVoto = voto.TipoVoto.ToString(),
                NumeroSessao = numeroSessao
            };
            
            // Gerar protocolo único
            comprovante.ProtocoloComprovante = comprovante.GerarProtocolo();
            
            // Gerar código de verificação
            comprovante.CodigoVerificacao = comprovante.GerarCodigoVerificacao();
            
            // Gerar hash do comprovante
            comprovante.HashComprovante = comprovante.GerarHashComprovante();
            
            // Assinar digitalmente
            comprovante.AssinaturaDigital = comprovante.AssinarComprovante();
            
            // Gerar conteúdos em diferentes formatos
            comprovante.GerarConteudos();
            
            // Gerar QR Code
            comprovante.GerarQrCode();
            
            return comprovante;
        }
        
        /// <summary>
        /// Gera protocolo único do comprovante
        /// </summary>
        private string GerarProtocolo()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            var hash = GerarHashCurto($"{VotoId}{timestamp}{random}");
            return $"COMP-{timestamp}-{hash}-{random}";
        }
        
        /// <summary>
        /// Gera código de verificação
        /// </summary>
        private string GerarCodigoVerificacao()
        {
            var dados = $"{ProtocoloComprovante}|{VotoId}|{DataEmissao:O}";
            return GerarHashCurto(dados);
        }
        
        /// <summary>
        /// Gera hash do comprovante
        /// </summary>
        private string GerarHashComprovante()
        {
            var dados = $"{ProtocoloComprovante}|{CodigoVerificacao}|{HashVoto}|{DataHoraVoto:O}|{TipoVoto}";
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Assina digitalmente o comprovante
        /// </summary>
        private string AssinarComprovante()
        {
            // Simulação de assinatura digital
            // Em produção, usar certificado digital real
            var dados = $"{HashComprovante}|{ProtocoloComprovante}|{CodigoVerificacao}";
            
            using (var sha512 = SHA512.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dados + "CHAVE_PRIVADA_SISTEMA");
                var hash = sha512.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Gera hash curto para códigos
        /// </summary>
        private string GerarHashCurto(string dados)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                var hashString = Convert.ToBase64String(hash);
                return hashString.Substring(0, 8).ToUpper();
            }
        }
        
        /// <summary>
        /// Gera conteúdos em diferentes formatos
        /// </summary>
        private void GerarConteudos()
        {
            // JSON
            var jsonData = new
            {
                protocolo = ProtocoloComprovante,
                codigo_verificacao = CodigoVerificacao,
                eleicao = NomeEleicao,
                uf = UfEleicao,
                data_voto = DataHoraVoto.ToString("dd/MM/yyyy HH:mm:ss"),
                tipo_voto = TipoVoto,
                sessao = NumeroSessao,
                hash_comprovante = HashComprovante,
                assinatura = AssinaturaDigital,
                validade = DataExpiracao.ToString("dd/MM/yyyy")
            };
            ConteudoJson = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions { WriteIndented = true });
            
            // HTML
            ConteudoHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Comprovante de Votação</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background: #0066cc; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 20px; border: 1px solid #ddd; margin-top: 20px; }}
        .field {{ margin: 10px 0; }}
        .label {{ font-weight: bold; }}
        .footer {{ margin-top: 30px; padding: 20px; background: #f0f0f0; text-align: center; }}
        .qrcode {{ text-align: center; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>COMPROVANTE DE VOTAÇÃO</h1>
        <h2>{NomeEleicao}</h2>
    </div>
    <div class='content'>
        <div class='field'><span class='label'>Protocolo:</span> {ProtocoloComprovante}</div>
        <div class='field'><span class='label'>Código de Verificação:</span> {CodigoVerificacao}</div>
        <div class='field'><span class='label'>UF:</span> {UfEleicao}</div>
        <div class='field'><span class='label'>Data/Hora do Voto:</span> {DataHoraVoto:dd/MM/yyyy HH:mm:ss}</div>
        <div class='field'><span class='label'>Tipo de Voto:</span> {TipoVoto}</div>
        <div class='field'><span class='label'>Sessão:</span> {NumeroSessao}</div>
        <div class='field'><span class='label'>Válido até:</span> {DataExpiracao:dd/MM/yyyy}</div>
    </div>
    <div class='footer'>
        <p>Este comprovante é criptograficamente seguro e pode ser verificado online.</p>
        <p>Hash: {HashComprovante}</p>
        <p>Assinatura Digital: {AssinaturaDigital.Substring(0, 20)}...</p>
    </div>
</body>
</html>";
            
            // Texto Simples
            ConteudoTexto = $@"
=====================================
       COMPROVANTE DE VOTAÇÃO
=====================================
Eleição: {NomeEleicao}
UF: {UfEleicao}
-------------------------------------
Protocolo: {ProtocoloComprovante}
Código de Verificação: {CodigoVerificacao}
Data/Hora do Voto: {DataHoraVoto:dd/MM/yyyy HH:mm:ss}
Tipo de Voto: {TipoVoto}
Sessão: {NumeroSessao}
Válido até: {DataExpiracao:dd/MM/yyyy}
-------------------------------------
Hash: {HashComprovante}
Assinatura: {AssinaturaDigital.Substring(0, 20)}...
=====================================
Este comprovante é criptograficamente 
seguro e pode ser verificado online.
=====================================";
        }
        
        /// <summary>
        /// Gera QR Code do comprovante
        /// </summary>
        private void GerarQrCode()
        {
            var qrData = $"https://eleicoes.caubr.gov.br/verificar/{ProtocoloComprovante}/{CodigoVerificacao}";
            
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    QrCode = qrCode.GetGraphic(20);
                }
            }
        }
        
        /// <summary>
        /// Verifica a autenticidade do comprovante
        /// </summary>
        public bool VerificarAutenticidade()
        {
            if (Invalidado)
                return false;
                
            if (DateTime.UtcNow > DataExpiracao)
            {
                Invalidar("Comprovante expirado");
                return false;
            }
            
            // Verificar hash
            var hashCalculado = GerarHashComprovante();
            if (hashCalculado != HashComprovante)
            {
                RegistrarVerificacao(false, "Hash inválido");
                return false;
            }
            
            // Verificar assinatura
            var assinaturaCalculada = AssinarComprovante();
            if (assinaturaCalculada != AssinaturaDigital)
            {
                RegistrarVerificacao(false, "Assinatura inválida");
                return false;
            }
            
            RegistrarVerificacao(true, "Comprovante válido");
            return true;
        }
        
        /// <summary>
        /// Invalida o comprovante
        /// </summary>
        public void Invalidar(string motivo)
        {
            if (Invalidado)
                return;
                
            Invalidado = true;
            Valido = false;
            DataInvalidacao = DateTime.UtcNow;
            MotivoInvalidacao = motivo;
            
            RegistrarVerificacao(false, $"Comprovante invalidado: {motivo}");
        }
        
        /// <summary>
        /// Registra verificação do comprovante
        /// </summary>
        public void RegistrarVerificacao(bool sucesso, string detalhes, string ip = null)
        {
            QuantidadeVerificacoes++;
            UltimaVerificacao = DateTime.UtcNow;
            
            var log = new LogVerificacaoComprovante
            {
                ComprovanteId = Id,
                DataHora = DateTime.UtcNow,
                Sucesso = sucesso,
                Detalhes = detalhes,
                IpOrigem = ip
            };
            
            LogsVerificacao?.Add(log);
        }
        
        /// <summary>
        /// Obtém o comprovante em formato específico
        /// </summary>
        public string ObterComprovante(FormatoComprovante formato)
        {
            return formato switch
            {
                FormatoComprovante.Json => ConteudoJson,
                FormatoComprovante.Html => ConteudoHtml,
                FormatoComprovante.Texto => ConteudoTexto,
                _ => ConteudoTexto
            };
        }
        
        /// <summary>
        /// Obtém URL de verificação online
        /// </summary>
        public string ObterUrlVerificacao()
        {
            return $"https://eleicoes.caubr.gov.br/verificar/{ProtocoloComprovante}/{CodigoVerificacao}";
        }
    }
    
    /// <summary>
    /// Formato de saída do comprovante
    /// </summary>
    public enum FormatoComprovante
    {
        Json,
        Html,
        Texto,
        Pdf
    }
    
    /// <summary>
    /// Log de verificação do comprovante
    /// </summary>
    public class LogVerificacaoComprovante
    {
        public int Id { get; set; }
        public int ComprovanteId { get; set; }
        public DateTime DataHora { get; set; }
        public bool Sucesso { get; set; }
        public string Detalhes { get; set; }
        public string IpOrigem { get; set; }
        
        public virtual ComprovanteVotacaoAvancado Comprovante { get; set; }
    }
}