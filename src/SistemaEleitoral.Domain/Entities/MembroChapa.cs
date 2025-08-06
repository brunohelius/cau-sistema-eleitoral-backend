using System;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Domain.Exceptions;

namespace SistemaEleitoral.Domain.Entities
{
    public class MembroChapa : BaseEntity
    {
        public new int Id { get; set; }
        public int ChapaEleicaoId { get; set; }
        public int ProfissionalId { get; set; }
        public TipoMembroChapa TipoMembro { get; set; }
        public StatusMembroChapa Status { get; set; } = StatusMembroChapa.ConvitePendente;
        public DateTime DataConvite { get; set; }
        public DateTime? DataAceite { get; set; }
        public DateTime? DataRecusa { get; set; }
        public string MotivoRecusa { get; set; }
        public int OrdemExibicao { get; set; }
        public bool Elegivel { get; set; }
        public string MotivoInelegibilidade { get; set; }
        
        // Diversity tracking
        public string Genero { get; set; }
        public string Etnia { get; set; }
        public bool LGBTQI { get; set; }
        public bool PossuiDeficiencia { get; set; }
        public string TipoDeficiencia { get; set; }
        
        // Professional information
        public string NumeroRegistro { get; set; }
        public string UfRegistro { get; set; }
        public DateTime? DataFormatura { get; set; }
        public string InstituicaoFormacao { get; set; }
        public bool AdimplenteSituacaoFinanceira { get; set; }
        public bool AdimplenteSituacaoEtica { get; set; }
        public bool RegistroAtivo { get; set; }
        
        // Navigation Properties
        public virtual ChapaEleicao ChapaEleicao { get; set; }
        public virtual Profissional Profissional { get; set; }
        public virtual ICollection<ValidacaoElegibilidade> ValidacoesElegibilidade { get; set; } = new List<ValidacaoElegibilidade>();

        // Business Methods
        public void AceitarConvite()
        {
            if (Status != StatusMembroChapa.ConvitePendente)
                throw new BusinessException("Convite não está pendente");
                
            Status = StatusMembroChapa.Ativo;
            DataAceite = DateTime.UtcNow;
        }

        public void RecusarConvite(string motivo)
        {
            if (Status != StatusMembroChapa.ConvitePendente)
                throw new BusinessException("Convite não está pendente");
                
            Status = StatusMembroChapa.Recusado;
            DataRecusa = DateTime.UtcNow;
            MotivoRecusa = motivo;
        }

        public void RemoverMembro(string motivo)
        {
            if (Status == StatusMembroChapa.Removido)
                throw new BusinessException("Membro já foi removido");
                
            Status = StatusMembroChapa.Removido;
            MotivoRecusa = motivo;
        }

        public void ValidarElegibilidade()
        {
            Elegivel = true;
            var motivos = new List<string>();

            // Rule 1: Financial compliance
            if (!AdimplenteSituacaoFinanceira)
            {
                Elegivel = false;
                motivos.Add("Inadimplente com situação financeira");
            }

            // Rule 2: Ethical compliance
            if (!AdimplenteSituacaoEtica)
            {
                Elegivel = false;
                motivos.Add("Inadimplente com situação ética");
            }

            // Rule 3: Active registration
            if (!RegistroAtivo)
            {
                Elegivel = false;
                motivos.Add("Registro profissional inativo");
            }

            // Rule 4: Minimum years since graduation (3 years)
            if (DataFormatura.HasValue)
            {
                var anosFormado = (DateTime.Now - DataFormatura.Value).TotalDays / 365;
                if (anosFormado < 3)
                {
                    Elegivel = false;
                    motivos.Add($"Menos de 3 anos de formação ({anosFormado:F1} anos)");
                }
            }
            else
            {
                Elegivel = false;
                motivos.Add("Data de formatura não informada");
            }

            // Rule 5: Cannot be in more than one chapa
            // This rule is validated at the service level

            MotivoInelegibilidade = string.Join("; ", motivos);
        }

        public bool PodeSerCoordenador()
        {
            // Additional rules for coordinator eligibility
            if (!Elegivel)
                return false;

            // Coordinator must have at least 5 years of experience
            if (DataFormatura.HasValue)
            {
                var anosFormado = (DateTime.Now - DataFormatura.Value).TotalDays / 365;
                return anosFormado >= 5;
            }

            return false;
        }

        public bool PodeSerViceCoordenador()
        {
            // Vice coordinator must have at least 4 years of experience
            if (!Elegivel)
                return false;

            if (DataFormatura.HasValue)
            {
                var anosFormado = (DateTime.Now - DataFormatura.Value).TotalDays / 365;
                return anosFormado >= 4;
            }

            return false;
        }
    }
}