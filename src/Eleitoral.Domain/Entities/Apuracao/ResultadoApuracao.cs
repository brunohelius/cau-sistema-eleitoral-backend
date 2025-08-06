using System;
using System.Collections.Generic;
using System.Linq;
using Eleitoral.Domain.Entities.Eleicoes;
using Eleitoral.Domain.Entities.Chapas;

namespace Eleitoral.Domain.Entities.Apuracao
{
    /// <summary>
    /// Entidade que representa o resultado da apuração de uma eleição
    /// </summary>
    public class ResultadoApuracao : BaseEntity
    {
        // Propriedades básicas
        public int EleicaoId { get; private set; }
        public virtual Eleicao Eleicao { get; private set; }
        
        public DateTime InicioApuracao { get; private set; }
        public DateTime? FimApuracao { get; private set; }
        
        public int TotalEleitores { get; private set; }
        public int TotalVotantes { get; private set; }
        public int VotosBrancos { get; private set; }
        public int VotosNulos { get; private set; }
        public int VotosValidos { get; private set; }
        
        public decimal PercentualParticipacao { get; private set; }
        public decimal PercentualApuracao { get; private set; }
        
        public StatusApuracao Status { get; private set; }
        
        // Relacionamentos
        public virtual ICollection<ResultadoChapa> ResultadosChapas { get; private set; }
        public virtual ICollection<LogApuracao> LogsApuracao { get; private set; }
        public virtual ICollection<BoletimUrna> BoletinsUrna { get; private set; }
        
        // Propriedades de auditoria
        public string HashApuracao { get; private set; }
        public bool Auditado { get; private set; }
        public DateTime? DataAuditoria { get; private set; }
        public string AuditorId { get; private set; }
        
        // Construtor
        protected ResultadoApuracao() 
        {
            ResultadosChapas = new HashSet<ResultadoChapa>();
            LogsApuracao = new HashSet<LogApuracao>();
            BoletinsUrna = new HashSet<BoletimUrna>();
        }
        
        public ResultadoApuracao(int eleicaoId, int totalEleitores) : this()
        {
            EleicaoId = eleicaoId;
            TotalEleitores = totalEleitores;
            InicioApuracao = DateTime.Now;
            Status = StatusApuracao.EmAndamento;
            PercentualApuracao = 0;
            
            ValidarDados();
        }
        
        // Métodos de negócio
        public void IniciarApuracao()
        {
            if (Status != StatusApuracao.NaoIniciada)
                throw new InvalidOperationException("Apuração já foi iniciada.");
                
            InicioApuracao = DateTime.Now;
            Status = StatusApuracao.EmAndamento;
            
            AdicionarLog("Apuração iniciada", TipoLogApuracao.Inicio);
        }
        
        public void AtualizarContagem(int votosChapa, int chapaId, int numeroUrna)
        {
            if (Status != StatusApuracao.EmAndamento)
                throw new InvalidOperationException("Apuração não está em andamento.");
                
            var resultadoChapa = ResultadosChapas.FirstOrDefault(r => r.ChapaId == chapaId);
            if (resultadoChapa == null)
            {
                resultadoChapa = new ResultadoChapa(this.Id, chapaId);
                ResultadosChapas.Add(resultadoChapa);
            }
            
            resultadoChapa.AdicionarVotos(votosChapa);
            RecalcularTotais();
            
            AdicionarLog($"Adicionados {votosChapa} votos para chapa {chapaId} da urna {numeroUrna}", 
                        TipoLogApuracao.ContagemVotos);
        }
        
        public void AdicionarVotosBrancosNulos(int brancos, int nulos, int numeroUrna)
        {
            if (Status != StatusApuracao.EmAndamento)
                throw new InvalidOperationException("Apuração não está em andamento.");
                
            VotosBrancos += brancos;
            VotosNulos += nulos;
            RecalcularTotais();
            
            AdicionarLog($"Adicionados {brancos} votos brancos e {nulos} votos nulos da urna {numeroUrna}", 
                        TipoLogApuracao.ContagemVotos);
        }
        
        public void ProcessarBoletimUrna(BoletimUrna boletim)
        {
            if (Status != StatusApuracao.EmAndamento)
                throw new InvalidOperationException("Apuração não está em andamento.");
                
            if (BoletinsUrna.Any(b => b.NumeroUrna == boletim.NumeroUrna))
                throw new InvalidOperationException($"Boletim da urna {boletim.NumeroUrna} já foi processado.");
                
            BoletinsUrna.Add(boletim);
            
            // Processar votos do boletim
            foreach (var votoChapa in boletim.VotosChapas)
            {
                AtualizarContagem(votoChapa.QuantidadeVotos, votoChapa.ChapaId, boletim.NumeroUrna);
            }
            
            AdicionarVotosBrancosNulos(boletim.VotosBrancos, boletim.VotosNulos, boletim.NumeroUrna);
            
            // Atualizar percentual de apuração
            PercentualApuracao = (decimal)BoletinsUrna.Count / boletim.TotalUrnasEleicao * 100;
            
            AdicionarLog($"Boletim da urna {boletim.NumeroUrna} processado", TipoLogApuracao.ProcessamentoBoletim);
        }
        
        public void FinalizarApuracao()
        {
            if (Status != StatusApuracao.EmAndamento)
                throw new InvalidOperationException("Apuração não está em andamento.");
                
            FimApuracao = DateTime.Now;
            Status = StatusApuracao.Finalizada;
            PercentualApuracao = 100;
            
            // Calcular e definir vencedores
            CalcularVencedores();
            
            // Gerar hash de auditoria
            GerarHashApuracao();
            
            AdicionarLog("Apuração finalizada", TipoLogApuracao.Fim);
        }
        
        public void Auditar(string auditorId)
        {
            if (Status != StatusApuracao.Finalizada)
                throw new InvalidOperationException("Apenas apurações finalizadas podem ser auditadas.");
                
            Auditado = true;
            DataAuditoria = DateTime.Now;
            AuditorId = auditorId;
            
            // Verificar integridade dos dados
            if (!VerificarIntegridade())
                throw new InvalidOperationException("Falha na verificação de integridade dos dados.");
                
            AdicionarLog($"Apuração auditada por {auditorId}", TipoLogApuracao.Auditoria);
        }
        
        public void Reabrir(string motivo)
        {
            if (Status != StatusApuracao.Finalizada)
                throw new InvalidOperationException("Apenas apurações finalizadas podem ser reabertas.");
                
            Status = StatusApuracao.Reaberta;
            FimApuracao = null;
            Auditado = false;
            DataAuditoria = null;
            AuditorId = null;
            
            AdicionarLog($"Apuração reaberta. Motivo: {motivo}", TipoLogApuracao.Reabertura);
        }
        
        // Métodos privados
        private void RecalcularTotais()
        {
            TotalVotantes = ResultadosChapas.Sum(r => r.TotalVotos) + VotosBrancos + VotosNulos;
            VotosValidos = ResultadosChapas.Sum(r => r.TotalVotos);
            
            if (TotalEleitores > 0)
                PercentualParticipacao = (decimal)TotalVotantes / TotalEleitores * 100;
        }
        
        private void CalcularVencedores()
        {
            var chapasOrdenadas = ResultadosChapas
                .OrderByDescending(r => r.TotalVotos)
                .ThenBy(r => r.ChapaId)
                .ToList();
                
            for (int i = 0; i < chapasOrdenadas.Count; i++)
            {
                chapasOrdenadas[i].DefinirPosicao(i + 1);
                
                if (i == 0)
                {
                    chapasOrdenadas[i].MarcarComoVencedora();
                }
            }
        }
        
        private void GerarHashApuracao()
        {
            var dados = $"{EleicaoId}|{TotalVotantes}|{VotosValidos}|{VotosBrancos}|{VotosNulos}";
            foreach (var resultado in ResultadosChapas.OrderBy(r => r.ChapaId))
            {
                dados += $"|{resultado.ChapaId}:{resultado.TotalVotos}";
            }
            
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                HashApuracao = Convert.ToBase64String(hash);
            }
        }
        
        private bool VerificarIntegridade()
        {
            // Verificar se o total de votos bate
            var totalCalculado = VotosValidos + VotosBrancos + VotosNulos;
            if (totalCalculado != TotalVotantes)
                return false;
                
            // Verificar se o hash bate
            var hashAnterior = HashApuracao;
            GerarHashApuracao();
            
            return hashAnterior == HashApuracao;
        }
        
        private void AdicionarLog(string descricao, TipoLogApuracao tipo)
        {
            var log = new LogApuracao(this.Id, descricao, tipo);
            LogsApuracao.Add(log);
        }
        
        private void ValidarDados()
        {
            if (EleicaoId <= 0)
                throw new ArgumentException("ID da eleição inválido.");
                
            if (TotalEleitores <= 0)
                throw new ArgumentException("Total de eleitores deve ser maior que zero.");
        }
    }
    
    // Enums
    public enum StatusApuracao
    {
        NaoIniciada = 0,
        EmAndamento = 1,
        Pausada = 2,
        Finalizada = 3,
        Reaberta = 4,
        Cancelada = 5
    }
    
    public enum TipoLogApuracao
    {
        Inicio = 1,
        ContagemVotos = 2,
        ProcessamentoBoletim = 3,
        Correcao = 4,
        Fim = 5,
        Auditoria = 6,
        Reabertura = 7,
        Erro = 8
    }
}