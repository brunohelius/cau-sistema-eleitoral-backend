using System;
using Eleitoral.Domain.Entities.Chapas;

namespace Eleitoral.Domain.Entities.Apuracao
{
    /// <summary>
    /// Entidade que representa o resultado de uma chapa específica na apuração
    /// </summary>
    public class ResultadoChapa : BaseEntity
    {
        // Propriedades básicas
        public int ResultadoApuracaoId { get; private set; }
        public virtual ResultadoApuracao ResultadoApuracao { get; private set; }
        
        public int ChapaId { get; private set; }
        public virtual ChapaEleicao Chapa { get; private set; }
        
        public int TotalVotos { get; private set; }
        public decimal PercentualVotos { get; private set; }
        
        public int? Posicao { get; private set; }
        public bool Eleita { get; private set; }
        
        // Dados estatísticos
        public int VotosPorUrna { get; private set; }
        public int VotosPorRegiao { get; private set; }
        public DateTime UltimaAtualizacao { get; private set; }
        
        // Construtor
        protected ResultadoChapa() { }
        
        public ResultadoChapa(int resultadoApuracaoId, int chapaId)
        {
            ResultadoApuracaoId = resultadoApuracaoId;
            ChapaId = chapaId;
            TotalVotos = 0;
            PercentualVotos = 0;
            Eleita = false;
            UltimaAtualizacao = DateTime.Now;
            
            ValidarDados();
        }
        
        // Métodos de negócio
        public void AdicionarVotos(int quantidade)
        {
            if (quantidade < 0)
                throw new ArgumentException("Quantidade de votos não pode ser negativa.");
                
            TotalVotos += quantidade;
            UltimaAtualizacao = DateTime.Now;
        }
        
        public void RemoverVotos(int quantidade)
        {
            if (quantidade < 0)
                throw new ArgumentException("Quantidade de votos não pode ser negativa.");
                
            if (TotalVotos - quantidade < 0)
                throw new InvalidOperationException("Total de votos não pode ficar negativo.");
                
            TotalVotos -= quantidade;
            UltimaAtualizacao = DateTime.Now;
        }
        
        public void AtualizarPercentual(int totalVotosValidos)
        {
            if (totalVotosValidos <= 0)
            {
                PercentualVotos = 0;
                return;
            }
            
            PercentualVotos = (decimal)TotalVotos / totalVotosValidos * 100;
            PercentualVotos = Math.Round(PercentualVotos, 2);
        }
        
        public void DefinirPosicao(int posicao)
        {
            if (posicao <= 0)
                throw new ArgumentException("Posição deve ser maior que zero.");
                
            Posicao = posicao;
            UltimaAtualizacao = DateTime.Now;
        }
        
        public void MarcarComoVencedora()
        {
            Eleita = true;
            if (!Posicao.HasValue || Posicao.Value != 1)
                Posicao = 1;
                
            UltimaAtualizacao = DateTime.Now;
        }
        
        public void DesmarcarComoVencedora()
        {
            Eleita = false;
            UltimaAtualizacao = DateTime.Now;
        }
        
        public void AtualizarEstatisticas(int votosPorUrna, int votosPorRegiao)
        {
            VotosPorUrna = votosPorUrna;
            VotosPorRegiao = votosPorRegiao;
            UltimaAtualizacao = DateTime.Now;
        }
        
        // Métodos privados
        private void ValidarDados()
        {
            if (ResultadoApuracaoId <= 0)
                throw new ArgumentException("ID do resultado de apuração inválido.");
                
            if (ChapaId <= 0)
                throw new ArgumentException("ID da chapa inválido.");
        }
    }
}