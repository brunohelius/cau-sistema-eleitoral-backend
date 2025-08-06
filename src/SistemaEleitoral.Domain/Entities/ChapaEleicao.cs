using System;
using System.Collections.Generic;
using System.Linq;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Domain.Exceptions;

namespace SistemaEleitoral.Domain.Entities
{
    public class ChapaEleicao : BaseEntity
    {
        public new int Id { get; set; }
        public int EleicaoId { get; set; }
        public string Nome { get; set; }
        public string Numero { get; set; }
        public SistemaEleitoral.Domain.Enums.StatusChapa Status { get; set; } = SistemaEleitoral.Domain.Enums.StatusChapa.EmElaboracao;
        public TipoChapa TipoChapa { get; set; } = TipoChapa.Nacional;
        public DateTime? DataSubmissao { get; set; }
        public DateTime? DataConfirmacao { get; set; }
        public DateTime? DataHomologacao { get; set; }
        public string MotivoRejeicao { get; set; }
        public int? InstituicaoEnsinoId { get; set; }
        public string UfChapa { get; set; }
        public bool DiversidadeGenero { get; set; }
        public bool DiversidadeEtnica { get; set; }
        public bool DiversidadeLGBTQI { get; set; }
        public bool DiversidadeDeficiencia { get; set; }
        public string DiversidadeJson { get; set; }
        public int TotalMembros => MembrosChapa?.Count ?? 0;
        public bool PossuiCoordenador => MembrosChapa?.Any(m => m.TipoMembro == TipoMembroChapa.Coordenador) ?? false;
        
        // Navigation Properties
        public virtual Eleicao Eleicao { get; set; }
        public virtual ICollection<MembroChapa> MembrosChapa { get; set; } = new List<MembroChapa>();
        public virtual ICollection<Impugnacao> Impugnacoes { get; set; } = new List<Impugnacao>();
        public virtual ICollection<ProcessoSubstituicao> ProcessosSubstituicao { get; set; } = new List<ProcessoSubstituicao>();
        public virtual ICollection<DocumentoEleitoral> Documentos { get; set; } = new List<DocumentoEleitoral>();

        // Business Methods
        public bool PodeAdicionarMembro()
        {
            return Status == SistemaEleitoral.Domain.Enums.StatusChapa.EmElaboracao && TotalMembros < GetLimiteMembros();
        }

        public bool PodeSubmeterParaAprovacao()
        {
            return Status == SistemaEleitoral.Domain.Enums.StatusChapa.EmElaboracao && 
                   PossuiCoordenador && 
                   TotalMembros >= GetMinimoMembros() &&
                   ValidaDiversidade() &&
                   TodosMembrosSaoElegiveis();
        }

        public void SubmeterParaAprovacao()
        {
            if (!PodeSubmeterParaAprovacao())
                throw new BusinessException("Chapa não atende aos requisitos para submissão");
                
            Status = SistemaEleitoral.Domain.Enums.StatusChapa.AguardandoAprovacao;
            DataSubmissao = DateTime.UtcNow;
            Numero = GerarNumeroChapa();
        }

        public void AprovarChapa(string aprovadorId)
        {
            if (Status != SistemaEleitoral.Domain.Enums.StatusChapa.AguardandoAprovacao)
                throw new BusinessException("Chapa não está aguardando aprovação");
                
            Status = SistemaEleitoral.Domain.Enums.StatusChapa.Aprovada;
            DataConfirmacao = DateTime.UtcNow;
            UpdatedBy = aprovadorId;
        }

        public void RejeitarChapa(string motivo, string rejeitadorId)
        {
            if (Status != SistemaEleitoral.Domain.Enums.StatusChapa.AguardandoAprovacao)
                throw new BusinessException("Chapa não está aguardando aprovação");
                
            Status = SistemaEleitoral.Domain.Enums.StatusChapa.Rejeitada;
            MotivoRejeicao = motivo;
            UpdatedBy = rejeitadorId;
        }

        public void HomologarChapa(string homologadorId)
        {
            if (Status != SistemaEleitoral.Domain.Enums.StatusChapa.Aprovada)
                throw new BusinessException("Chapa deve estar aprovada para ser homologada");
                
            Status = SistemaEleitoral.Domain.Enums.StatusChapa.Homologada;
            DataHomologacao = DateTime.UtcNow;
            UpdatedBy = homologadorId;
        }

        public void ImpugnarChapa()
        {
            if (Status != SistemaEleitoral.Domain.Enums.StatusChapa.Aprovada && Status != SistemaEleitoral.Domain.Enums.StatusChapa.Homologada)
                throw new BusinessException("Apenas chapas aprovadas ou homologadas podem ser impugnadas");
                
            Status = SistemaEleitoral.Domain.Enums.StatusChapa.Impugnada;
        }

        private int GetLimiteMembros()
        {
            return TipoChapa switch
            {
                TipoChapa.Nacional => 5,
                TipoChapa.Estadual => 3,
                TipoChapa.IES => 10,
                _ => 5
            };
        }

        private int GetMinimoMembros()
        {
            return TipoChapa switch
            {
                TipoChapa.Nacional => 5,
                TipoChapa.Estadual => 3,
                TipoChapa.IES => 3,
                _ => 3
            };
        }

        private bool ValidaDiversidade()
        {
            var membrosAtivos = MembrosChapa.Where(m => m.Status == StatusMembroChapa.Ativo).ToList();
            
            // Gender diversity requirement (minimum 30% of any gender)
            var totalMulheres = membrosAtivos.Count(m => m.Genero == "F");
            var totalHomens = membrosAtivos.Count(m => m.Genero == "M");
            var percentualMinimo = Math.Ceiling(membrosAtivos.Count * 0.3);
            
            DiversidadeGenero = totalMulheres >= percentualMinimo || totalHomens >= percentualMinimo;
            
            // Check other diversity criteria
            DiversidadeEtnica = membrosAtivos.Any(m => m.Etnia != "Branco");
            DiversidadeLGBTQI = membrosAtivos.Any(m => m.LGBTQI);
            DiversidadeDeficiencia = membrosAtivos.Any(m => m.PossuiDeficiencia);
            
            return DiversidadeGenero;
        }

        private bool TodosMembrosSaoElegiveis()
        {
            return MembrosChapa.All(m => m.Status == StatusMembroChapa.Ativo && m.Elegivel);
        }

        private string GerarNumeroChapa()
        {
            return $"{EleicaoId:D4}{DataSubmissao:yyyyMMddHHmmss}";
        }
    }
}