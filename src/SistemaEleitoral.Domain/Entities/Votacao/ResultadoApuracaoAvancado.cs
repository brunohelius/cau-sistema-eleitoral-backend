using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SistemaEleitoral.Domain.Entities.Votacao
{
    /// <summary>
    /// Resultado da apuração com controle de integridade e auditoria
    /// </summary>
    public class ResultadoApuracaoAvancado : BaseEntity
    {
        // Propriedades Básicas
        public int Id { get; set; }
        public int EleicaoId { get; set; }
        public int? UfId { get; set; }
        public DateTime DataInicioApuracao { get; set; }
        public DateTime? DataFimApuracao { get; set; }
        public StatusApuracao Status { get; set; }
        
        // Contadores Gerais
        public int TotalEleitoresAptos { get; set; }
        public int TotalVotosApurados { get; set; }
        public int TotalVotosValidos { get; set; }
        public int TotalVotosBrancos { get; set; }
        public int TotalVotosNulos { get; set; }
        public int TotalAbstencoes { get; set; }
        
        // Percentuais
        public decimal PercentualParticipacao { get; set; }
        public decimal PercentualAbstencao { get; set; }
        public decimal PercentualVotosValidos { get; set; }
        public decimal PercentualVotosBrancos { get; set; }
        public decimal PercentualVotosNulos { get; set; }
        public decimal PercentualApurado { get; set; }
        
        // Controle de Integridade
        public string HashIntegridade { get; private set; }
        public string HashAnterior { get; set; }
        public int VersaoApuracao { get; set; }
        public DateTime UltimaAtualizacao { get; set; }
        
        // Recontagem
        public bool TeveRecontagem { get; set; }
        public int QuantidadeRecontagens { get; set; }
        public DateTime? DataUltimaRecontagem { get; set; }
        public string MotivoRecontagem { get; set; }
        public int? SolicitadoPorId { get; set; }
        
        // Auditoria
        public bool FoiAuditado { get; set; }
        public DateTime? DataAuditoria { get; set; }
        public string AuditorResponsavel { get; set; }
        public string ParecerAuditoria { get; set; }
        public bool AprovadoAuditoria { get; set; }
        
        // Responsabilidades
        public int? FinalizadoPorId { get; set; }
        public int? SuspensoPorId { get; set; }
        public DateTime? DataSuspensao { get; set; }
        public string MotivoSuspensao { get; set; }
        
        // Navegação
        public virtual Eleicao Eleicao { get; set; }
        public virtual Uf Uf { get; set; }
        public virtual Usuario FinalizadoPor { get; set; }
        public virtual Usuario SuspensoPor { get; set; }
        public virtual Usuario SolicitadoPor { get; set; }
        public virtual ICollection<ResultadoChapa> ResultadosChapas { get; set; }
        public virtual ICollection<HistoricoApuracao> Historicos { get; set; }
        public virtual ICollection<EstatisticasParticipacao> Estatisticas { get; set; }
        
        // Construtor
        public ResultadoApuracaoAvancado()
        {
            Status = StatusApuracao.EmAndamento;
            DataInicioApuracao = DateTime.UtcNow;
            VersaoApuracao = 1;
            UltimaAtualizacao = DateTime.UtcNow;
            ResultadosChapas = new List<ResultadoChapa>();
            Historicos = new List<HistoricoApuracao>();
            Estatisticas = new List<EstatisticasParticipacao>();
        }
        
        // Métodos de Negócio
        
        /// <summary>
        /// Atualiza contagem de votos
        /// </summary>
        public void AtualizarContagem(int votosValidos, int votosBrancos, int votosNulos)
        {
            if (Status != StatusApuracao.EmAndamento)
                throw new InvalidOperationException($"Não é possível atualizar contagem no status {Status}");
            
            // Atualizar contadores
            TotalVotosValidos = votosValidos;
            TotalVotosBrancos = votosBrancos;
            TotalVotosNulos = votosNulos;
            TotalVotosApurados = votosValidos + votosBrancos + votosNulos;
            TotalAbstencoes = TotalEleitoresAptos - TotalVotosApurados;
            
            // Recalcular percentuais
            RecalcularPercentuais();
            
            // Atualizar integridade
            HashAnterior = HashIntegridade;
            HashIntegridade = GerarHashIntegridade();
            UltimaAtualizacao = DateTime.UtcNow;
            
            // Registrar histórico
            RegistrarHistorico($"Contagem atualizada: {TotalVotosApurados} votos apurados");
        }
        
        /// <summary>
        /// Recalcula todos os percentuais
        /// </summary>
        private void RecalcularPercentuais()
        {
            if (TotalEleitoresAptos > 0)
            {
                PercentualParticipacao = (decimal)TotalVotosApurados / TotalEleitoresAptos * 100;
                PercentualAbstencao = (decimal)TotalAbstencoes / TotalEleitoresAptos * 100;
                PercentualApurado = PercentualParticipacao; // Assumindo que todos os votos registrados foram apurados
            }
            
            if (TotalVotosApurados > 0)
            {
                PercentualVotosValidos = (decimal)TotalVotosValidos / TotalVotosApurados * 100;
                PercentualVotosBrancos = (decimal)TotalVotosBrancos / TotalVotosApurados * 100;
                PercentualVotosNulos = (decimal)TotalVotosNulos / TotalVotosApurados * 100;
            }
        }
        
        /// <summary>
        /// Finaliza a apuração
        /// </summary>
        public void Finalizar(int usuarioId)
        {
            if (Status != StatusApuracao.EmAndamento)
                throw new InvalidOperationException($"Não é possível finalizar apuração no status {Status}");
            
            // Validar integridade antes de finalizar
            if (!VerificarIntegridade())
                throw new InvalidOperationException("Falha na verificação de integridade");
            
            Status = StatusApuracao.Finalizada;
            DataFimApuracao = DateTime.UtcNow;
            FinalizadoPorId = usuarioId;
            
            // Calcular posições finais das chapas
            CalcularPosicoesChapas();
            
            // Atualizar hash final
            HashIntegridade = GerarHashIntegridade();
            
            RegistrarHistorico($"Apuração finalizada com {TotalVotosApurados} votos apurados");
        }
        
        /// <summary>
        /// Suspende a apuração
        /// </summary>
        public void Suspender(int usuarioId, string motivo)
        {
            if (Status == StatusApuracao.Finalizada)
                throw new InvalidOperationException("Não é possível suspender apuração finalizada");
            
            Status = StatusApuracao.Suspensa;
            DataSuspensao = DateTime.UtcNow;
            SuspensoPorId = usuarioId;
            MotivoSuspensao = motivo;
            
            RegistrarHistorico($"Apuração suspensa: {motivo}");
        }
        
        /// <summary>
        /// Retoma apuração suspensa
        /// </summary>
        public void Retomar(int usuarioId)
        {
            if (Status != StatusApuracao.Suspensa)
                throw new InvalidOperationException($"Não é possível retomar apuração no status {Status}");
            
            Status = StatusApuracao.EmAndamento;
            DataSuspensao = null;
            SuspensoPorId = null;
            MotivoSuspensao = null;
            
            RegistrarHistorico("Apuração retomada");
        }
        
        /// <summary>
        /// Solicita recontagem
        /// </summary>
        public void SolicitarRecontagem(int usuarioId, string motivo)
        {
            if (Status != StatusApuracao.Finalizada)
                throw new InvalidOperationException("Recontagem só pode ser solicitada após finalização");
            
            TeveRecontagem = true;
            QuantidadeRecontagens++;
            DataUltimaRecontagem = DateTime.UtcNow;
            MotivoRecontagem = motivo;
            SolicitadoPorId = usuarioId;
            Status = StatusApuracao.Recontagem;
            VersaoApuracao++;
            
            // Zerar contadores para nova contagem
            TotalVotosApurados = 0;
            TotalVotosValidos = 0;
            TotalVotosBrancos = 0;
            TotalVotosNulos = 0;
            
            RegistrarHistorico($"Recontagem #{QuantidadeRecontagens} solicitada: {motivo}");
        }
        
        /// <summary>
        /// Calcula posições das chapas
        /// </summary>
        private void CalcularPosicoesChapas()
        {
            if (ResultadosChapas == null || !ResultadosChapas.Any())
                return;
            
            var chapaOrdenadas = ResultadosChapas
                .OrderByDescending(r => r.TotalVotos)
                .ThenBy(r => r.NumeroChapa)
                .ToList();
            
            for (int i = 0; i < chapaOrdenadas.Count; i++)
            {
                var resultado = chapaOrdenadas[i];
                resultado.Posicao = i + 1;
                
                // Calcular diferença para o primeiro
                if (i > 0)
                {
                    resultado.DiferencaParaPrimeiro = chapaOrdenadas[0].TotalVotos - resultado.TotalVotos;
                }
                
                // Calcular diferença para o anterior
                if (i > 0)
                {
                    resultado.DiferencaParaAnterior = chapaOrdenadas[i - 1].TotalVotos - resultado.TotalVotos;
                }
            }
        }
        
        /// <summary>
        /// Adiciona resultado de chapa
        /// </summary>
        public void AdicionarResultadoChapa(ResultadoChapa resultado)
        {
            if (Status == StatusApuracao.Finalizada)
                throw new InvalidOperationException("Não é possível adicionar resultados após finalização");
            
            ResultadosChapas?.Add(resultado);
            
            // Recalcular totais
            TotalVotosValidos = ResultadosChapas.Sum(r => r.TotalVotos);
            RecalcularPercentuais();
        }
        
        /// <summary>
        /// Audita o resultado
        /// </summary>
        public void Auditar(string auditor, string parecer, bool aprovado)
        {
            if (Status != StatusApuracao.Finalizada)
                throw new InvalidOperationException("Apenas apurações finalizadas podem ser auditadas");
            
            FoiAuditado = true;
            DataAuditoria = DateTime.UtcNow;
            AuditorResponsavel = auditor;
            ParecerAuditoria = parecer;
            AprovadoAuditoria = aprovado;
            
            if (aprovado)
            {
                Status = StatusApuracao.Homologada;
            }
            else
            {
                Status = StatusApuracao.Contestada;
            }
            
            RegistrarHistorico($"Apuração auditada por {auditor}: {(aprovado ? "Aprovada" : "Contestada")}");
        }
        
        /// <summary>
        /// Gera hash de integridade
        /// </summary>
        private string GerarHashIntegridade()
        {
            var dados = $"{Id}|{EleicaoId}|{TotalVotosApurados}|{TotalVotosValidos}|{TotalVotosBrancos}|{TotalVotosNulos}|{VersaoApuracao}|{UltimaAtualizacao:O}";
            
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Verifica integridade dos dados
        /// </summary>
        public bool VerificarIntegridade()
        {
            // Verificar se totais batem
            var totalCalculado = TotalVotosValidos + TotalVotosBrancos + TotalVotosNulos;
            if (totalCalculado != TotalVotosApurados)
                return false;
            
            // Verificar se abstencões batem
            var abstencaoCalculada = TotalEleitoresAptos - TotalVotosApurados;
            if (abstencaoCalculada != TotalAbstencoes)
                return false;
            
            // Verificar percentuais
            var somaPercentuais = PercentualParticipacao + PercentualAbstencao;
            if (Math.Abs(somaPercentuais - 100) > 0.01m)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Obtém estatísticas da apuração
        /// </summary>
        public EstatisticasApuracao ObterEstatisticas()
        {
            return new EstatisticasApuracao
            {
                TotalEleitoresAptos = TotalEleitoresAptos,
                TotalVotosApurados = TotalVotosApurados,
                TotalVotosValidos = TotalVotosValidos,
                TotalVotosBrancos = TotalVotosBrancos,
                TotalVotosNulos = TotalVotosNulos,
                TotalAbstencoes = TotalAbstencoes,
                PercentualParticipacao = PercentualParticipacao,
                PercentualAbstencao = PercentualAbstencao,
                PercentualVotosValidos = PercentualVotosValidos,
                PercentualVotosBrancos = PercentualVotosBrancos,
                PercentualVotosNulos = PercentualVotosNulos,
                PercentualApurado = PercentualApurado,
                QuantidadeChapas = ResultadosChapas?.Count ?? 0,
                Status = Status.ToString(),
                DuracaoApuracao = DataFimApuracao.HasValue 
                    ? DataFimApuracao.Value - DataInicioApuracao 
                    : DateTime.UtcNow - DataInicioApuracao
            };
        }
        
        /// <summary>
        /// Registra histórico da apuração
        /// </summary>
        private void RegistrarHistorico(string acao)
        {
            var historico = new HistoricoApuracao
            {
                ResultadoApuracaoId = Id,
                DataHora = DateTime.UtcNow,
                Acao = acao,
                VersaoApuracao = VersaoApuracao,
                TotalVotosNoMomento = TotalVotosApurados,
                HashIntegridade = HashIntegridade
            };
            
            Historicos?.Add(historico);
        }
    }
    
    /// <summary>
    /// Status da apuração
    /// </summary>
    public enum StatusApuracao
    {
        EmAndamento,
        Suspensa,
        Finalizada,
        Recontagem,
        Homologada,
        Contestada,
        Cancelada
    }
    
    /// <summary>
    /// Histórico de ações da apuração
    /// </summary>
    public class HistoricoApuracao
    {
        public int Id { get; set; }
        public int ResultadoApuracaoId { get; set; }
        public DateTime DataHora { get; set; }
        public string Acao { get; set; }
        public int VersaoApuracao { get; set; }
        public int TotalVotosNoMomento { get; set; }
        public string HashIntegridade { get; set; }
        
        public virtual ResultadoApuracaoAvancado ResultadoApuracao { get; set; }
    }
    
    /// <summary>
    /// Estatísticas da apuração
    /// </summary>
    public class EstatisticasApuracao
    {
        public int TotalEleitoresAptos { get; set; }
        public int TotalVotosApurados { get; set; }
        public int TotalVotosValidos { get; set; }
        public int TotalVotosBrancos { get; set; }
        public int TotalVotosNulos { get; set; }
        public int TotalAbstencoes { get; set; }
        public decimal PercentualParticipacao { get; set; }
        public decimal PercentualAbstencao { get; set; }
        public decimal PercentualVotosValidos { get; set; }
        public decimal PercentualVotosBrancos { get; set; }
        public decimal PercentualVotosNulos { get; set; }
        public decimal PercentualApurado { get; set; }
        public int QuantidadeChapas { get; set; }
        public string Status { get; set; }
        public TimeSpan DuracaoApuracao { get; set; }
    }
}