using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SistemaEleitoral.Domain.Entities.Votacao
{
    /// <summary>
    /// Boletim de urna oficial com assinatura digital e auditoria
    /// </summary>
    public class BoletimUrna : BaseEntity
    {
        // Propriedades Básicas
        public int Id { get; set; }
        public int EleicaoId { get; set; }
        public int? UfId { get; set; }
        public int? SessaoVotacaoId { get; set; }
        public string NumeroBoletim { get; private set; }
        public DateTime DataGeracao { get; set; }
        public TipoBoletim TipoBoletim { get; set; }
        
        // Dados da Eleição
        public string NomeEleicao { get; set; }
        public string TurnoEleicao { get; set; }
        public DateTime DataEleicao { get; set; }
        public string LocalEleicao { get; set; }
        
        // Totalizadores
        public int TotalEleitoresAptos { get; set; }
        public int TotalComparecimento { get; set; }
        public int TotalAbstencoes { get; set; }
        public int TotalVotosValidos { get; set; }
        public int TotalVotosBrancos { get; set; }
        public int TotalVotosNulos { get; set; }
        public int TotalVotosApurados { get; set; }
        
        // Resultados por Chapa
        public string ResultadosChapaJson { get; private set; } // JSON com resultados detalhados
        public int QuantidadeChapas { get; set; }
        public string ChapaVencedora { get; set; }
        public int VotosChapaVencedora { get; set; }
        
        // Segurança e Integridade
        public string HashBoletim { get; private set; }
        public string AssinaturaDigital { get; private set; }
        public string ChavePublicaAssinatura { get; set; }
        public DateTime DataAssinatura { get; set; }
        public string AssinadoPor { get; set; }
        
        // Publicação
        public bool Publicado { get; set; }
        public DateTime? DataPublicacao { get; set; }
        public string UrlPublicacao { get; set; }
        public string CodigoVerificacao { get; private set; }
        
        // Arquivamento
        public bool Arquivado { get; set; }
        public DateTime? DataArquivamento { get; set; }
        public string MotivoArquivamento { get; set; }
        public string LocalArquivamento { get; set; }
        
        // Auditoria
        public string ConteudoCompleto { get; private set; } // JSON completo do boletim
        public int VersaoBoletim { get; set; }
        public bool FoiRetificado { get; set; }
        public string MotivoRetificacao { get; set; }
        public int? BoletimAnteriorId { get; set; }
        
        // Navegação
        public virtual Eleicao Eleicao { get; set; }
        public virtual Uf Uf { get; set; }
        public virtual SessaoVotacaoAvancada SessaoVotacao { get; set; }
        public virtual BoletimUrna BoletimAnterior { get; set; }
        public virtual ICollection<LogAuditoriaBoletim> LogsAuditoria { get; set; }
        
        // Construtor
        public BoletimUrna()
        {
            DataGeracao = DateTime.UtcNow;
            TipoBoletim = TipoBoletim.Parcial;
            VersaoBoletim = 1;
            Publicado = false;
            Arquivado = false;
            FoiRetificado = false;
            LogsAuditoria = new List<LogAuditoriaBoletim>();
        }
        
        // Métodos de Negócio
        
        /// <summary>
        /// Gera boletim de urna oficial
        /// </summary>
        public static BoletimUrna GerarBoletim(
            ResultadoApuracaoAvancado apuracao,
            Eleicao eleicao,
            List<ResultadoChapa> resultadosChapas,
            string responsavel)
        {
            var boletim = new BoletimUrna
            {
                EleicaoId = eleicao.Id,
                UfId = apuracao.UfId,
                NomeEleicao = eleicao.Nome,
                DataEleicao = eleicao.DataEleicao,
                TipoBoletim = apuracao.Status == StatusApuracao.Finalizada ? TipoBoletim.Final : TipoBoletim.Parcial,
                
                // Totalizadores
                TotalEleitoresAptos = apuracao.TotalEleitoresAptos,
                TotalComparecimento = apuracao.TotalVotosApurados,
                TotalAbstencoes = apuracao.TotalAbstencoes,
                TotalVotosValidos = apuracao.TotalVotosValidos,
                TotalVotosBrancos = apuracao.TotalVotosBrancos,
                TotalVotosNulos = apuracao.TotalVotosNulos,
                TotalVotosApurados = apuracao.TotalVotosApurados,
                
                AssinadoPor = responsavel
            };
            
            // Gerar número único do boletim
            boletim.NumeroBoletim = boletim.GerarNumeroBoletim();
            
            // Processar resultados das chapas
            boletim.ProcessarResultadosChapas(resultadosChapas);
            
            // Gerar conteúdo completo
            boletim.GerarConteudoCompleto();
            
            // Gerar hash e assinatura
            boletim.HashBoletim = boletim.GerarHashBoletim();
            boletim.CodigoVerificacao = boletim.GerarCodigoVerificacao();
            
            return boletim;
        }
        
        /// <summary>
        /// Gera número único do boletim
        /// </summary>
        private string GerarNumeroBoletim()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            var tipo = TipoBoletim == TipoBoletim.Final ? "F" : "P";
            return $"BU-{tipo}-{EleicaoId:D4}-{timestamp}-{random}";
        }
        
        /// <summary>
        /// Processa resultados das chapas
        /// </summary>
        private void ProcessarResultadosChapas(List<ResultadoChapa> resultados)
        {
            if (resultados == null || !resultados.Any())
                return;
            
            QuantidadeChapas = resultados.Count;
            
            // Identificar vencedora
            var vencedora = resultados.OrderByDescending(r => r.TotalVotos).FirstOrDefault();
            if (vencedora != null)
            {
                ChapaVencedora = vencedora.NomeChapa;
                VotosChapaVencedora = vencedora.TotalVotos;
            }
            
            // Serializar resultados
            var resultadosSimplificados = resultados.Select(r => new
            {
                Numero = r.NumeroChapa,
                Nome = r.NomeChapa,
                Sigla = r.SiglaChapa,
                Votos = r.TotalVotos,
                Percentual = r.PercentualVotos,
                Posicao = r.Posicao
            }).OrderBy(r => r.Posicao);
            
            ResultadosChapaJson = JsonSerializer.Serialize(resultadosSimplificados, new JsonSerializerOptions { WriteIndented = true });
        }
        
        /// <summary>
        /// Gera conteúdo completo do boletim
        /// </summary>
        private void GerarConteudoCompleto()
        {
            var conteudo = new
            {
                boletim = new
                {
                    numero = NumeroBoletim,
                    tipo = TipoBoletim.ToString(),
                    versao = VersaoBoletim,
                    data_geracao = DataGeracao.ToString("yyyy-MM-dd HH:mm:ss")
                },
                eleicao = new
                {
                    id = EleicaoId,
                    nome = NomeEleicao,
                    turno = TurnoEleicao,
                    data = DataEleicao.ToString("yyyy-MM-dd"),
                    local = LocalEleicao,
                    uf = UfId
                },
                totalizacao = new
                {
                    eleitores_aptos = TotalEleitoresAptos,
                    comparecimento = TotalComparecimento,
                    abstencoes = TotalAbstencoes,
                    percentual_participacao = TotalEleitoresAptos > 0 ? 
                        Math.Round((decimal)TotalComparecimento / TotalEleitoresAptos * 100, 2) : 0,
                    votos = new
                    {
                        total_apurados = TotalVotosApurados,
                        validos = TotalVotosValidos,
                        brancos = TotalVotosBrancos,
                        nulos = TotalVotosNulos
                    }
                },
                resultado = new
                {
                    quantidade_chapas = QuantidadeChapas,
                    chapa_vencedora = ChapaVencedora,
                    votos_vencedora = VotosChapaVencedora,
                    chapas = JsonSerializer.Deserialize<object>(ResultadosChapaJson ?? "{}")
                },
                seguranca = new
                {
                    hash = HashBoletim,
                    codigo_verificacao = CodigoVerificacao,
                    assinado_por = AssinadoPor,
                    data_assinatura = DataAssinatura.ToString("yyyy-MM-dd HH:mm:ss")
                }
            };
            
            ConteudoCompleto = JsonSerializer.Serialize(conteudo, new JsonSerializerOptions { WriteIndented = true });
        }
        
        /// <summary>
        /// Gera hash do boletim
        /// </summary>
        private string GerarHashBoletim()
        {
            var dados = $"{NumeroBoletim}|{EleicaoId}|{TotalVotosApurados}|{TotalVotosValidos}|{ResultadosChapaJson}|{DataGeracao:O}";
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Gera código de verificação
        /// </summary>
        private string GerarCodigoVerificacao()
        {
            var dados = $"{NumeroBoletim}|{HashBoletim}";
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                var hashString = Convert.ToBase64String(hash);
                return hashString.Substring(0, 12).ToUpper();
            }
        }
        
        /// <summary>
        /// Assina digitalmente o boletim
        /// </summary>
        public void AssinarDigitalmente(string chavePrivada, string chavePublica)
        {
            if (!string.IsNullOrEmpty(AssinaturaDigital))
                throw new InvalidOperationException("Boletim já foi assinado");
            
            // Simular assinatura digital (em produção usar certificado real)
            var dadosParaAssinar = $"{HashBoletim}|{ConteudoCompleto}|{NumeroBoletim}";
            
            using (var sha512 = SHA512.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dadosParaAssinar + chavePrivada);
                var hash = sha512.ComputeHash(bytes);
                AssinaturaDigital = Convert.ToBase64String(hash);
            }
            
            ChavePublicaAssinatura = chavePublica;
            DataAssinatura = DateTime.UtcNow;
            
            RegistrarAuditoria("Boletim assinado digitalmente", AssinadoPor);
        }
        
        /// <summary>
        /// Publica o boletim
        /// </summary>
        public void Publicar(string urlPublicacao)
        {
            if (string.IsNullOrEmpty(AssinaturaDigital))
                throw new InvalidOperationException("Boletim deve ser assinado antes da publicação");
            
            if (Publicado)
                throw new InvalidOperationException("Boletim já foi publicado");
            
            Publicado = true;
            DataPublicacao = DateTime.UtcNow;
            UrlPublicacao = urlPublicacao;
            
            RegistrarAuditoria("Boletim publicado", AssinadoPor);
        }
        
        /// <summary>
        /// Arquiva o boletim
        /// </summary>
        public void Arquivar(string motivo, string local)
        {
            if (Arquivado)
                throw new InvalidOperationException("Boletim já foi arquivado");
            
            Arquivado = true;
            DataArquivamento = DateTime.UtcNow;
            MotivoArquivamento = motivo;
            LocalArquivamento = local;
            
            RegistrarAuditoria($"Boletim arquivado: {motivo}", AssinadoPor);
        }
        
        /// <summary>
        /// Retifica o boletim
        /// </summary>
        public BoletimUrna Retificar(string motivo, string responsavel)
        {
            if (!Publicado)
                throw new InvalidOperationException("Apenas boletins publicados podem ser retificados");
            
            var novoBoletim = new BoletimUrna
            {
                EleicaoId = EleicaoId,
                UfId = UfId,
                SessaoVotacaoId = SessaoVotacaoId,
                NomeEleicao = NomeEleicao,
                TurnoEleicao = TurnoEleicao,
                DataEleicao = DataEleicao,
                LocalEleicao = LocalEleicao,
                TipoBoletim = TipoBoletim,
                VersaoBoletim = VersaoBoletim + 1,
                FoiRetificado = true,
                MotivoRetificacao = motivo,
                BoletimAnteriorId = Id,
                AssinadoPor = responsavel
            };
            
            // Copiar totalizadores
            novoBoletim.TotalEleitoresAptos = TotalEleitoresAptos;
            novoBoletim.TotalComparecimento = TotalComparecimento;
            novoBoletim.TotalAbstencoes = TotalAbstencoes;
            novoBoletim.TotalVotosValidos = TotalVotosValidos;
            novoBoletim.TotalVotosBrancos = TotalVotosBrancos;
            novoBoletim.TotalVotosNulos = TotalVotosNulos;
            novoBoletim.TotalVotosApurados = TotalVotosApurados;
            
            // Gerar novo número
            novoBoletim.NumeroBoletim = novoBoletim.GerarNumeroBoletim();
            
            RegistrarAuditoria($"Boletim retificado - Nova versão: {novoBoletim.NumeroBoletim}", responsavel);
            
            return novoBoletim;
        }
        
        /// <summary>
        /// Verifica integridade do boletim
        /// </summary>
        public bool VerificarIntegridade()
        {
            // Verificar hash
            var hashCalculado = GerarHashBoletim();
            if (hashCalculado != HashBoletim)
            {
                RegistrarAuditoria("Falha na verificação de integridade - Hash inválido", "Sistema");
                return false;
            }
            
            // Verificar totalizadores
            var totalCalculado = TotalVotosValidos + TotalVotosBrancos + TotalVotosNulos;
            if (totalCalculado != TotalVotosApurados)
            {
                RegistrarAuditoria("Falha na verificação de integridade - Totais não conferem", "Sistema");
                return false;
            }
            
            // Verificar abstencões
            var abstencaoCalculada = TotalEleitoresAptos - TotalComparecimento;
            if (abstencaoCalculada != TotalAbstencoes)
            {
                RegistrarAuditoria("Falha na verificação de integridade - Abstencões não conferem", "Sistema");
                return false;
            }
            
            RegistrarAuditoria("Integridade verificada com sucesso", "Sistema");
            return true;
        }
        
        /// <summary>
        /// Registra auditoria do boletim
        /// </summary>
        private void RegistrarAuditoria(string acao, string responsavel)
        {
            var log = new LogAuditoriaBoletim
            {
                BoletimUrnaId = Id,
                DataHora = DateTime.UtcNow,
                Acao = acao,
                Responsavel = responsavel,
                VersaoBoletim = VersaoBoletim,
                HashNoMomento = HashBoletim
            };
            
            LogsAuditoria?.Add(log);
        }
        
        /// <summary>
        /// Obtém URL de verificação online
        /// </summary>
        public string ObterUrlVerificacao()
        {
            return $"https://eleicoes.caubr.gov.br/boletim/{NumeroBoletim}/{CodigoVerificacao}";
        }
    }
    
    /// <summary>
    /// Tipo de boletim de urna
    /// </summary>
    public enum TipoBoletim
    {
        Parcial,
        Final,
        Retificacao,
        Complementar
    }
    
    /// <summary>
    /// Log de auditoria do boletim
    /// </summary>
    public class LogAuditoriaBoletim
    {
        public int Id { get; set; }
        public int BoletimUrnaId { get; set; }
        public DateTime DataHora { get; set; }
        public string Acao { get; set; }
        public string Responsavel { get; set; }
        public int VersaoBoletim { get; set; }
        public string HashNoMomento { get; set; }
        
        public virtual BoletimUrna BoletimUrna { get; set; }
    }
}