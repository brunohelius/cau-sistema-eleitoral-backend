using System;
using System.Collections.Generic;
using System.Linq;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Domain.Exceptions;

namespace SistemaEleitoral.Domain.Entities
{
    public class Impugnacao : AuditableEntity
    {
        public int Id { get; set; }
        public string Protocolo { get; set; }
        public int ChapaEleicaoId { get; set; }
        public int? MembroChapaId { get; set; } // Specific member impugnation
        public int ImpugnadorId { get; set; }
        public string Motivo { get; set; }
        public string FundamentosLegais { get; set; }
        public string FundamentosLegaisJson { get; set; } // JSON array of legal grounds
        public StatusImpugnacao Status { get; set; } = StatusImpugnacao.Protocolada;
        public DateTime DataProtocolo { get; set; } = DateTime.UtcNow;
        public DateTime? DataAnalise { get; set; }
        public DateTime? DataNotificacaoDefesa { get; set; }
        public DateTime? PrazoDefesa { get; set; }
        public DateTime? DataRecebimentoDefesa { get; set; }
        public string DefesaTexto { get; set; }
        public DateTime? DataJulgamento { get; set; }
        public DecisaoJulgamento? Decisao { get; set; }
        public string FundamentacaoDecisao { get; set; }
        public bool CabeRecurso { get; set; }
        public DateTime? PrazoRecurso { get; set; }
        
        // Navigation Properties
        public virtual ChapaEleicao ChapaEleicao { get; set; }
        public virtual MembroChapa MembroChapa { get; set; }
        public virtual Profissional Impugnador { get; set; }
        public virtual ICollection<Julgamento> Julgamentos { get; set; } = new List<Julgamento>();
        public virtual ICollection<DocumentoEleitoral> Documentos { get; set; } = new List<DocumentoEleitoral>();
        public virtual ICollection<Recurso> Recursos { get; set; } = new List<Recurso>();

        // Business Methods
        public void AnalisarImpugnacao()
        {
            if (Status != StatusImpugnacao.Protocolada)
                throw new BusinessException("Impugnação não está protocolada");

            DataAnalise = DateTime.UtcNow;
            Status = StatusImpugnacao.EmAnalise;
        }

        public void NotificarParaDefesa(int prazoEmDias = 10)
        {
            if (Status != StatusImpugnacao.EmAnalise)
                throw new BusinessException("Impugnação deve estar em análise para notificar defesa");

            DataNotificacaoDefesa = DateTime.UtcNow;
            PrazoDefesa = DateTime.UtcNow.AddDays(prazoEmDias);
            Status = StatusImpugnacao.AguardandoDefesa;
        }

        public void ReceberDefesa(string textoDefesa)
        {
            if (Status != StatusImpugnacao.AguardandoDefesa)
                throw new BusinessException("Impugnação não está aguardando defesa");

            if (DateTime.UtcNow > PrazoDefesa)
                throw new BusinessException("Prazo para defesa expirado");

            DefesaTexto = textoDefesa;
            DataRecebimentoDefesa = DateTime.UtcNow;
            Status = StatusImpugnacao.DefesaRecebida;
        }

        public void PrepararParaJulgamento()
        {
            if (Status != StatusImpugnacao.DefesaRecebida && !PrazoDefesaVencido())
                throw new BusinessException("Impugnação não está pronta para julgamento");

            Status = StatusImpugnacao.AguardandoJulgamento;
        }

        public void Julgar(DecisaoJulgamento decisao, string fundamentacao, bool cabeRecurso, int prazoRecursoDias = 10)
        {
            if (Status != StatusImpugnacao.AguardandoJulgamento)
                throw new BusinessException("Impugnação não está aguardando julgamento");

            Decisao = decisao;
            FundamentacaoDecisao = fundamentacao;
            DataJulgamento = DateTime.UtcNow;
            CabeRecurso = cabeRecurso;

            if (cabeRecurso)
            {
                PrazoRecurso = DateTime.UtcNow.AddDays(prazoRecursoDias);
            }

            Status = decisao switch
            {
                DecisaoJulgamento.Procedente => StatusImpugnacao.Procedente,
                DecisaoJulgamento.Improcedente => StatusImpugnacao.Improcedente,
                DecisaoJulgamento.ParcialmenteProcedente => StatusImpugnacao.ParcialmenteProcedente,
                _ => StatusImpugnacao.Arquivada
            };

            // Update chapa status if impugnation is procedente
            if (decisao == DecisaoJulgamento.Procedente)
            {
                ChapaEleicao.ImpugnarChapa();
            }
        }

        public void Arquivar(string motivo)
        {
            if (Status == StatusImpugnacao.Arquivada)
                throw new BusinessException("Impugnação já está arquivada");

            Status = StatusImpugnacao.Arquivada;
            FundamentacaoDecisao = $"Arquivada: {motivo}";
        }

        public void IniciarRecurso()
        {
            if (!CabeRecurso)
                throw new BusinessException("Esta impugnação não permite recurso");

            if (PrazoRecursoVencido())
                throw new BusinessException("Prazo para recurso expirado");

            Status = StatusImpugnacao.EmRecurso;
        }

        public bool PrazoDefesaVencido()
        {
            return PrazoDefesa.HasValue && DateTime.UtcNow > PrazoDefesa.Value;
        }

        public bool PrazoRecursoVencido()
        {
            return PrazoRecurso.HasValue && DateTime.UtcNow > PrazoRecurso.Value;
        }

        public string GerarProtocolo()
        {
            return $"IMP/{ChapaEleicao?.EleicaoId:D4}/{DateTime.Now.Year}/{Id:D6}";
        }

        public bool AfetaElegibilidade()
        {
            // Check if impugnation affects eligibility
            return Decisao == DecisaoJulgamento.Procedente || 
                   Decisao == DecisaoJulgamento.ParcialmenteProcedente;
        }
    }
}