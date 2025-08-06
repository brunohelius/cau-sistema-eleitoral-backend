using System;

namespace Eleitoral.Domain.Entities.Apuracao
{
    /// <summary>
    /// Entidade que representa o log de eventos durante a apuração
    /// </summary>
    public class LogApuracao : BaseEntity
    {
        // Propriedades básicas
        public int ResultadoApuracaoId { get; private set; }
        public virtual ResultadoApuracao ResultadoApuracao { get; private set; }
        
        public DateTime DataHora { get; private set; }
        public string Descricao { get; private set; }
        public TipoLogApuracao Tipo { get; private set; }
        
        public string Usuario { get; private set; }
        public string IpOrigem { get; private set; }
        
        // Dados adicionais
        public string DadosAnteriores { get; private set; }
        public string DadosNovos { get; private set; }
        public string Observacoes { get; private set; }
        
        // Construtor
        protected LogApuracao() { }
        
        public LogApuracao(int resultadoApuracaoId, string descricao, TipoLogApuracao tipo)
        {
            ResultadoApuracaoId = resultadoApuracaoId;
            Descricao = descricao;
            Tipo = tipo;
            DataHora = DateTime.Now;
            
            ValidarDados();
        }
        
        // Métodos de negócio
        public void DefinirUsuario(string usuario, string ipOrigem)
        {
            Usuario = usuario;
            IpOrigem = ipOrigem;
        }
        
        public void RegistrarAlteracao(string dadosAnteriores, string dadosNovos)
        {
            DadosAnteriores = dadosAnteriores;
            DadosNovos = dadosNovos;
        }
        
        public void AdicionarObservacao(string observacao)
        {
            if (string.IsNullOrWhiteSpace(observacao))
                return;
                
            Observacoes = observacao;
        }
        
        // Métodos privados
        private void ValidarDados()
        {
            if (ResultadoApuracaoId <= 0)
                throw new ArgumentException("ID do resultado de apuração inválido.");
                
            if (string.IsNullOrWhiteSpace(Descricao))
                throw new ArgumentException("Descrição do log é obrigatória.");
        }
    }
}