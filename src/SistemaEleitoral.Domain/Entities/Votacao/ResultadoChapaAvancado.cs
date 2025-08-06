using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaEleitoral.Domain.Entities.Votacao
{
    /// <summary>
    /// Resultado individual de cada chapa na eleição
    /// </summary>
    public class ResultadoChapa : BaseEntity
    {
        // Propriedades Básicas
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        public int ChapaId { get; set; }
        public int NumeroChapa { get; set; }
        public string NomeChapa { get; set; }
        public string SiglaChapa { get; set; }
        
        // Contagem de Votos
        public int TotalVotos { get; set; }
        public decimal PercentualVotos { get; set; }
        public int VotosPorUf { get; set; }
        public int VotosPorCategoria { get; set; }
        
        // Posicionamento
        public int Posicao { get; set; }
        public StatusResultadoChapa Status { get; set; }
        public int DiferencaParaPrimeiro { get; set; }
        public int DiferencaParaAnterior { get; set; }
        public int DiferencaParaProximo { get; set; }
        
        // Distribuição
        public string DistribuicaoVotosJson { get; set; } // JSON com distribuição por UF
        public string DistribuicaoTemporalJson { get; set; } // JSON com votos por hora
        public string DistribuicaoCategoriaJson { get; set; } // JSON com votos por categoria profissional
        
        // Validação
        public bool Validado { get; set; }
        public DateTime? DataValidacao { get; set; }
        public string ValidadoPor { get; set; }
        public bool TemIrregularidade { get; set; }
        public string DescricaoIrregularidade { get; set; }
        
        // Auditoria
        public string HashResultado { get; set; }
        public DateTime DataApuracao { get; set; }
        public int VersaoApuracao { get; set; }
        
        // Navegação
        public virtual ResultadoApuracaoAvancado ResultadoApuracao { get; set; }
        public virtual ChapaEleicao Chapa { get; set; }
        public virtual ICollection<VotoChapa> Votos { get; set; }
        public virtual ICollection<ContestacaoResultado> Contestacoes { get; set; }
        
        // Construtor
        public ResultadoChapa()
        {
            Status = StatusResultadoChapa.Provisorio;
            TotalVotos = 0;
            PercentualVotos = 0;
            Posicao = 0;
            Validado = false;
            TemIrregularidade = false;
            DataApuracao = DateTime.UtcNow;
            VersaoApuracao = 1;
            Votos = new List<VotoChapa>();
            Contestacoes = new List<ContestacaoResultado>();
        }
        
        // Métodos de Negócio
        
        /// <summary>
        /// Atualiza total de votos da chapa
        /// </summary>
        public void AtualizarVotos(int novoTotalVotos, int totalVotosValidos)
        {
            if (Status == StatusResultadoChapa.Homologado)
                throw new InvalidOperationException("Resultado homologado não pode ser alterado");
            
            TotalVotos = novoTotalVotos;
            
            // Calcular percentual
            if (totalVotosValidos > 0)
            {
                PercentualVotos = (decimal)TotalVotos / totalVotosValidos * 100;
            }
            
            // Atualizar hash
            HashResultado = GerarHashResultado();
            
            // Marcar como não validado
            Validado = false;
            DataValidacao = null;
            ValidadoPor = null;
        }
        
        /// <summary>
        /// Define a posição da chapa na classificação
        /// </summary>
        public void DefinirPosicao(int posicao, int diferencaPrimeiro = 0, int diferencaAnterior = 0, int diferencaProximo = 0)
        {
            Posicao = posicao;
            DiferencaParaPrimeiro = diferencaPrimeiro;
            DiferencaParaAnterior = diferencaAnterior;
            DiferencaParaProximo = diferencaProximo;
            
            // Determinar status baseado na posição
            if (posicao == 1)
            {
                Status = StatusResultadoChapa.Vencedor;
            }
            else if (posicao == 2)
            {
                Status = StatusResultadoChapa.SegundoLugar;
            }
            else
            {
                Status = StatusResultadoChapa.Classificado;
            }
        }
        
        /// <summary>
        /// Valida o resultado da chapa
        /// </summary>
        public void ValidarResultado(string responsavel)
        {
            if (TemIrregularidade)
                throw new InvalidOperationException($"Resultado com irregularidade não pode ser validado: {DescricaoIrregularidade}");
            
            Validado = true;
            DataValidacao = DateTime.UtcNow;
            ValidadoPor = responsavel;
            
            if (Status == StatusResultadoChapa.Provisorio)
            {
                Status = StatusResultadoChapa.Oficial;
            }
        }
        
        /// <summary>
        /// Registra irregularidade no resultado
        /// </summary>
        public void RegistrarIrregularidade(string descricao)
        {
            TemIrregularidade = true;
            DescricaoIrregularidade = descricao;
            Validado = false;
            Status = StatusResultadoChapa.Contestado;
        }
        
        /// <summary>
        /// Remove irregularidade
        /// </summary>
        public void RemoverIrregularidade(string justificativa)
        {
            TemIrregularidade = false;
            DescricaoIrregularidade = $"Irregularidade removida: {justificativa}";
            Status = StatusResultadoChapa.Provisorio;
        }
        
        /// <summary>
        /// Homologa o resultado
        /// </summary>
        public void Homologar()
        {
            if (!Validado)
                throw new InvalidOperationException("Resultado deve ser validado antes da homologação");
            
            if (TemIrregularidade)
                throw new InvalidOperationException("Resultado com irregularidade não pode ser homologado");
            
            Status = StatusResultadoChapa.Homologado;
        }
        
        /// <summary>
        /// Adiciona contestação ao resultado
        /// </summary>
        public void AdicionarContestacao(string motivo, string solicitante, string documentos = null)
        {
            var contestacao = new ContestacaoResultado
            {
                ResultadoChapaId = Id,
                Motivo = motivo,
                Solicitante = solicitante,
                Documentos = documentos,
                DataContestacao = DateTime.UtcNow,
                Status = StatusContestacao.Pendente
            };
            
            Contestacoes?.Add(contestacao);
            Status = StatusResultadoChapa.Contestado;
        }
        
        /// <summary>
        /// Atualiza distribuição de votos por UF
        /// </summary>
        public void AtualizarDistribuicaoUf(Dictionary<string, int> distribuicao)
        {
            DistribuicaoVotosJson = System.Text.Json.JsonSerializer.Serialize(distribuicao);
            VotosPorUf = distribuicao.Count;
        }
        
        /// <summary>
        /// Atualiza distribuição temporal de votos
        /// </summary>
        public void AtualizarDistribuicaoTemporal(Dictionary<string, int> distribuicao)
        {
            DistribuicaoTemporalJson = System.Text.Json.JsonSerializer.Serialize(distribuicao);
        }
        
        /// <summary>
        /// Atualiza distribuição por categoria profissional
        /// </summary>
        public void AtualizarDistribuicaoCategoria(Dictionary<string, int> distribuicao)
        {
            DistribuicaoCategoriaJson = System.Text.Json.JsonSerializer.Serialize(distribuicao);
            VotosPorCategoria = distribuicao.Count;
        }
        
        /// <summary>
        /// Gera hash do resultado para integridade
        /// </summary>
        private string GerarHashResultado()
        {
            var dados = $"{ChapaId}|{NumeroChapa}|{TotalVotos}|{PercentualVotos}|{Posicao}|{DataApuracao:O}";
            
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Verifica integridade do resultado
        /// </summary>
        public bool VerificarIntegridade()
        {
            var hashCalculado = GerarHashResultado();
            return hashCalculado == HashResultado;
        }
        
        /// <summary>
        /// Obtém resumo do resultado
        /// </summary>
        public ResumoResultadoChapa ObterResumo()
        {
            return new ResumoResultadoChapa
            {
                NumeroChapa = NumeroChapa,
                NomeChapa = NomeChapa,
                SiglaChapa = SiglaChapa,
                TotalVotos = TotalVotos,
                PercentualVotos = PercentualVotos,
                Posicao = Posicao,
                Status = Status.ToString(),
                Validado = Validado,
                TemIrregularidade = TemIrregularidade,
                DiferencaParaPrimeiro = DiferencaParaPrimeiro,
                QuantidadeContestacoes = Contestacoes?.Count(c => c.Status == StatusContestacao.Pendente) ?? 0
            };
        }
    }
    
    /// <summary>
    /// Status do resultado da chapa
    /// </summary>
    public enum StatusResultadoChapa
    {
        Provisorio,
        Oficial,
        Contestado,
        Homologado,
        Vencedor,
        SegundoLugar,
        Classificado,
        Desclassificado
    }
    
    /// <summary>
    /// Contestação do resultado
    /// </summary>
    public class ContestacaoResultado
    {
        public int Id { get; set; }
        public int ResultadoChapaId { get; set; }
        public string Motivo { get; set; }
        public string Solicitante { get; set; }
        public string Documentos { get; set; }
        public DateTime DataContestacao { get; set; }
        public DateTime? DataAnalise { get; set; }
        public string ParecerAnalise { get; set; }
        public string AnalisadoPor { get; set; }
        public StatusContestacao Status { get; set; }
        
        public virtual ResultadoChapa ResultadoChapa { get; set; }
    }
    
    /// <summary>
    /// Status da contestação
    /// </summary>
    public enum StatusContestacao
    {
        Pendente,
        EmAnalise,
        Deferida,
        Indeferida,
        Arquivada
    }
    
    /// <summary>
    /// Resumo do resultado da chapa
    /// </summary>
    public class ResumoResultadoChapa
    {
        public int NumeroChapa { get; set; }
        public string NomeChapa { get; set; }
        public string SiglaChapa { get; set; }
        public int TotalVotos { get; set; }
        public decimal PercentualVotos { get; set; }
        public int Posicao { get; set; }
        public string Status { get; set; }
        public bool Validado { get; set; }
        public bool TemIrregularidade { get; set; }
        public int DiferencaParaPrimeiro { get; set; }
        public int QuantidadeContestacoes { get; set; }
    }
}