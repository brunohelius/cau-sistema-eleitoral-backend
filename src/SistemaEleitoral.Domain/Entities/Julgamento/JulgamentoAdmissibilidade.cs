using System;
using System.Collections.Generic;

namespace SistemaEleitoral.Domain.Entities.Julgamento
{
    /// <summary>
    /// Entidade que representa o julgamento de admissibilidade de recursos e denúncias
    /// </summary>
    public class JulgamentoAdmissibilidade : BaseEntity
    {
        public TipoProcessoJulgamento TipoProcesso { get; set; }
        public int ProcessoId { get; set; } // ID da Denúncia, Recurso ou Impugnação
        
        // Dados do Relator
        public int RelatorId { get; set; }
        public virtual MembroComissao Relator { get; set; }
        
        // Datas
        public DateTime DataDistribuicao { get; set; }
        public DateTime? DataJulgamento { get; set; }
        public DateTime PrazoAnalise { get; set; }
        
        // Decisão
        public ResultadoAdmissibilidade Resultado { get; set; }
        public string Fundamentacao { get; set; }
        public string ObservacoesRelator { get; set; }
        
        // Requisitos analisados
        public bool Tempestividade { get; set; }
        public bool Legitimidade { get; set; }
        public bool Interesse { get; set; }
        public bool RequisitosFormal { get; set; }
        public string AnaliseRequisitos { get; set; }
        
        // Documentos
        public virtual ICollection<DocumentoJulgamento> Documentos { get; set; }
        
        // Votação (se colegiado)
        public bool JulgamentoColegiado { get; set; }
        public virtual ICollection<VotoAdmissibilidade> Votos { get; set; }
        
        public StatusJulgamento Status { get; set; }
        
        public JulgamentoAdmissibilidade()
        {
            DataDistribuicao = DateTime.Now;
            Status = StatusJulgamento.PendenteAnalise;
            Documentos = new HashSet<DocumentoJulgamento>();
            Votos = new HashSet<VotoAdmissibilidade>();
        }
        
        public void Admitir(string fundamentacao)
        {
            if (Status != StatusJulgamento.PendenteAnalise)
                throw new InvalidOperationException("Julgamento já foi realizado");
                
            Resultado = ResultadoAdmissibilidade.Admitido;
            Fundamentacao = fundamentacao;
            DataJulgamento = DateTime.Now;
            Status = StatusJulgamento.Julgado;
        }
        
        public void NaoAdmitir(string fundamentacao)
        {
            if (Status != StatusJulgamento.PendenteAnalise)
                throw new InvalidOperationException("Julgamento já foi realizado");
                
            Resultado = ResultadoAdmissibilidade.NaoAdmitido;
            Fundamentacao = fundamentacao;
            DataJulgamento = DateTime.Now;
            Status = StatusJulgamento.Julgado;
        }
        
        public bool VerificarRequisitos()
        {
            return Tempestividade && Legitimidade && Interesse && RequisitosFormal;
        }
    }
    
    public class VotoAdmissibilidade : BaseEntity
    {
        public int JulgamentoAdmissibilidadeId { get; set; }
        public virtual JulgamentoAdmissibilidade JulgamentoAdmissibilidade { get; set; }
        
        public int MembroComissaoId { get; set; }
        public virtual MembroComissao MembroComissao { get; set; }
        
        public ResultadoAdmissibilidade Voto { get; set; }
        public string Fundamentacao { get; set; }
        public DateTime DataVoto { get; set; }
        public bool Divergente { get; set; }
    }
    
    public enum TipoProcessoJulgamento
    {
        Denuncia = 1,
        RecursoImpugnacao = 2,
        RecursoDenuncia = 3,
        RecursoResultado = 4,
        PedidoSubstituicao = 5
    }
    
    public enum ResultadoAdmissibilidade
    {
        PendenteAnalise = 1,
        Admitido = 2,
        NaoAdmitido = 3,
        ParcialmenteAdmitido = 4
    }
    
    public enum StatusJulgamento
    {
        PendenteAnalise = 1,
        EmAnalise = 2,
        AguardandoJulgamento = 3,
        Julgado = 4,
        Arquivado = 5
    }
}