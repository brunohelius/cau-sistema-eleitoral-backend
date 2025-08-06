using System;
using System.Collections.Generic;

namespace SistemaEleitoral.Domain.Entities.Julgamento
{
    /// <summary>
    /// Entidade que representa um recurso eleitoral genérico
    /// </summary>
    public class RecursoEleitoral : BaseEntity
    {
        public TipoRecurso TipoRecurso { get; set; }
        public int ProcessoOrigemId { get; set; } // ID do processo que originou o recurso
        
        // Recorrente
        public int RecorrenteId { get; set; }
        public virtual Profissional Recorrente { get; set; }
        public string AdvogadoRecorrente { get; set; }
        public string OABAdvogado { get; set; }
        
        // Recorrido
        public int? RecorridoId { get; set; }
        public virtual Profissional Recorrido { get; set; }
        
        // Dados do Recurso
        public string NumeroProcesso { get; set; }
        public DateTime DataInterposicao { get; set; }
        public DateTime PrazoFinal { get; set; }
        public string Fundamentacao { get; set; }
        public string PedidoRecurso { get; set; }
        
        // Status
        public StatusRecurso Status { get; set; }
        public bool Tempestivo { get; set; }
        
        // Admissibilidade
        public int? JulgamentoAdmissibilidadeId { get; set; }
        public virtual JulgamentoAdmissibilidade JulgamentoAdmissibilidade { get; set; }
        
        // Contrarrazões
        public virtual ICollection<ContrarrazaoRecurso> Contrarrazoes { get; set; }
        
        // Julgamento Final
        public int? JulgamentoRecursoId { get; set; }
        public virtual JulgamentoRecurso JulgamentoRecurso { get; set; }
        
        // Documentos
        public virtual ICollection<DocumentoRecurso> Documentos { get; set; }
        
        // Histórico
        public virtual ICollection<HistoricoRecurso> Historicos { get; set; }
        
        public RecursoEleitoral()
        {
            DataInterposicao = DateTime.Now;
            Status = StatusRecurso.Protocolado;
            Contrarrazoes = new HashSet<ContrarrazaoRecurso>();
            Documentos = new HashSet<DocumentoRecurso>();
            Historicos = new HashSet<HistoricoRecurso>();
        }
        
        public void VerificarTempestividade()
        {
            Tempestivo = DataInterposicao <= PrazoFinal;
            if (!Tempestivo)
            {
                Status = StatusRecurso.Intempestivo;
            }
        }
        
        public void AdmitirRecurso()
        {
            if (Status != StatusRecurso.AguardandoAdmissibilidade)
                throw new InvalidOperationException("Recurso não está aguardando admissibilidade");
                
            Status = StatusRecurso.Admitido;
        }
        
        public void NaoAdmitirRecurso(string motivo)
        {
            if (Status != StatusRecurso.AguardandoAdmissibilidade)
                throw new InvalidOperationException("Recurso não está aguardando admissibilidade");
                
            Status = StatusRecurso.NaoAdmitido;
            AdicionarHistorico($"Recurso não admitido: {motivo}");
        }
        
        private void AdicionarHistorico(string descricao)
        {
            Historicos.Add(new HistoricoRecurso
            {
                RecursoEleitoralId = this.Id,
                Data = DateTime.Now,
                Descricao = descricao,
                Status = this.Status
            });
        }
    }
    
    public class ContrarrazaoRecurso : BaseEntity
    {
        public int RecursoEleitoralId { get; set; }
        public virtual RecursoEleitoral RecursoEleitoral { get; set; }
        
        public int AutorId { get; set; }
        public virtual Profissional Autor { get; set; }
        
        public DateTime DataApresentacao { get; set; }
        public string Argumentacao { get; set; }
        public string PedidoContrarrazao { get; set; }
        
        public bool Tempestiva { get; set; }
        public StatusContrarrazao Status { get; set; }
        
        public virtual ICollection<DocumentoContrarrazao> Documentos { get; set; }
        
        public ContrarrazaoRecurso()
        {
            DataApresentacao = DateTime.Now;
            Status = StatusContrarrazao.Apresentada;
            Documentos = new HashSet<DocumentoContrarrazao>();
        }
    }
    
    public class JulgamentoRecurso : BaseEntity
    {
        public int RecursoEleitoralId { get; set; }
        public virtual RecursoEleitoral RecursoEleitoral { get; set; }
        
        // Relator
        public int RelatorId { get; set; }
        public virtual MembroComissao Relator { get; set; }
        
        // Revisor (se houver)
        public int? RevisorId { get; set; }
        public virtual MembroComissao Revisor { get; set; }
        
        // Julgamento
        public DateTime DataJulgamento { get; set; }
        public ResultadoJulgamentoRecurso Resultado { get; set; }
        public string Acordao { get; set; }
        public string Ementa { get; set; }
        public string VotoRelator { get; set; }
        public string VotoRevisor { get; set; }
        
        // Votação
        public int VotosFavoraveis { get; set; }
        public int VotosContrarios { get; set; }
        public int Abstencoes { get; set; }
        
        public virtual ICollection<VotoJulgamentoRecurso> Votos { get; set; }
        
        // Efeitos
        public EfeitoJulgamento Efeito { get; set; }
        public bool EfeitoSuspensivo { get; set; }
        
        public JulgamentoRecurso()
        {
            Votos = new HashSet<VotoJulgamentoRecurso>();
        }
        
        public void ProferirDecisao(ResultadoJulgamentoRecurso resultado, string acordao)
        {
            Resultado = resultado;
            Acordao = acordao;
            DataJulgamento = DateTime.Now;
            
            // Atualizar status do recurso
            if (resultado == ResultadoJulgamentoRecurso.Provido)
            {
                RecursoEleitoral.Status = StatusRecurso.Provido;
            }
            else if (resultado == ResultadoJulgamentoRecurso.NaoProvido)
            {
                RecursoEleitoral.Status = StatusRecurso.NaoProvido;
            }
            else
            {
                RecursoEleitoral.Status = StatusRecurso.ParcialmenteProvido;
            }
        }
    }
    
    public class VotoJulgamentoRecurso : BaseEntity
    {
        public int JulgamentoRecursoId { get; set; }
        public virtual JulgamentoRecurso JulgamentoRecurso { get; set; }
        
        public int MembroComissaoId { get; set; }
        public virtual MembroComissao MembroComissao { get; set; }
        
        public TipoVoto TipoVoto { get; set; }
        public string Fundamentacao { get; set; }
        public DateTime DataVoto { get; set; }
        public bool VotoDivergente { get; set; }
        public string DeclaracaoVoto { get; set; }
    }
    
    public class HistoricoRecurso : BaseEntity
    {
        public int RecursoEleitoralId { get; set; }
        public virtual RecursoEleitoral RecursoEleitoral { get; set; }
        
        public DateTime Data { get; set; }
        public string Descricao { get; set; }
        public StatusRecurso Status { get; set; }
        public int? ResponsavelId { get; set; }
    }
    
    public class DocumentoRecurso : BaseEntity
    {
        public int RecursoEleitoralId { get; set; }
        public virtual RecursoEleitoral RecursoEleitoral { get; set; }
        
        public string NomeArquivo { get; set; }
        public string CaminhoArquivo { get; set; }
        public TipoDocumentoRecurso TipoDocumento { get; set; }
        public DateTime DataUpload { get; set; }
        public int UploadPorId { get; set; }
    }
    
    public class DocumentoContrarrazao : BaseEntity
    {
        public int ContrarrazaoRecursoId { get; set; }
        public virtual ContrarrazaoRecurso ContrarrazaoRecurso { get; set; }
        
        public string NomeArquivo { get; set; }
        public string CaminhoArquivo { get; set; }
        public DateTime DataUpload { get; set; }
    }
    
    public class DocumentoJulgamento : BaseEntity
    {
        public int JulgamentoId { get; set; }
        public string TipoJulgamento { get; set; } // Admissibilidade, Recurso, Final
        
        public string NomeArquivo { get; set; }
        public string CaminhoArquivo { get; set; }
        public TipoDocumentoJulgamento TipoDocumento { get; set; }
        public DateTime DataUpload { get; set; }
    }
    
    public enum TipoRecurso
    {
        Ordinario = 1,
        Especial = 2,
        Extraordinario = 3,
        Agravo = 4,
        Embargos = 5
    }
    
    public enum StatusRecurso
    {
        Protocolado = 1,
        AguardandoAdmissibilidade = 2,
        Admitido = 3,
        NaoAdmitido = 4,
        Intempestivo = 5,
        AguardandoContrarrazoes = 6,
        AguardandoJulgamento = 7,
        Provido = 8,
        NaoProvido = 9,
        ParcialmenteProvido = 10,
        Desistencia = 11,
        Arquivado = 12
    }
    
    public enum StatusContrarrazao
    {
        Apresentada = 1,
        Aceita = 2,
        Rejeitada = 3,
        Intempestiva = 4
    }
    
    public enum ResultadoJulgamentoRecurso
    {
        Provido = 1,
        NaoProvido = 2,
        ParcialmenteProvido = 3,
        NaoConhecido = 4,
        Prejudicado = 5
    }
    
    public enum EfeitoJulgamento
    {
        Imediato = 1,
        Suspensivo = 2,
        Devolutivo = 3
    }
    
    public enum TipoVoto
    {
        Favoravel = 1,
        Contrario = 2,
        Abstencao = 3,
        Impedido = 4,
        Suspeito = 5
    }
    
    public enum TipoDocumentoRecurso
    {
        PeticaoRecurso = 1,
        Procuracao = 2,
        DocumentoComprobatorio = 3,
        Jurisprudencia = 4,
        ParecerTecnico = 5
    }
    
    public enum TipoDocumentoJulgamento
    {
        Acordao = 1,
        VotoRelator = 2,
        VotoVogal = 3,
        VotoDivergente = 4,
        Ata = 5,
        Certidao = 6
    }
}