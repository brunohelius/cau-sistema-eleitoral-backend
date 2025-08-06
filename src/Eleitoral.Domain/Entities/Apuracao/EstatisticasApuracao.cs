using System;
using System.Collections.Generic;
using System.Linq;

namespace Eleitoral.Domain.Entities.Apuracao
{
    /// <summary>
    /// Entidade que representa as estatísticas detalhadas da apuração
    /// </summary>
    public class EstatisticasApuracao : BaseEntity
    {
        // Propriedades básicas
        public int ResultadoApuracaoId { get; private set; }
        public virtual ResultadoApuracao ResultadoApuracao { get; private set; }
        
        // Estatísticas de participação
        public int TotalEleitoresAptos { get; private set; }
        public int TotalComparecimento { get; private set; }
        public int TotalAbstencoes { get; private set; }
        public decimal PercentualComparecimento { get; private set; }
        public decimal PercentualAbstencao { get; private set; }
        
        // Estatísticas de votos
        public int VotosValidos { get; private set; }
        public int VotosBrancos { get; private set; }
        public int VotosNulos { get; private set; }
        public int VotosAnulados { get; private set; }
        public decimal PercentualVotosValidos { get; private set; }
        public decimal PercentualVotosBrancos { get; private set; }
        public decimal PercentualVotosNulos { get; private set; }
        
        // Estatísticas por região
        public virtual ICollection<EstatisticaRegional> EstatisticasRegionais { get; private set; }
        
        // Estatísticas temporais
        public DateTime InicioVotacao { get; private set; }
        public DateTime FimVotacao { get; private set; }
        public TimeSpan DuracaoVotacao { get; private set; }
        public int PicoVotacaoHora { get; private set; }
        public DateTime HoraPicoVotacao { get; private set; }
        
        // Estatísticas de urnas
        public int TotalUrnas { get; private set; }
        public int UrnasProcessadas { get; private set; }
        public int UrnasPendentes { get; private set; }
        public int UrnasComProblema { get; private set; }
        public decimal PercentualUrnasProcessadas { get; private set; }
        
        // Dados de auditoria
        public DateTime DataGeracao { get; private set; }
        public DateTime UltimaAtualizacao { get; private set; }
        
        // Construtor
        protected EstatisticasApuracao() 
        {
            EstatisticasRegionais = new HashSet<EstatisticaRegional>();
        }
        
        public EstatisticasApuracao(int resultadoApuracaoId, int totalEleitoresAptos, int totalUrnas) : this()
        {
            ResultadoApuracaoId = resultadoApuracaoId;
            TotalEleitoresAptos = totalEleitoresAptos;
            TotalUrnas = totalUrnas;
            DataGeracao = DateTime.Now;
            UltimaAtualizacao = DateTime.Now;
            
            ValidarDados();
        }
        
        // Métodos de negócio
        public void AtualizarEstatisticasVotacao(
            int totalComparecimento,
            int votosValidos,
            int votosBrancos,
            int votosNulos)
        {
            TotalComparecimento = totalComparecimento;
            VotosValidos = votosValidos;
            VotosBrancos = votosBrancos;
            VotosNulos = votosNulos;
            
            CalcularAbstencoes();
            CalcularPercentuais();
            
            UltimaAtualizacao = DateTime.Now;
        }
        
        public void AtualizarEstatisticasUrnas(int urnasProcessadas, int urnasComProblema)
        {
            UrnasProcessadas = urnasProcessadas;
            UrnasComProblema = urnasComProblema;
            UrnasPendentes = TotalUrnas - UrnasProcessadas;
            
            if (TotalUrnas > 0)
                PercentualUrnasProcessadas = (decimal)UrnasProcessadas / TotalUrnas * 100;
                
            UltimaAtualizacao = DateTime.Now;
        }
        
        public void RegistrarPicoVotacao(int quantidadeVotos, DateTime horario)
        {
            if (quantidadeVotos > PicoVotacaoHora)
            {
                PicoVotacaoHora = quantidadeVotos;
                HoraPicoVotacao = horario;
                UltimaAtualizacao = DateTime.Now;
            }
        }
        
        public void DefinirPeriodoVotacao(DateTime inicio, DateTime fim)
        {
            if (fim <= inicio)
                throw new ArgumentException("Fim da votação deve ser posterior ao início.");
                
            InicioVotacao = inicio;
            FimVotacao = fim;
            DuracaoVotacao = fim - inicio;
            UltimaAtualizacao = DateTime.Now;
        }
        
        public void AdicionarEstatisticaRegional(EstatisticaRegional estatistica)
        {
            if (estatistica == null)
                throw new ArgumentNullException(nameof(estatistica));
                
            EstatisticasRegionais.Add(estatistica);
            UltimaAtualizacao = DateTime.Now;
        }
        
        public EstatisticaRegional ObterEstatisticaRegional(string regiao)
        {
            return EstatisticasRegionais.FirstOrDefault(e => e.Regiao == regiao);
        }
        
        public void AnularVotos(int quantidade, string motivo)
        {
            if (quantidade < 0)
                throw new ArgumentException("Quantidade de votos anulados não pode ser negativa.");
                
            VotosAnulados += quantidade;
            VotosValidos = Math.Max(0, VotosValidos - quantidade);
            
            CalcularPercentuais();
            UltimaAtualizacao = DateTime.Now;
        }
        
        // Métodos privados
        private void CalcularAbstencoes()
        {
            TotalAbstencoes = TotalEleitoresAptos - TotalComparecimento;
            
            if (TotalEleitoresAptos > 0)
            {
                PercentualComparecimento = (decimal)TotalComparecimento / TotalEleitoresAptos * 100;
                PercentualAbstencao = (decimal)TotalAbstencoes / TotalEleitoresAptos * 100;
            }
        }
        
        private void CalcularPercentuais()
        {
            if (TotalComparecimento > 0)
            {
                PercentualVotosValidos = (decimal)VotosValidos / TotalComparecimento * 100;
                PercentualVotosBrancos = (decimal)VotosBrancos / TotalComparecimento * 100;
                PercentualVotosNulos = (decimal)VotosNulos / TotalComparecimento * 100;
            }
        }
        
        private void ValidarDados()
        {
            if (ResultadoApuracaoId <= 0)
                throw new ArgumentException("ID do resultado de apuração inválido.");
                
            if (TotalEleitoresAptos <= 0)
                throw new ArgumentException("Total de eleitores aptos deve ser maior que zero.");
                
            if (TotalUrnas <= 0)
                throw new ArgumentException("Total de urnas deve ser maior que zero.");
        }
    }
    
    /// <summary>
    /// Classe que representa estatísticas de uma região específica
    /// </summary>
    public class EstatisticaRegional : BaseEntity
    {
        public int EstatisticasApuracaoId { get; private set; }
        public virtual EstatisticasApuracao EstatisticasApuracao { get; private set; }
        
        public string Regiao { get; private set; }
        public string UF { get; private set; }
        public string Cidade { get; private set; }
        
        public int EleitoresAptos { get; private set; }
        public int Comparecimento { get; private set; }
        public int Abstencoes { get; private set; }
        
        public int VotosValidos { get; private set; }
        public int VotosBrancos { get; private set; }
        public int VotosNulos { get; private set; }
        
        public decimal PercentualComparecimento { get; private set; }
        public decimal PercentualAbstencao { get; private set; }
        
        public int TotalUrnas { get; private set; }
        public int UrnasProcessadas { get; private set; }
        
        protected EstatisticaRegional() { }
        
        public EstatisticaRegional(
            int estatisticasApuracaoId,
            string regiao,
            string uf,
            string cidade,
            int eleitoresAptos,
            int totalUrnas)
        {
            EstatisticasApuracaoId = estatisticasApuracaoId;
            Regiao = regiao;
            UF = uf;
            Cidade = cidade;
            EleitoresAptos = eleitoresAptos;
            TotalUrnas = totalUrnas;
        }
        
        public void AtualizarDados(
            int comparecimento,
            int votosValidos,
            int votosBrancos,
            int votosNulos,
            int urnasProcessadas)
        {
            Comparecimento = comparecimento;
            VotosValidos = votosValidos;
            VotosBrancos = votosBrancos;
            VotosNulos = votosNulos;
            UrnasProcessadas = urnasProcessadas;
            
            Abstencoes = EleitoresAptos - Comparecimento;
            
            if (EleitoresAptos > 0)
            {
                PercentualComparecimento = (decimal)Comparecimento / EleitoresAptos * 100;
                PercentualAbstencao = (decimal)Abstencoes / EleitoresAptos * 100;
            }
        }
    }
}