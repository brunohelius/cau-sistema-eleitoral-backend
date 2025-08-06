using System;
using System.Collections.Generic;

namespace Eleitoral.Domain.Entities.Apuracao
{
    /// <summary>
    /// Entidade que representa o boletim de urna com os resultados de votação
    /// </summary>
    public class BoletimUrna : BaseEntity
    {
        // Propriedades básicas
        public int ResultadoApuracaoId { get; private set; }
        public virtual ResultadoApuracao ResultadoApuracao { get; private set; }
        
        public int NumeroUrna { get; private set; }
        public string CodigoIdentificacao { get; private set; }
        public string LocalVotacao { get; private set; }
        public string Secao { get; private set; }
        public string Zona { get; private set; }
        
        public int TotalEleitoresUrna { get; private set; }
        public int TotalVotantes { get; private set; }
        public int VotosBrancos { get; private set; }
        public int VotosNulos { get; private set; }
        
        public DateTime DataHoraAbertura { get; private set; }
        public DateTime DataHoraEncerramento { get; private set; }
        public DateTime DataHoraProcessamento { get; private set; }
        
        public StatusBoletim Status { get; private set; }
        public string HashBoletim { get; private set; }
        
        // Relacionamentos
        public virtual ICollection<VotoChapa> VotosChapas { get; private set; }
        
        // Propriedades adicionais
        public string ArquivoBoletim { get; private set; }
        public bool Conferido { get; private set; }
        public string ConferidoPor { get; private set; }
        public DateTime? DataConferencia { get; private set; }
        public string Observacoes { get; private set; }
        
        // Total de urnas na eleição (para cálculo de percentual)
        public int TotalUrnasEleicao { get; private set; }
        
        // Construtor
        protected BoletimUrna() 
        {
            VotosChapas = new HashSet<VotoChapa>();
        }
        
        public BoletimUrna(
            int resultadoApuracaoId,
            int numeroUrna,
            string codigoIdentificacao,
            string localVotacao,
            string secao,
            string zona,
            int totalEleitoresUrna,
            int totalUrnasEleicao) : this()
        {
            ResultadoApuracaoId = resultadoApuracaoId;
            NumeroUrna = numeroUrna;
            CodigoIdentificacao = codigoIdentificacao;
            LocalVotacao = localVotacao;
            Secao = secao;
            Zona = zona;
            TotalEleitoresUrna = totalEleitoresUrna;
            TotalUrnasEleicao = totalUrnasEleicao;
            DataHoraProcessamento = DateTime.Now;
            Status = StatusBoletim.Pendente;
            
            ValidarDados();
        }
        
        // Métodos de negócio
        public void RegistrarVotacao(
            DateTime dataHoraAbertura,
            DateTime dataHoraEncerramento,
            int totalVotantes,
            int votosBrancos,
            int votosNulos)
        {
            if (Status != StatusBoletim.Pendente)
                throw new InvalidOperationException("Boletim já foi processado.");
                
            DataHoraAbertura = dataHoraAbertura;
            DataHoraEncerramento = dataHoraEncerramento;
            TotalVotantes = totalVotantes;
            VotosBrancos = votosBrancos;
            VotosNulos = votosNulos;
            
            ValidarTotais();
        }
        
        public void AdicionarVotoChapa(int chapaId, int quantidadeVotos)
        {
            if (Status != StatusBoletim.Pendente && Status != StatusBoletim.EmProcessamento)
                throw new InvalidOperationException("Boletim não pode receber votos neste status.");
                
            if (quantidadeVotos < 0)
                throw new ArgumentException("Quantidade de votos não pode ser negativa.");
                
            var votoChapa = new VotoChapa(this.Id, chapaId, quantidadeVotos);
            VotosChapas.Add(votoChapa);
        }
        
        public void ProcessarBoletim()
        {
            if (Status != StatusBoletim.Pendente)
                throw new InvalidOperationException("Boletim já foi processado.");
                
            Status = StatusBoletim.EmProcessamento;
            
            // Validar integridade dos dados
            ValidarIntegridade();
            
            // Gerar hash do boletim
            GerarHash();
            
            Status = StatusBoletim.Processado;
            DataHoraProcessamento = DateTime.Now;
        }
        
        public void MarcarComoConferido(string conferidoPor)
        {
            if (Status != StatusBoletim.Processado)
                throw new InvalidOperationException("Apenas boletins processados podem ser conferidos.");
                
            Conferido = true;
            ConferidoPor = conferidoPor;
            DataConferencia = DateTime.Now;
            Status = StatusBoletim.Conferido;
        }
        
        public void RejeitarBoletim(string motivo)
        {
            Status = StatusBoletim.Rejeitado;
            Observacoes = $"Boletim rejeitado: {motivo}";
            DataHoraProcessamento = DateTime.Now;
        }
        
        public void AnexarArquivo(string caminhoArquivo)
        {
            if (string.IsNullOrWhiteSpace(caminhoArquivo))
                throw new ArgumentException("Caminho do arquivo não pode ser vazio.");
                
            ArquivoBoletim = caminhoArquivo;
        }
        
        public void AdicionarObservacao(string observacao)
        {
            if (string.IsNullOrWhiteSpace(observacao))
                return;
                
            if (string.IsNullOrWhiteSpace(Observacoes))
                Observacoes = observacao;
            else
                Observacoes += $"\n{DateTime.Now:dd/MM/yyyy HH:mm} - {observacao}";
        }
        
        // Métodos privados
        private void ValidarDados()
        {
            if (ResultadoApuracaoId <= 0)
                throw new ArgumentException("ID do resultado de apuração inválido.");
                
            if (NumeroUrna <= 0)
                throw new ArgumentException("Número da urna inválido.");
                
            if (string.IsNullOrWhiteSpace(CodigoIdentificacao))
                throw new ArgumentException("Código de identificação é obrigatório.");
                
            if (string.IsNullOrWhiteSpace(LocalVotacao))
                throw new ArgumentException("Local de votação é obrigatório.");
                
            if (TotalEleitoresUrna <= 0)
                throw new ArgumentException("Total de eleitores da urna deve ser maior que zero.");
        }
        
        private void ValidarTotais()
        {
            if (TotalVotantes > TotalEleitoresUrna)
                throw new InvalidOperationException("Total de votantes não pode ser maior que o total de eleitores.");
                
            if (DataHoraEncerramento <= DataHoraAbertura)
                throw new InvalidOperationException("Data/hora de encerramento deve ser posterior à abertura.");
        }
        
        private void ValidarIntegridade()
        {
            var totalVotosChapas = 0;
            foreach (var voto in VotosChapas)
            {
                totalVotosChapas += voto.QuantidadeVotos;
            }
            
            var totalCalculado = totalVotosChapas + VotosBrancos + VotosNulos;
            
            if (totalCalculado != TotalVotantes)
                throw new InvalidOperationException($"Total de votos não confere. Esperado: {TotalVotantes}, Calculado: {totalCalculado}");
        }
        
        private void GerarHash()
        {
            var dados = $"{NumeroUrna}|{CodigoIdentificacao}|{TotalVotantes}|{VotosBrancos}|{VotosNulos}";
            
            foreach (var voto in VotosChapas)
            {
                dados += $"|{voto.ChapaId}:{voto.QuantidadeVotos}";
            }
            
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                HashBoletim = Convert.ToBase64String(hash);
            }
        }
    }
    
    /// <summary>
    /// Classe que representa os votos de uma chapa em um boletim de urna
    /// </summary>
    public class VotoChapa : BaseEntity
    {
        public int BoletimUrnaId { get; private set; }
        public virtual BoletimUrna BoletimUrna { get; private set; }
        
        public int ChapaId { get; private set; }
        public int QuantidadeVotos { get; private set; }
        
        protected VotoChapa() { }
        
        public VotoChapa(int boletimUrnaId, int chapaId, int quantidadeVotos)
        {
            BoletimUrnaId = boletimUrnaId;
            ChapaId = chapaId;
            QuantidadeVotos = quantidadeVotos;
        }
    }
    
    // Enum
    public enum StatusBoletim
    {
        Pendente = 0,
        EmProcessamento = 1,
        Processado = 2,
        Conferido = 3,
        Rejeitado = 4,
        Corrigido = 5
    }
}