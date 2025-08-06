using System;
using System.Collections.Generic;
using System.Linq;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Domain.Exceptions;

namespace SistemaEleitoral.Domain.Entities
{
    public class Julgamento : AuditableEntity
    {
        public int Id { get; set; }
        public string Protocolo { get; set; }
        public int ProcessoId { get; set; }
        public TipoProcessoJudicial TipoProcesso { get; set; }
        public InstanciaJulgamento Instancia { get; set; } = InstanciaJulgamento.Primeira;
        public int? JulgamentoAnteriorId { get; set; } // For second instance
        public int? RecursoId { get; set; } // If it's a recurso judgment
        public StatusJulgamento Status { get; set; } = StatusJulgamento.Agendado;
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public DateTime DataAgendamento { get; set; }
        public DecisaoJulgamento? Decisao { get; set; }
        public string Fundamentacao { get; set; }
        public int? RelatorId { get; set; }
        public string JulgadoresIds { get; set; } // JSON array of judge IDs
        public DateTime? DataDecisao { get; set; }
        public string VotosJson { get; set; } // JSON with voting details
        public int VotosFavor { get; set; }
        public int VotosContra { get; set; }
        public int Abstencoes { get; set; }
        public bool Unanime { get; set; }
        public string ObservacoesProcessuais { get; set; }
        public bool CabeRecurso { get; set; }
        public DateTime? PrazoRecurso { get; set; }
        
        // Navigation Properties
        public virtual Julgamento JulgamentoAnterior { get; set; }
        public virtual Recurso Recurso { get; set; }
        public virtual MembroComissaoEleitoral Relator { get; set; }
        public virtual ICollection<VotoJulgamento> Votos { get; set; } = new List<VotoJulgamento>();
        public virtual ICollection<DocumentoEleitoral> Documentos { get; set; } = new List<DocumentoEleitoral>();
        public virtual ICollection<Recurso> RecursosGerados { get; set; } = new List<Recurso>();

        // Business Methods
        public void IniciarJulgamento()
        {
            if (Status != StatusJulgamento.Agendado)
                throw new BusinessException("Julgamento não está agendado");

            DataInicio = DateTime.UtcNow;
            Status = StatusJulgamento.EmAndamento;
        }

        public void SuspenderJulgamento(string motivo)
        {
            if (Status != StatusJulgamento.EmAndamento)
                throw new BusinessException("Apenas julgamentos em andamento podem ser suspensos");

            Status = StatusJulgamento.Suspenso;
            ObservacoesProcessuais = $"Suspenso: {motivo}";
        }

        public void RetomarJulgamento()
        {
            if (Status != StatusJulgamento.Suspenso)
                throw new BusinessException("Julgamento não está suspenso");

            Status = StatusJulgamento.EmAndamento;
            ObservacoesProcessuais += $"\nRetomado em: {DateTime.UtcNow}";
        }

        public void AdiarJulgamento(DateTime novaData, string motivo)
        {
            if (Status == StatusJulgamento.Julgado)
                throw new BusinessException("Julgamento já foi concluído");

            Status = StatusJulgamento.Adiado;
            DataAgendamento = novaData;
            ObservacoesProcessuais = $"Adiado: {motivo}";
        }

        public void RegistrarVoto(VotoJulgamento voto)
        {
            if (Status != StatusJulgamento.EmAndamento)
                throw new BusinessException("Julgamento não está em andamento");

            Votos.Add(voto);
            
            // Update vote counts
            switch (voto.TipoVoto)
            {
                case TipoVoto.Favor:
                    VotosFavor++;
                    break;
                case TipoVoto.Contra:
                    VotosContra++;
                    break;
                case TipoVoto.Abstencao:
                    Abstencoes++;
                    break;
            }
        }

        public void ProferirDecisao(DecisaoJulgamento decisao, string fundamentacao, bool cabeRecurso, int prazoRecursoDias = 10)
        {
            if (Status != StatusJulgamento.EmAndamento)
                throw new BusinessException("Julgamento não está em andamento");

            if (Votos.Count == 0)
                throw new BusinessException("Não há votos registrados");

            Decisao = decisao;
            Fundamentacao = fundamentacao;
            DataDecisao = DateTime.UtcNow;
            DataFim = DateTime.UtcNow;
            Status = StatusJulgamento.Julgado;
            CabeRecurso = cabeRecurso;

            if (cabeRecurso)
            {
                PrazoRecurso = DateTime.UtcNow.AddDays(prazoRecursoDias);
            }

            // Check if unanimous
            Unanime = (VotosFavor == 0 || VotosContra == 0) && Abstencoes == 0;
        }

        public void AnularJulgamento(string motivo)
        {
            if (Status != StatusJulgamento.Julgado)
                throw new BusinessException("Apenas julgamentos concluídos podem ser anulados");

            Status = StatusJulgamento.Anulado;
            ObservacoesProcessuais = $"Anulado: {motivo}";
            Decisao = DecisaoJulgamento.Anulado;
        }

        public bool TemQuorum()
        {
            var totalJulgadores = GetTotalJulgadores();
            var votosRegistrados = Votos.Count;
            var quorumMinimo = Math.Ceiling(totalJulgadores / 2.0);
            
            return votosRegistrados >= quorumMinimo;
        }

        public bool PodeRecorrer()
        {
            if (!CabeRecurso)
                return false;

            if (Status != StatusJulgamento.Julgado)
                return false;

            if (PrazoRecurso.HasValue && DateTime.UtcNow > PrazoRecurso.Value)
                return false;

            return true;
        }

        public bool PrazoRecursoVencido()
        {
            return PrazoRecurso.HasValue && DateTime.UtcNow > PrazoRecurso.Value;
        }

        private int GetTotalJulgadores()
        {
            if (string.IsNullOrEmpty(JulgadoresIds))
                return 0;

            try
            {
                var julgadores = System.Text.Json.JsonSerializer.Deserialize<List<int>>(JulgadoresIds);
                return julgadores?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        public string GerarProtocolo()
        {
            var tipoAbrev = TipoProcesso switch
            {
                TipoProcessoJudicial.Denuncia => "DEN",
                TipoProcessoJudicial.Impugnacao => "IMP",
                TipoProcessoJudicial.Recurso => "REC",
                TipoProcessoJudicial.Substituicao => "SUB",
                TipoProcessoJudicial.Cassacao => "CAS",
                _ => "JUL"
            };

            var instancia = Instancia == InstanciaJulgamento.Segunda ? "2I" : "1I";
            return $"JUL/{tipoAbrev}/{instancia}/{DateTime.Now.Year}/{Id:D6}";
        }
    }

    public class VotoJulgamento : AuditableEntity
    {
        public int Id { get; set; }
        public int JulgamentoId { get; set; }
        public int MembroComissaoEleitoralId { get; set; }
        public TipoVoto TipoVoto { get; set; }
        public string Fundamentacao { get; set; }
        public DateTime DataVoto { get; set; }
        public bool VotoVencedor { get; set; }
        public bool VotoVista { get; set; }
        public string ObservacoesVoto { get; set; }
        
        // Navigation Properties
        public virtual Julgamento Julgamento { get; set; }
        public virtual MembroComissaoEleitoral MembroComissaoEleitoral { get; set; }
    }

    public class Recurso : AuditableEntity
    {
        public int Id { get; set; }
        public string Protocolo { get; set; }
        public int JulgamentoId { get; set; }
        public int RecorrenteId { get; set; }
        public TipoRecurso TipoRecurso { get; set; }
        public StatusRecurso Status { get; set; } = StatusRecurso.Protocolado;
        public string Fundamentacao { get; set; }
        public string AlegacoesJson { get; set; } // JSON array of allegations
        public DateTime DataInterposicao { get; set; }
        public DateTime? PrazoContraRazoes { get; set; }
        public DateTime? DataContraRazoes { get; set; }
        public string ContraRazoesTexto { get; set; }
        public int? ContraRazoesApresentanteId { get; set; }
        public DateTime? DataAnalise { get; set; }
        public DateTime? DataJulgamento { get; set; }
        public DecisaoJulgamento? Decisao { get; set; }
        public string FundamentacaoDecisao { get; set; }
        
        // Navigation Properties
        public virtual Julgamento JulgamentoOriginal { get; set; }
        public virtual Julgamento JulgamentoRecurso { get; set; }
        public virtual Profissional Recorrente { get; set; }
        public virtual Profissional ContraRazoesApresentante { get; set; }
        public virtual ICollection<DocumentoEleitoral> Documentos { get; set; } = new List<DocumentoEleitoral>();

        // Business Methods
        public void IniciarPrazoContraRazoes(int prazoEmDias = 10)
        {
            if (Status != StatusRecurso.Protocolado)
                throw new BusinessException("Recurso não está protocolado");

            PrazoContraRazoes = DateTime.UtcNow.AddDays(prazoEmDias);
            Status = StatusRecurso.AguardandoContraRazoes;
        }

        public void ApresentarContraRazoes(string contraRazoes, int apresentanteId)
        {
            if (Status != StatusRecurso.AguardandoContraRazoes)
                throw new BusinessException("Recurso não está aguardando contra-razões");

            if (DateTime.UtcNow > PrazoContraRazoes)
                throw new BusinessException("Prazo para contra-razões expirado");

            ContraRazoesTexto = contraRazoes;
            ContraRazoesApresentanteId = apresentanteId;
            DataContraRazoes = DateTime.UtcNow;
            Status = StatusRecurso.ContraRazoesRecebidas;
        }

        public void PrepararParaJulgamento()
        {
            if (Status != StatusRecurso.ContraRazoesRecebidas && !PrazoContraRazoesVencido())
                throw new BusinessException("Recurso não está pronto para julgamento");

            Status = StatusRecurso.AguardandoJulgamento;
        }

        public bool PrazoContraRazoesVencido()
        {
            return PrazoContraRazoes.HasValue && DateTime.UtcNow > PrazoContraRazoes.Value;
        }

        public string GerarProtocolo()
        {
            return $"REC/{DateTime.Now.Year}/{Id:D6}";
        }
    }

    public enum TipoRecurso
    {
        Ordinario,
        Especial,
        Embargos,
        AgravoDeinstrumento,
        ReconsideracaoDeOficio
    }
}