using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaEleitoral.Domain.Entities.Votacao
{
    /// <summary>
    /// Sessão de votação com controles avançados de segurança e auditoria
    /// </summary>
    public class SessaoVotacaoAvancada : BaseEntity
    {
        // Propriedades Básicas
        public int Id { get; set; }
        public int EleicaoId { get; set; }
        public int CalendarioId { get; set; }
        public int? UfId { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        
        // Período e Status
        public DateTime DataHoraAberturaPrevista { get; set; }
        public DateTime DataHoraEncerramentoPrevisto { get; set; }
        public DateTime? DataHoraAberturaReal { get; set; }
        public DateTime? DataHoraEncerramentoReal { get; set; }
        public StatusSessaoVotacao Status { get; set; }
        
        // Controle de Responsabilidade
        public int? AbertaPorId { get; set; }
        public int? EncerradaPorId { get; set; }
        public int? SuspensaPorId { get; set; }
        public DateTime? DataHoraSuspensao { get; set; }
        public string MotivoSuspensao { get; set; }
        
        // Contadores
        public int TotalVotosRegistrados { get; set; }
        public int TotalVotosValidos { get; set; }
        public int TotalVotosBrancos { get; set; }
        public int TotalVotosNulos { get; set; }
        public int TotalEleitoresAptos { get; set; }
        public int TotalAbstencoes { get; set; }
        
        // Configurações
        public bool PermitirVotoRemoto { get; set; }
        public bool ExigirBiometria { get; set; }
        public bool ExigirDocumentoFoto { get; set; }
        public int DuracaoMaximaHoras { get; set; }
        public bool PodeSerProrrogada { get; set; }
        public int? ProrrogacaoMaximaHoras { get; set; }
        
        // Auditoria
        public string HashInicial { get; set; }
        public string HashFinal { get; set; }
        public bool FoiAuditada { get; set; }
        public DateTime? DataAuditoria { get; set; }
        public string AuditorResponsavel { get; set; }
        public string ObservacoesAuditoria { get; set; }
        
        // Navegação
        public virtual Eleicao Eleicao { get; set; }
        public virtual Calendario Calendario { get; set; }
        public virtual Uf Uf { get; set; }
        public virtual Usuario AbertaPor { get; set; }
        public virtual Usuario EncerradaPor { get; set; }
        public virtual Usuario SuspensaPor { get; set; }
        public virtual ICollection<VotoEleitoral> Votos { get; set; }
        public virtual ICollection<HistoricoSessaoVotacao> Historicos { get; set; }
        
        // Construtor
        public SessaoVotacaoAvancada()
        {
            Status = StatusSessaoVotacao.Agendada;
            TotalVotosRegistrados = 0;
            TotalVotosValidos = 0;
            TotalVotosBrancos = 0;
            TotalVotosNulos = 0;
            TotalAbstencoes = 0;
            DuracaoMaximaHoras = 8;
            PermitirVotoRemoto = true;
            Votos = new List<VotoEleitoral>();
            Historicos = new List<HistoricoSessaoVotacao>();
        }
        
        // Métodos de Negócio
        
        /// <summary>
        /// Abre a sessão de votação
        /// </summary>
        public void AbrirSessao(int usuarioId)
        {
            if (Status != StatusSessaoVotacao.Agendada)
                throw new InvalidOperationException($"Sessão não pode ser aberta no status {Status}");
                
            if (DateTime.UtcNow < DataHoraAberturaPrevista.AddMinutes(-30))
                throw new InvalidOperationException("Sessão só pode ser aberta 30 minutos antes do horário previsto");
                
            Status = StatusSessaoVotacao.Aberta;
            DataHoraAberturaReal = DateTime.UtcNow;
            AbertaPorId = usuarioId;
            
            // Gerar hash inicial
            HashInicial = GerarHashSessao();
            
            RegistrarHistorico("Sessão aberta", usuarioId);
        }
        
        /// <summary>
        /// Encerra a sessão de votação
        /// </summary>
        public void EncerrarSessao(int usuarioId, bool forcado = false)
        {
            if (Status != StatusSessaoVotacao.Aberta && Status != StatusSessaoVotacao.Suspensa)
                throw new InvalidOperationException($"Sessão não pode ser encerrada no status {Status}");
                
            if (!forcado && DateTime.UtcNow < DataHoraEncerramentoPrevisto)
                throw new InvalidOperationException("Sessão ainda não atingiu o horário de encerramento previsto");
                
            Status = StatusSessaoVotacao.Encerrada;
            DataHoraEncerramentoReal = DateTime.UtcNow;
            EncerradaPorId = usuarioId;
            
            // Calcular abstencões
            TotalAbstencoes = TotalEleitoresAptos - TotalVotosRegistrados;
            
            // Gerar hash final
            HashFinal = GerarHashSessao();
            
            RegistrarHistorico(forcado ? "Sessão encerrada forçadamente" : "Sessão encerrada", usuarioId);
        }
        
        /// <summary>
        /// Suspende temporariamente a sessão
        /// </summary>
        public void SuspenderSessao(int usuarioId, string motivo)
        {
            if (Status != StatusSessaoVotacao.Aberta)
                throw new InvalidOperationException($"Sessão não pode ser suspensa no status {Status}");
                
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("Motivo da suspensão é obrigatório");
                
            Status = StatusSessaoVotacao.Suspensa;
            DataHoraSuspensao = DateTime.UtcNow;
            SuspensaPorId = usuarioId;
            MotivoSuspensao = motivo;
            
            RegistrarHistorico($"Sessão suspensa: {motivo}", usuarioId);
        }
        
        /// <summary>
        /// Reabre sessão suspensa
        /// </summary>
        public void ReabrirSessao(int usuarioId)
        {
            if (Status != StatusSessaoVotacao.Suspensa)
                throw new InvalidOperationException($"Sessão não pode ser reaberta no status {Status}");
                
            Status = StatusSessaoVotacao.Aberta;
            DataHoraSuspensao = null;
            SuspensaPorId = null;
            MotivoSuspensao = null;
            
            RegistrarHistorico("Sessão reaberta após suspensão", usuarioId);
        }
        
        /// <summary>
        /// Verifica se pode aceitar votos
        /// </summary>
        public bool PodeAceitarVotos()
        {
            if (Status != StatusSessaoVotacao.Aberta)
                return false;
                
            if (!DataHoraAberturaReal.HasValue)
                return false;
                
            // Verificar se não excedeu duração máxima
            var duracaoAtual = (DateTime.UtcNow - DataHoraAberturaReal.Value).TotalHours;
            var duracaoMaxima = DuracaoMaximaHoras + (ProrrogacaoMaximaHoras ?? 0);
            
            if (duracaoAtual > duracaoMaxima)
            {
                // Auto-encerrar por tempo excedido
                Status = StatusSessaoVotacao.Encerrada;
                DataHoraEncerramentoReal = DateTime.UtcNow;
                RegistrarHistorico("Sessão encerrada automaticamente por tempo excedido", null);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Registra um voto na sessão
        /// </summary>
        public void RegistrarVoto(VotoEleitoral voto)
        {
            if (!PodeAceitarVotos())
                throw new InvalidOperationException("Sessão não está aceitando votos");
                
            TotalVotosRegistrados++;
            
            switch (voto.TipoVoto)
            {
                case TipoVoto.Valido:
                    TotalVotosValidos++;
                    break;
                case TipoVoto.Branco:
                    TotalVotosBrancos++;
                    break;
                case TipoVoto.Nulo:
                    TotalVotosNulos++;
                    break;
            }
            
            Votos?.Add(voto);
        }
        
        /// <summary>
        /// Prorroga a sessão
        /// </summary>
        public void ProrrogarSessao(int horas, int usuarioId, string justificativa)
        {
            if (!PodeSerProrrogada)
                throw new InvalidOperationException("Sessão não pode ser prorrogada");
                
            if (Status != StatusSessaoVotacao.Aberta)
                throw new InvalidOperationException($"Sessão não pode ser prorrogada no status {Status}");
                
            if (ProrrogacaoMaximaHoras.HasValue && horas > ProrrogacaoMaximaHoras.Value)
                throw new InvalidOperationException($"Prorrogação máxima permitida é de {ProrrogacaoMaximaHoras} horas");
                
            DataHoraEncerramentoPrevisto = DataHoraEncerramentoPrevisto.AddHours(horas);
            ProrrogacaoMaximaHoras = (ProrrogacaoMaximaHoras ?? 0) + horas;
            
            RegistrarHistorico($"Sessão prorrogada por {horas} horas. Justificativa: {justificativa}", usuarioId);
        }
        
        /// <summary>
        /// Calcula a participação percentual
        /// </summary>
        public decimal CalcularParticipacao()
        {
            if (TotalEleitoresAptos == 0)
                return 0;
                
            return (decimal)TotalVotosRegistrados / TotalEleitoresAptos * 100;
        }
        
        /// <summary>
        /// Obtém estatísticas da sessão
        /// </summary>
        public EstatisticasSessao ObterEstatisticas()
        {
            return new EstatisticasSessao
            {
                TotalVotosRegistrados = TotalVotosRegistrados,
                TotalVotosValidos = TotalVotosValidos,
                TotalVotosBrancos = TotalVotosBrancos,
                TotalVotosNulos = TotalVotosNulos,
                TotalEleitoresAptos = TotalEleitoresAptos,
                TotalAbstencoes = TotalAbstencoes,
                PercentualParticipacao = CalcularParticipacao(),
                PercentualAbstencao = TotalEleitoresAptos > 0 ? (decimal)TotalAbstencoes / TotalEleitoresAptos * 100 : 0,
                PercentualVotosValidos = TotalVotosRegistrados > 0 ? (decimal)TotalVotosValidos / TotalVotosRegistrados * 100 : 0,
                PercentualVotosBrancos = TotalVotosRegistrados > 0 ? (decimal)TotalVotosBrancos / TotalVotosRegistrados * 100 : 0,
                PercentualVotosNulos = TotalVotosRegistrados > 0 ? (decimal)TotalVotosNulos / TotalVotosRegistrados * 100 : 0,
                DuracaoSessao = DataHoraEncerramentoReal.HasValue && DataHoraAberturaReal.HasValue 
                    ? DataHoraEncerramentoReal.Value - DataHoraAberturaReal.Value 
                    : TimeSpan.Zero
            };
        }
        
        /// <summary>
        /// Verifica se a sessão expirou
        /// </summary>
        public bool Expirou()
        {
            if (Status == StatusSessaoVotacao.Encerrada)
                return true;
                
            if (Status == StatusSessaoVotacao.Aberta && DataHoraAberturaReal.HasValue)
            {
                var duracaoAtual = (DateTime.UtcNow - DataHoraAberturaReal.Value).TotalHours;
                var duracaoMaxima = DuracaoMaximaHoras + (ProrrogacaoMaximaHoras ?? 0);
                return duracaoAtual > duracaoMaxima;
            }
            
            return false;
        }
        
        /// <summary>
        /// Gera hash da sessão para auditoria
        /// </summary>
        private string GerarHashSessao()
        {
            var dados = $"{Id}|{EleicaoId}|{Status}|{TotalVotosRegistrados}|{TotalVotosValidos}|{TotalVotosBrancos}|{TotalVotosNulos}|{DateTime.UtcNow:O}";
            
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(dados);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Registra histórico da sessão
        /// </summary>
        private void RegistrarHistorico(string acao, int? usuarioId)
        {
            var historico = new HistoricoSessaoVotacao
            {
                SessaoVotacaoId = Id,
                DataHora = DateTime.UtcNow,
                Acao = acao,
                UsuarioId = usuarioId,
                StatusAnterior = Status.ToString(),
                TotalVotosNoMomento = TotalVotosRegistrados
            };
            
            Historicos?.Add(historico);
        }
        
        /// <summary>
        /// Audita a sessão
        /// </summary>
        public void Auditar(string auditor, string observacoes)
        {
            if (Status != StatusSessaoVotacao.Encerrada)
                throw new InvalidOperationException("Apenas sessões encerradas podem ser auditadas");
                
            FoiAuditada = true;
            DataAuditoria = DateTime.UtcNow;
            AuditorResponsavel = auditor;
            ObservacoesAuditoria = observacoes;
            
            RegistrarHistorico($"Sessão auditada por {auditor}", null);
        }
    }
    
    /// <summary>
    /// Status da sessão de votação
    /// </summary>
    public enum StatusSessaoVotacao
    {
        Agendada,
        Aberta,
        Suspensa,
        Encerrada,
        Cancelada,
        Auditada
    }
    
    /// <summary>
    /// Histórico de ações da sessão
    /// </summary>
    public class HistoricoSessaoVotacao
    {
        public int Id { get; set; }
        public int SessaoVotacaoId { get; set; }
        public DateTime DataHora { get; set; }
        public string Acao { get; set; }
        public int? UsuarioId { get; set; }
        public string StatusAnterior { get; set; }
        public int TotalVotosNoMomento { get; set; }
        
        public virtual SessaoVotacaoAvancada SessaoVotacao { get; set; }
        public virtual Usuario Usuario { get; set; }
    }
    
    /// <summary>
    /// Estatísticas da sessão
    /// </summary>
    public class EstatisticasSessao
    {
        public int TotalVotosRegistrados { get; set; }
        public int TotalVotosValidos { get; set; }
        public int TotalVotosBrancos { get; set; }
        public int TotalVotosNulos { get; set; }
        public int TotalEleitoresAptos { get; set; }
        public int TotalAbstencoes { get; set; }
        public decimal PercentualParticipacao { get; set; }
        public decimal PercentualAbstencao { get; set; }
        public decimal PercentualVotosValidos { get; set; }
        public decimal PercentualVotosBrancos { get; set; }
        public decimal PercentualVotosNulos { get; set; }
        public TimeSpan DuracaoSessao { get; set; }
    }
}