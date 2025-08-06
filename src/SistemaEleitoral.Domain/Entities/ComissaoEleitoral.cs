using System;
using System.Collections.Generic;
using System.Linq;
using SistemaEleitoral.Domain.Common;
using SistemaEleitoral.Domain.Enums;
using SistemaEleitoral.Domain.Exceptions;

namespace SistemaEleitoral.Domain.Entities
{
    public class ComissaoEleitoral : AuditableEntity
    {
        public int Id { get; set; }
        public int EleicaoId { get; set; }
        public TipoComissao TipoComissao { get; set; }
        public string UfComissao { get; set; } // For state commissions
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public DateTime DataConstituicao { get; set; }
        public DateTime? DataDissolucao { get; set; }
        public bool Ativa { get; set; } = true;
        public int NumeroMembros => MembrosComissao?.Count ?? 0;
        
        // Navigation Properties
        public virtual Eleicao Eleicao { get; set; }
        public virtual ICollection<MembroComissaoEleitoral> MembrosComissao { get; set; } = new List<MembroComissaoEleitoral>();
        public virtual ICollection<DecisaoComissao> Decisoes { get; set; } = new List<DecisaoComissao>();
        public virtual ICollection<AtaReuniao> AtasReuniao { get; set; } = new List<AtaReuniao>();

        // Business Methods
        public int GetNumeroMembrosRequerido()
        {
            return TipoComissao switch
            {
                TipoComissao.Nacional => 5,
                TipoComissao.Estadual => 3,
                _ => 3
            };
        }

        public bool ComissaoCompleta()
        {
            return NumeroMembros == GetNumeroMembrosRequerido();
        }

        public bool PodeTomarDecisao()
        {
            if (!Ativa)
                return false;

            // Quorum mínimo: maioria simples
            var membrosAtivos = MembrosComissao.Count(m => m.Status == StatusMembroComissaoEleitoral.Ativo);
            var quorumMinimo = Math.Ceiling(GetNumeroMembrosRequerido() / 2.0);
            
            return membrosAtivos >= quorumMinimo;
        }

        public void AdicionarMembro(MembroComissaoEleitoral membro)
        {
            if (NumeroMembros >= GetNumeroMembrosRequerido())
                throw new BusinessException($"Comissão já possui o número máximo de membros ({GetNumeroMembrosRequerido()})");

            // Validate unique roles
            if (membro.Cargo == CargoComissao.Coordenador && 
                MembrosComissao.Any(m => m.Cargo == CargoComissao.Coordenador && m.Status == StatusMembroComissaoEleitoral.Ativo))
            {
                throw new BusinessException("Já existe um coordenador ativo nesta comissão");
            }

            if (membro.Cargo == CargoComissao.ViceCoordenador && 
                MembrosComissao.Any(m => m.Cargo == CargoComissao.ViceCoordenador && m.Status == StatusMembroComissaoEleitoral.Ativo))
            {
                throw new BusinessException("Já existe um vice-coordenador ativo nesta comissão");
            }

            MembrosComissao.Add(membro);
        }

        public void RemoverMembro(int membroId, string motivo)
        {
            var membro = MembrosComissao.FirstOrDefault(m => m.Id == membroId);
            if (membro == null)
                throw new BusinessException("Membro não encontrado na comissão");

            membro.Status = StatusMembroComissaoEleitoral.Removido;
            membro.DataSaida = DateTime.UtcNow;
            membro.MotivoSaida = motivo;
        }

        public void RegistrarDecisao(DecisaoComissao decisao)
        {
            if (!PodeTomarDecisao())
                throw new BusinessException("Comissão não possui quorum para tomar decisões");

            decisao.ComissaoEleitoralId = Id;
            decisao.DataDecisao = DateTime.UtcNow;
            Decisoes.Add(decisao);
        }

        public void Dissolver(string motivo)
        {
            if (!Ativa)
                throw new BusinessException("Comissão já está dissolvida");

            Ativa = false;
            DataDissolucao = DateTime.UtcNow;

            // Deactivate all members
            foreach (var membro in MembrosComissao.Where(m => m.Status == StatusMembroComissaoEleitoral.Ativo))
            {
                membro.Status = StatusMembroComissaoEleitoral.Inativo;
                membro.DataSaida = DateTime.UtcNow;
                membro.MotivoSaida = $"Comissão dissolvida: {motivo}";
            }
        }

        public MembroComissaoEleitoral ObterCoordenador()
        {
            return MembrosComissao.FirstOrDefault(m => 
                m.Cargo == CargoComissao.Coordenador && 
                m.Status == StatusMembroComissaoEleitoral.Ativo);
        }

        public MembroComissaoEleitoral ObterViceCoordenador()
        {
            return MembrosComissao.FirstOrDefault(m => 
                m.Cargo == CargoComissao.ViceCoordenador && 
                m.Status == StatusMembroComissaoEleitoral.Ativo);
        }

        public IEnumerable<MembroComissaoEleitoral> ObterMembrosAtivos()
        {
            return MembrosComissao.Where(m => m.Status == StatusMembroComissaoEleitoral.Ativo);
        }

        public void CalcularComposicaoProporcional(int totalProfissionaisUf)
        {
            // Calculate proportional composition based on Resolution 179/2022
            // This method would be called when constituting the commission
            // Implementation depends on specific business rules for proportional calculation
        }
    }

    public class MembroComissaoEleitoral : AuditableEntity
    {
        public int Id { get; set; }
        public int ComissaoEleitoralId { get; set; }
        public int ProfissionalId { get; set; }
        public CargoComissao Cargo { get; set; }
        public StatusMembroComissaoEleitoral Status { get; set; } = StatusMembroComissaoEleitoral.Ativo;
        public DateTime DataNomeacao { get; set; }
        public DateTime? DataSaida { get; set; }
        public string MotivoSaida { get; set; }
        public bool PodeVotar { get; set; } = true;
        public bool PodeRelatar { get; set; } = true;
        
        // Navigation Properties
        public virtual ComissaoEleitoral ComissaoEleitoral { get; set; }
        public virtual Profissional Profissional { get; set; }
        public virtual ICollection<VotoDecisao> Votos { get; set; } = new List<VotoDecisao>();
    }

    public class DecisaoComissao : AuditableEntity
    {
        public int Id { get; set; }
        public int ComissaoEleitoralId { get; set; }
        public TipoDecisao TipoDecisao { get; set; }
        public string Assunto { get; set; }
        public string Descricao { get; set; }
        public DateTime DataDecisao { get; set; }
        public ResultadoDecisao Resultado { get; set; }
        public string Fundamentacao { get; set; }
        public int? ProcessoRelacionadoId { get; set; }
        public string TipoProcesso { get; set; } // DENUNCIA, IMPUGNACAO, SUBSTITUICAO, etc.
        public int VotosFavor { get; set; }
        public int VotosContra { get; set; }
        public int Abstencoes { get; set; }
        
        // Navigation Properties
        public virtual ComissaoEleitoral ComissaoEleitoral { get; set; }
        public virtual ICollection<VotoDecisao> Votos { get; set; } = new List<VotoDecisao>();
    }

    public class VotoDecisao : AuditableEntity
    {
        public int Id { get; set; }
        public int DecisaoComissaoId { get; set; }
        public int MembroComissaoEleitoralId { get; set; }
        public TipoVoto TipoVoto { get; set; }
        public string Justificativa { get; set; }
        public DateTime DataVoto { get; set; }
        
        // Navigation Properties
        public virtual DecisaoComissao DecisaoComissao { get; set; }
        public virtual MembroComissaoEleitoral MembroComissaoEleitoral { get; set; }
    }

    public class AtaReuniao : AuditableEntity
    {
        public int Id { get; set; }
        public int ComissaoEleitoralId { get; set; }
        public int NumeroAta { get; set; }
        public DateTime DataReuniao { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan? HoraFim { get; set; }
        public string Local { get; set; }
        public string PautaReuniao { get; set; }
        public string Deliberacoes { get; set; }
        public string ParticipantesPresentes { get; set; }
        public string ParticipantesAusentes { get; set; }
        public bool Aprovada { get; set; }
        public DateTime? DataAprovacao { get; set; }
        
        // Navigation Properties
        public virtual ComissaoEleitoral ComissaoEleitoral { get; set; }
        public virtual ICollection<DocumentoEleitoral> Anexos { get; set; } = new List<DocumentoEleitoral>();
    }
}