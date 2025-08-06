using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa uma chapa candidata em uma eleição
    /// </summary>
    public class ChapaEleicao : BaseEntity
    {
        public int EleicaoId { get; set; }
        public virtual Eleicao Eleicao { get; set; }
        
        public int Numero { get; set; }
        public string Nome { get; set; }
        public string Slogan { get; set; }
        public string PropostaResumo { get; set; }
        public string PropostaCompleta { get; set; }
        public string FotoUrl { get; set; }
        
        public StatusChapa Status { get; set; }
        public DateTime DataInscricao { get; set; }
        public DateTime? DataHomologacao { get; set; }
        public string MotivoIndeferimento { get; set; }
        
        // Votos
        public int TotalVotos { get; set; }
        public decimal PercentualVotos { get; set; }
        public int Classificacao { get; set; }
        public bool Eleita { get; set; }
        
        // Relacionamentos
        public virtual ICollection<MembroChapa> Membros { get; set; }
        public virtual ICollection<DocumentoChapa> Documentos { get; set; }
        public virtual ICollection<ImpugnacaoChapa> Impugnacoes { get; set; }
        public virtual ICollection<VotoEleicao> Votos { get; set; }
        
        public ChapaEleicao()
        {
            Status = StatusChapa.PendenteAnalise;
            DataInscricao = DateTime.Now;
            TotalVotos = 0;
            PercentualVotos = 0;
            Membros = new HashSet<MembroChapa>();
            Documentos = new HashSet<DocumentoChapa>();
            Impugnacoes = new HashSet<ImpugnacaoChapa>();
            Votos = new HashSet<VotoEleicao>();
        }
        
        public void AdicionarMembro(MembroChapa membro)
        {
            if (Status != StatusChapa.PendenteAnalise && Status != StatusChapa.EmCorrecao)
                throw new InvalidOperationException("Não é possível adicionar membros após homologação");
                
            Membros.Add(membro);
        }
        
        public void RemoverMembro(int membroId)
        {
            if (Status != StatusChapa.PendenteAnalise && Status != StatusChapa.EmCorrecao)
                throw new InvalidOperationException("Não é possível remover membros após homologação");
                
            var membro = Membros.FirstOrDefault(m => m.Id == membroId);
            if (membro != null)
                Membros.Remove(membro);
        }
        
        public void Homologar()
        {
            if (Status != StatusChapa.PendenteAnalise)
                throw new InvalidOperationException("Chapa deve estar pendente de análise para ser homologada");
                
            Status = StatusChapa.Homologada;
            DataHomologacao = DateTime.Now;
        }
        
        public void Indeferir(string motivo)
        {
            if (Status != StatusChapa.PendenteAnalise)
                throw new InvalidOperationException("Chapa deve estar pendente de análise para ser indeferida");
                
            Status = StatusChapa.Indeferida;
            MotivoIndeferimento = motivo;
        }
        
        public void SolicitarCorrecao(string motivo)
        {
            if (Status != StatusChapa.PendenteAnalise)
                throw new InvalidOperationException("Chapa deve estar pendente de análise");
                
            Status = StatusChapa.EmCorrecao;
            MotivoIndeferimento = motivo;
        }
        
        public void ReenviarParaAnalise()
        {
            if (Status != StatusChapa.EmCorrecao)
                throw new InvalidOperationException("Chapa deve estar em correção");
                
            Status = StatusChapa.PendenteAnalise;
            MotivoIndeferimento = null;
        }
        
        public void Impugnar(string motivo)
        {
            if (Status != StatusChapa.Homologada)
                throw new InvalidOperationException("Apenas chapas homologadas podem ser impugnadas");
                
            Status = StatusChapa.Impugnada;
            MotivoIndeferimento = motivo;
        }
        
        public bool PodeRecurso()
        {
            return Status == StatusChapa.Indeferida || Status == StatusChapa.Impugnada;
        }
        
        public void AtualizarVotacao(int votos, int totalVotosEleicao)
        {
            TotalVotos = votos;
            if (totalVotosEleicao > 0)
                PercentualVotos = (decimal)votos / totalVotosEleicao * 100;
        }
    }
    
    public enum StatusChapa
    {
        PendenteAnalise = 1,
        EmCorrecao = 2,
        Homologada = 3,
        Indeferida = 4,
        Impugnada = 5,
        Desistente = 6,
        Eleita = 7,
        NaoEleita = 8
    }
}