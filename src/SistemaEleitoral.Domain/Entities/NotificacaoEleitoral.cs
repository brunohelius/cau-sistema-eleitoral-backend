using System;
using System.Collections.Generic;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Domain.Exceptions;

namespace SistemaEleitoral.Domain.Entities
{
    public class NotificacaoEleitoral : AuditableEntity  
    {
        public int Id { get; set; }
        public TipoNotificacao TipoNotificacao { get; set; }
        public string TipoEvento { get; set; } // CHAPA_CRIADA, CONVITE_MEMBRO, DENUNCIA_RECEBIDA, etc.
        public int? DestinatarioId { get; set; }
        public string EmailDestinatario { get; set; }
        public string TelefoneDestinatario { get; set; }
        public string Titulo { get; set; }
        public string Mensagem { get; set; }
        public string DadosAdicionaisJson { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataEnvio { get; set; }
        public DateTime? DataLeitura { get; set; }
        public bool Enviada { get; set; }
        public bool Lida { get; set; }
        public bool Erro { get; set; }
        public string MensagemErro { get; set; }
        public int TentativasEnvio { get; set; }
        public DateTime? ProximaTentativa { get; set; }
        public int? EmailJobId { get; set; }
        public string Template { get; set; }
        public int? CalendarioEleitoralId { get; set; }
        public int? EleicaoId { get; set; }
        
        // Navigation Properties
        public virtual Profissional Destinatario { get; set; }
        public virtual EmailJob EmailJob { get; set; }
        public virtual CalendarioEleitoral CalendarioEleitoral { get; set; }
        public virtual Eleicao Eleicao { get; set; }

        // Business Methods
        public void MarcarComoEnviada()
        {
            Enviada = true;
            DataEnvio = DateTime.UtcNow;
            Erro = false;
            MensagemErro = null;
        }

        public void MarcarComoLida()
        {
            Lida = true;
            DataLeitura = DateTime.UtcNow;
        }

        public void RegistrarErro(string erro)
        {
            Erro = true;
            MensagemErro = erro;
            TentativasEnvio++;
            
            // Exponential backoff for retry
            var minutosEspera = Math.Pow(2, Math.Min(TentativasEnvio, 5));
            ProximaTentativa = DateTime.UtcNow.AddMinutes(minutosEspera);
        }

        public bool DeveRetentar()
        {
            return Erro && 
                   TentativasEnvio < 3 && 
                   ProximaTentativa.HasValue && 
                   DateTime.UtcNow >= ProximaTentativa.Value;
        }

        public void ResetarTentativas()
        {
            TentativasEnvio = 0;
            ProximaTentativa = null;
            Erro = false;
            MensagemErro = null;
        }
    }

    public class EmailJob : AuditableEntity
    {
        public int Id { get; set; }
        public string EmailType { get; set; } // CHAPA_CREATED, MEMBER_INVITATION, etc.
        public string EntityId { get; set; }
        public string AdditionalDataJson { get; set; }
        public EmailJobStatus Status { get; set; } = EmailJobStatus.Pending;
        public DateTime ScheduledFor { get; set; }
        public DateTime? ProcessingStartedAt { get; set; }
        public DateTime? ProcessingCompletedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public int RetryCount { get; set; }
        public string ErrorMessage { get; set; }
        public string Recipients { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsHtml { get; set; } = true;
        public string AttachmentsJson { get; set; }
        public int? TemplateId { get; set; }
        public string ProcessedBy { get; set; }
        
        // Navigation Properties
        public virtual EmailTemplate Template { get; set; }
        public virtual ICollection<NotificacaoEleitoral> Notificacoes { get; set; } = new List<NotificacaoEleitoral>();

        // Business Methods
        public void StartProcessing()
        {
            Status = EmailJobStatus.Processing;
            ProcessingStartedAt = DateTime.UtcNow;
        }

        public void CompleteProcessing()
        {
            Status = EmailJobStatus.Sent;
            ProcessingCompletedAt = DateTime.UtcNow;
            SentAt = DateTime.UtcNow;
        }

        public void FailProcessing(string error)
        {
            Status = EmailJobStatus.Failed;
            ProcessingCompletedAt = DateTime.UtcNow;
            ErrorMessage = error;
            RetryCount++;
        }

        public bool ShouldRetry()
        {
            return Status == EmailJobStatus.Failed && RetryCount < 3;
        }
    }

    public class EmailTemplate : AuditableEntity
    {
        public int Id { get; set; }
        public string Codigo { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public string EmailType { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsHtml { get; set; } = true;
        public string VariaveisDisponiveis { get; set; } // JSON with available variables
        public bool Ativo { get; set; } = true;
        public string Categoria { get; set; } // CHAPA, JUDICIAL, CALENDARIO, ADMINISTRATIVO
        public string Idioma { get; set; } = "pt-BR";
        public int? TemplateAlternativoId { get; set; }
        
        // Navigation Properties
        public virtual EmailTemplate TemplateAlternativo { get; set; }
        public virtual ICollection<EmailJob> EmailJobs { get; set; } = new List<EmailJob>();
    }

    public class ConviteMembro : AuditableEntity
    {
        public int Id { get; set; }
        public int ChapaEleicaoId { get; set; }
        public int ProfissionalConvidadoId { get; set; }
        public string EmailConvidado { get; set; }
        public string TokenConvite { get; set; }
        public DateTime DataConvite { get; set; }
        public DateTime DataExpiracao { get; set; }
        public DateTime? DataResposta { get; set; }
        public bool Aceito { get; set; }
        public string MotivoRecusa { get; set; }
        public string MensagemConvite { get; set; }
        public int ConvidadoPorId { get; set; }
        public bool EmailEnviado { get; set; }
        public DateTime? DataEnvioEmail { get; set; }
        public int TentativasEnvio { get; set; }
        
        // Navigation Properties
        public virtual ChapaEleicao ChapaEleicao { get; set; }
        public virtual Profissional ProfissionalConvidado { get; set; }
        public virtual Profissional ConvidadoPor { get; set; }

        // Business Methods
        public void AceitarConvite()
        {
            if (DateTime.UtcNow > DataExpiracao)
                throw new BusinessException("Convite expirado");

            Aceito = true;
            DataResposta = DateTime.UtcNow;
        }

        public void RecusarConvite(string motivo)
        {
            if (DateTime.UtcNow > DataExpiracao)
                throw new BusinessException("Convite expirado");

            Aceito = false;
            DataResposta = DateTime.UtcNow;
            MotivoRecusa = motivo;
        }

        public string GerarTokenConvite()
        {
            TokenConvite = Guid.NewGuid().ToString("N");
            return TokenConvite;
        }

        public bool IsValido()
        {
            return !DataResposta.HasValue && DateTime.UtcNow <= DataExpiracao;
        }
    }

    public enum EmailJobStatus
    {
        Pending,
        Processing,
        Sent,
        Failed,
        Cancelled
    }
}