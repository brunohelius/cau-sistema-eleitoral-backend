using System;
using System.Collections.Generic;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Domain.Exceptions;

namespace SistemaEleitoral.Domain.Entities
{
    public class ProcessoSubstituicao : AuditableEntity
    {
        public int Id { get; set; }
        public string Protocolo { get; set; }
        public int ChapaEleicaoId { get; set; }
        public int MembroOriginalId { get; set; }
        public int MembroSubstitutoId { get; set; }
        public string Justificativa { get; set; }
        public StatusProcessoSubstituicao Status { get; set; } = StatusProcessoSubstituicao.Solicitado;
        public DateTime DataSolicitacao { get; set; }
        public DateTime? DataAnalise { get; set; }
        public DateTime? DataDecisao { get; set; }
        public DateTime? DataEfetivacao { get; set; }
        public string MotivoDecisao { get; set; }
        public int? AnalisadoPorId { get; set; }
        public int? AprovadoPorId { get; set; }
        public bool RequerDocumentacao { get; set; }
        public bool DocumentacaoRecebida { get; set; }
        public DateTime? PrazoDocumentacao { get; set; }
        public string ObservacoesProcesso { get; set; }
        public bool ValidacaoElegibilidadeOk { get; set; }
        public string MotivoInelegibilidade { get; set; }
        
        // Navigation Properties
        public virtual ChapaEleicao ChapaEleicao { get; set; }
        public virtual MembroChapa MembroOriginal { get; set; }
        public virtual Profissional MembroSubstituto { get; set; }
        public virtual MembroComissaoEleitoral AnalisadoPor { get; set; }
        public virtual MembroComissaoEleitoral AprovadoPor { get; set; }
        public virtual ICollection<DocumentoEleitoral> Documentos { get; set; } = new List<DocumentoEleitoral>();
        public virtual ICollection<ValidacaoElegibilidade> ValidacoesElegibilidade { get; set; } = new List<ValidacaoElegibilidade>();

        // Business Methods
        public void IniciarAnalise(int analistaId)
        {
            if (Status != StatusProcessoSubstituicao.Solicitado)
                throw new BusinessException("Processo não está em status adequado para análise");

            Status = StatusProcessoSubstituicao.EmAnalise;
            DataAnalise = DateTime.UtcNow;
            AnalisadoPorId = analistaId;
        }

        public void SolicitarDocumentacao(int prazoEmDias = 10)
        {
            if (Status != StatusProcessoSubstituicao.EmAnalise)
                throw new BusinessException("Processo deve estar em análise para solicitar documentação");

            Status = StatusProcessoSubstituicao.AguardandoDocumentacao;
            RequerDocumentacao = true;
            PrazoDocumentacao = DateTime.UtcNow.AddDays(prazoEmDias);
        }

        public void ReceberDocumentacao()
        {
            if (Status != StatusProcessoSubstituicao.AguardandoDocumentacao)
                throw new BusinessException("Processo não está aguardando documentação");

            if (DateTime.UtcNow > PrazoDocumentacao)
                throw new BusinessException("Prazo para envio de documentação expirado");

            DocumentacaoRecebida = true;
            Status = StatusProcessoSubstituicao.DocumentacaoRecebida;
        }

        public void IniciarValidacaoElegibilidade()
        {
            if (Status != StatusProcessoSubstituicao.DocumentacaoRecebida && Status != StatusProcessoSubstituicao.EmAnalise)
                throw new BusinessException("Processo não está pronto para validação de elegibilidade");

            Status = StatusProcessoSubstituicao.AguardandoValidacao;
        }

        public void RegistrarValidacaoElegibilidade(bool elegivel, string motivoInelegibilidade = null)
        {
            if (Status != StatusProcessoSubstituicao.AguardandoValidacao)
                throw new BusinessException("Processo não está aguardando validação");

            ValidacaoElegibilidadeOk = elegivel;
            MotivoInelegibilidade = motivoInelegibilidade;

            if (!elegivel)
            {
                Rejeitar($"Substituto não atende aos critérios de elegibilidade: {motivoInelegibilidade}");
            }
            else
            {
                Status = StatusProcessoSubstituicao.EmAnalise;
            }
        }

        public void Aprovar(int aprovadorId, string observacoes = null)
        {
            if (Status != StatusProcessoSubstituicao.EmAnalise && Status != StatusProcessoSubstituicao.DocumentacaoRecebida)
                throw new BusinessException("Processo não está em condições de ser aprovado");

            if (!ValidacaoElegibilidadeOk)
                throw new BusinessException("Não é possível aprovar substituição com membro inelegível");

            Status = StatusProcessoSubstituicao.Aprovado;
            DataDecisao = DateTime.UtcNow;
            AprovadoPorId = aprovadorId;
            MotivoDecisao = "Aprovado";
            ObservacoesProcesso = observacoes;
        }

        public void Rejeitar(string motivo)
        {
            if (Status == StatusProcessoSubstituicao.Rejeitado || Status == StatusProcessoSubstituicao.Efetivado)
                throw new BusinessException("Processo já foi decidido");

            Status = StatusProcessoSubstituicao.Rejeitado;
            DataDecisao = DateTime.UtcNow;
            MotivoDecisao = motivo;
        }

        public void EfetivarSubstituicao()
        {
            if (Status != StatusProcessoSubstituicao.Aprovado)
                throw new BusinessException("Apenas processos aprovados podem ser efetivados");

            Status = StatusProcessoSubstituicao.Efetivado;
            DataEfetivacao = DateTime.UtcNow;

            // Update original member status
            MembroOriginal.Status = StatusMembroChapa.Substituido;
            MembroOriginal.UpdatedAt = DateTime.UtcNow;
        }

        public bool PrazoDocumentacaoVencido()
        {
            return PrazoDocumentacao.HasValue && DateTime.UtcNow > PrazoDocumentacao.Value;
        }

        public string GerarProtocolo()
        {
            return $"SUB/{ChapaEleicao?.EleicaoId:D4}/{DateTime.Now.Year}/{Id:D6}";
        }

        public bool PodeSerAprovado()
        {
            return (Status == StatusProcessoSubstituicao.EmAnalise || 
                    Status == StatusProcessoSubstituicao.DocumentacaoRecebida) &&
                   ValidacaoElegibilidadeOk &&
                   (!RequerDocumentacao || DocumentacaoRecebida);
        }
    }
}