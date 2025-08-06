using System;
using System.Collections.Generic;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa uma eleição no sistema
    /// </summary>
    public class Eleicao : BaseEntity
    {
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public int Ano { get; set; }
        public TipoEleicao TipoEleicao { get; set; }
        public StatusEleicao Status { get; set; }
        
        // Datas importantes
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public DateTime DataInicioInscricao { get; set; }
        public DateTime DataFimInscricao { get; set; }
        public DateTime DataInicioVotacao { get; set; }
        public DateTime DataFimVotacao { get; set; }
        public DateTime DataApuracao { get; set; }
        public DateTime DataPosse { get; set; }
        
        // Configurações
        public int NumeroVagas { get; set; }
        public int NumeroSuplentes { get; set; }
        public decimal QuorumMinimo { get; set; }
        public bool PermiteReeleicao { get; set; }
        public int MandatoAnos { get; set; }
        
        // Relacionamentos
        public int? FilialId { get; set; }
        public virtual Filial Filial { get; set; }
        
        public int CalendarioId { get; set; }
        public virtual Calendario Calendario { get; set; }
        
        public virtual ICollection<ChapaEleicao> Chapas { get; set; }
        public virtual ICollection<ComissaoEleitoral> ComissaoEleitoral { get; set; }
        public virtual ICollection<DocumentoEleicao> Documentos { get; set; }
        
        public Eleicao()
        {
            Status = StatusEleicao.Planejamento;
            Chapas = new HashSet<ChapaEleicao>();
            ComissaoEleitoral = new HashSet<ComissaoEleitoral>();
            Documentos = new HashSet<DocumentoEleicao>();
        }
        
        public bool PodeInscreverChapa()
        {
            return Status == StatusEleicao.InscricoesAbertas && 
                   DateTime.Now >= DataInicioInscricao && 
                   DateTime.Now <= DataFimInscricao;
        }
        
        public bool PodeVotar()
        {
            return Status == StatusEleicao.VotacaoAberta && 
                   DateTime.Now >= DataInicioVotacao && 
                   DateTime.Now <= DataFimVotacao;
        }
        
        public void AbrirInscricoes()
        {
            if (Status != StatusEleicao.Planejamento)
                throw new InvalidOperationException("Eleição deve estar em planejamento para abrir inscrições");
                
            Status = StatusEleicao.InscricoesAbertas;
        }
        
        public void FecharInscricoes()
        {
            if (Status != StatusEleicao.InscricoesAbertas)
                throw new InvalidOperationException("Inscrições devem estar abertas para serem fechadas");
                
            Status = StatusEleicao.InscricoesFechadas;
        }
        
        public void AbrirVotacao()
        {
            if (Status != StatusEleicao.InscricoesFechadas)
                throw new InvalidOperationException("Inscrições devem estar fechadas para abrir votação");
                
            Status = StatusEleicao.VotacaoAberta;
        }
        
        public void FecharVotacao()
        {
            if (Status != StatusEleicao.VotacaoAberta)
                throw new InvalidOperationException("Votação deve estar aberta para ser fechada");
                
            Status = StatusEleicao.VotacaoFechada;
        }
        
        public void IniciarApuracao()
        {
            if (Status != StatusEleicao.VotacaoFechada)
                throw new InvalidOperationException("Votação deve estar fechada para iniciar apuração");
                
            Status = StatusEleicao.EmApuracao;
        }
        
        public void Finalizar()
        {
            if (Status != StatusEleicao.EmApuracao)
                throw new InvalidOperationException("Eleição deve estar em apuração para ser finalizada");
                
            Status = StatusEleicao.Finalizada;
        }
    }
    
    public enum TipoEleicao
    {
        ConselhoFederal = 1,
        ConselhoEstadual = 2,
        ComissaoEtica = 3,
        ComissaoEnsino = 4,
        DiretoriaRegional = 5
    }
    
    public enum StatusEleicao
    {
        Planejamento = 1,
        InscricoesAbertas = 2,
        InscricoesFechadas = 3,
        VotacaoAberta = 4,
        VotacaoFechada = 5,
        EmApuracao = 6,
        Finalizada = 7,
        Cancelada = 8
    }
}