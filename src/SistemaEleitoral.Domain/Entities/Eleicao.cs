using System;
using System.Collections.Generic;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Domain.Exceptions;

namespace SistemaEleitoral.Domain.Entities
{
    public class Eleicao : AuditableEntity
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public int Ano { get; set; }
        public StatusEleicao Status { get; set; } = StatusEleicao.Planejada;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public DateTime? DataVotacaoInicio { get; set; }
        public DateTime? DataVotacaoFim { get; set; }
        public DateTime? DataPosse { get; set; }
        public string ResolucaoNormativa { get; set; }
        public string ConfiguracaoJson { get; set; }
        public bool PermiteVotoOnline { get; set; } = true;
        public bool PermiteVotoPresencial { get; set; } = false;
        public int? TotalEleitores { get; set; }
        public int? TotalVotantes { get; set; }
        public decimal? PercentualParticipacao { get; set; }
        public DateTime? DataHomologacao { get; set; }
        public string MotivoAnulacao { get; set; }
        
        // Navigation Properties
        public virtual ICollection<CalendarioEleitoral> Calendarios { get; set; } = new List<CalendarioEleitoral>();
        public virtual ICollection<ChapaEleicao> Chapas { get; set; } = new List<ChapaEleicao>();
        public virtual ICollection<ComissaoEleitoral> Comissoes { get; set; } = new List<ComissaoEleitoral>();
        public virtual ICollection<Denuncia> Denuncias { get; set; } = new List<Denuncia>();

        // Business Methods
        public void IniciarEleicao()
        {
            if (Status != StatusEleicao.Planejada)
                throw new BusinessException("Eleição não está em status planejado");

            Status = StatusEleicao.Ativa;
        }

        public void IniciarVotacao()
        {
            if (Status != StatusEleicao.Ativa)
                throw new BusinessException("Eleição deve estar ativa para iniciar votação");

            if (DateTime.Now < DataVotacaoInicio)
                throw new BusinessException("Ainda não chegou o período de votação");

            Status = StatusEleicao.EmAndamento;
        }

        public void EncerrarVotacao()
        {
            if (Status != StatusEleicao.EmAndamento)
                throw new BusinessException("Votação não está em andamento");

            Status = StatusEleicao.Encerrada;
            CalcularPercentualParticipacao();
        }

        public void HomologarEleicao()
        {
            if (Status != StatusEleicao.Encerrada)
                throw new BusinessException("Eleição deve estar encerrada para ser homologada");

            Status = StatusEleicao.Homologada;
            DataHomologacao = DateTime.UtcNow;
        }

        public void AnularEleicao(string motivo)
        {
            if (Status == StatusEleicao.Anulada)
                throw new BusinessException("Eleição já está anulada");

            Status = StatusEleicao.Anulada;
            MotivoAnulacao = motivo;
        }

        public void CancelarEleicao(string motivo)
        {
            if (Status == StatusEleicao.Homologada)
                throw new BusinessException("Eleição homologada não pode ser cancelada");

            Status = StatusEleicao.Cancelada;
            MotivoAnulacao = motivo;
        }

        private void CalcularPercentualParticipacao()
        {
            if (TotalEleitores.HasValue && TotalVotantes.HasValue && TotalEleitores.Value > 0)
            {
                PercentualParticipacao = (decimal)TotalVotantes.Value / TotalEleitores.Value * 100;
            }
        }

        public bool EstaEmPeriodoEleitoral()
        {
            var agora = DateTime.Now;
            return agora >= DataInicio && agora <= DataFim;
        }

        public bool EstaEmPeriodoVotacao()
        {
            var agora = DateTime.Now;
            return DataVotacaoInicio.HasValue && DataVotacaoFim.HasValue &&
                   agora >= DataVotacaoInicio.Value && agora <= DataVotacaoFim.Value;
        }

        public string GerarCodigo()
        {
            return $"ELE{Ano}{Id:D4}";
        }
    }
}