using System;
using System.Collections.Generic;

namespace SistemaEleitoral.Domain.Entities.Diplomacao
{
    /// <summary>
    /// Entidade que representa o termo de posse dos eleitos
    /// </summary>
    public class TermoPosse : BaseEntity
    {
        public string NumeroTermo { get; set; }
        public int DiplomaEleitoralId { get; set; }
        public virtual DiplomaEleitoral DiplomaEleitoral { get; set; }
        
        public int EmpossadoId { get; set; }
        public virtual Profissional Empossado { get; set; }
        
        public string Cargo { get; set; }
        public DateTime DataPosse { get; set; }
        public TimeSpan HoraPosse { get; set; }
        public string LocalPosse { get; set; }
        
        // Mandato
        public DateTime InicioMandato { get; set; }
        public DateTime FimMandato { get; set; }
        public int DuracaoMandatoAnos { get; set; }
        
        // Cerimônia
        public string PresidenteCerimonia { get; set; }
        public string SecretarioCerimonia { get; set; }
        public string MestresCerimonia { get; set; }
        
        // Juramento
        public string TextoJuramento { get; set; }
        public bool JuramentoPrestado { get; set; }
        public DateTime? DataHoraJuramento { get; set; }
        
        // Testemunhas
        public virtual ICollection<TestemunhaPosse> Testemunhas { get; set; }
        
        // Documentação
        public string AtaPosse { get; set; }
        public string CaminhoArquivoAta { get; set; }
        public string CaminhoFotos { get; set; }
        public string CaminhoVideo { get; set; }
        
        // Status
        public StatusPosse Status { get; set; }
        public string Observacoes { get; set; }
        
        // Finalização do mandato anterior (se houver)
        public int? MandatoAnteriorId { get; set; }
        public virtual MandatoConselheiro MandatoAnterior { get; set; }
        public TipoFinalizacaoMandato? TipoFinalizacaoAnterior { get; set; }
        
        public TermoPosse()
        {
            Status = StatusPosse.Agendada;
            Testemunhas = new HashSet<TestemunhaPosse>();
            NumeroTermo = GerarNumeroTermo();
            DuracaoMandatoAnos = 3; // Padrão CAU
        }
        
        private string GerarNumeroTermo()
        {
            var ano = DateTime.Now.Year;
            var sequencial = new Random().Next(100, 999);
            return $"POSSE-{sequencial}/{ano}";
        }
        
        public void RegistrarJuramento()
        {
            if (Status != StatusPosse.EmAndamento)
                throw new InvalidOperationException("Posse deve estar em andamento");
                
            JuramentoPrestado = true;
            DataHoraJuramento = DateTime.Now;
        }
        
        public void ConcluirPosse()
        {
            if (!JuramentoPrestado)
                throw new InvalidOperationException("Juramento deve ser prestado antes de concluir a posse");
                
            Status = StatusPosse.Concluida;
            InicioMandato = DataPosse;
            FimMandato = DataPosse.AddYears(DuracaoMandatoAnos);
        }
        
        public void CancelarPosse(string motivo)
        {
            Status = StatusPosse.Cancelada;
            Observacoes = $"Posse cancelada: {motivo}";
        }
        
        public void AdicionarTestemunha(string nome, string cpf, string cargo)
        {
            Testemunhas.Add(new TestemunhaPosse
            {
                TermoPosseId = this.Id,
                Nome = nome,
                CPF = cpf,
                Cargo = cargo,
                DataAssinatura = DateTime.Now
            });
        }
    }
    
    public class TestemunhaPosse : BaseEntity
    {
        public int TermoPosseId { get; set; }
        public virtual TermoPosse TermoPosse { get; set; }
        
        public string Nome { get; set; }
        public string CPF { get; set; }
        public string Cargo { get; set; }
        public DateTime DataAssinatura { get; set; }
        public string Assinatura { get; set; }
    }
    
    public class MandatoConselheiro : BaseEntity
    {
        public int ConselheiroId { get; set; }
        public virtual Conselheiro Conselheiro { get; set; }
        
        public int EleicaoId { get; set; }
        public virtual Eleicao Eleicao { get; set; }
        
        public string Cargo { get; set; }
        public DateTime InicioMandato { get; set; }
        public DateTime FimMandatoPrevisto { get; set; }
        public DateTime? FimMandatoReal { get; set; }
        
        public StatusMandato Status { get; set; }
        public TipoFinalizacaoMandato? TipoFinalizacao { get; set; }
        public string MotivoFinalizacao { get; set; }
        
        // Substituições
        public virtual ICollection<SubstituicaoMandato> Substituicoes { get; set; }
        
        // Histórico
        public virtual ICollection<HistoricoMandato> Historicos { get; set; }
        
        public MandatoConselheiro()
        {
            Status = StatusMandato.Ativo;
            Substituicoes = new HashSet<SubstituicaoMandato>();
            Historicos = new HashSet<HistoricoMandato>();
        }
        
        public void FinalizarMandato(TipoFinalizacaoMandato tipo, string motivo)
        {
            Status = StatusMandato.Finalizado;
            TipoFinalizacao = tipo;
            MotivoFinalizacao = motivo;
            FimMandatoReal = DateTime.Now;
            
            AdicionarHistorico($"Mandato finalizado: {tipo} - {motivo}");
        }
        
        public void SuspenderMandato(string motivo)
        {
            Status = StatusMandato.Suspenso;
            AdicionarHistorico($"Mandato suspenso: {motivo}");
        }
        
        public void ReativarMandato()
        {
            if (Status != StatusMandato.Suspenso)
                throw new InvalidOperationException("Apenas mandatos suspensos podem ser reativados");
                
            Status = StatusMandato.Ativo;
            AdicionarHistorico("Mandato reativado");
        }
        
        private void AdicionarHistorico(string descricao)
        {
            Historicos.Add(new HistoricoMandato
            {
                MandatoConselheiroId = this.Id,
                Data = DateTime.Now,
                Descricao = descricao,
                Status = this.Status
            });
        }
    }
    
    public class SubstituicaoMandato : BaseEntity
    {
        public int MandatoTitularId { get; set; }
        public virtual MandatoConselheiro MandatoTitular { get; set; }
        
        public int SubstitutoId { get; set; }
        public virtual Conselheiro Substituto { get; set; }
        
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public string MotivoSubstituicao { get; set; }
        public TipoSubstituicao TipoSubstituicao { get; set; }
        public StatusSubstituicao Status { get; set; }
        
        // Documento que autoriza a substituição
        public string NumeroAto { get; set; }
        public DateTime DataAto { get; set; }
        public string CaminhoDocumento { get; set; }
        
        public SubstituicaoMandato()
        {
            Status = StatusSubstituicao.Ativa;
        }
        
        public void EncerrarSubstituicao()
        {
            DataFim = DateTime.Now;
            Status = StatusSubstituicao.Encerrada;
        }
    }
    
    public class HistoricoMandato : BaseEntity
    {
        public int MandatoConselheiroId { get; set; }
        public virtual MandatoConselheiro MandatoConselheiro { get; set; }
        
        public DateTime Data { get; set; }
        public string Descricao { get; set; }
        public StatusMandato Status { get; set; }
        public int? ResponsavelId { get; set; }
    }
    
    public enum StatusPosse
    {
        Agendada = 1,
        EmAndamento = 2,
        Concluida = 3,
        Cancelada = 4,
        Adiada = 5
    }
    
    public enum StatusMandato
    {
        Ativo = 1,
        Suspenso = 2,
        Finalizado = 3,
        Renunciado = 4,
        Cassado = 5
    }
    
    public enum TipoFinalizacaoMandato
    {
        TerminoNatural = 1,
        Renuncia = 2,
        Cassacao = 3,
        Falecimento = 4,
        Incompatibilidade = 5,
        Judicial = 6,
        Outros = 7
    }
    
    public enum TipoSubstituicao
    {
        Temporaria = 1,
        Definitiva = 2,
        Licenca = 3,
        Ferias = 4,
        Afastamento = 5
    }
    
    public enum StatusSubstituicao
    {
        Ativa = 1,
        Encerrada = 2,
        Cancelada = 3
    }
}