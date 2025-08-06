using System;
using System.Collections.Generic;

namespace SistemaEleitoral.Domain.Entities.Diplomacao
{
    /// <summary>
    /// Entidade que representa o diploma eleitoral dos eleitos
    /// </summary>
    public class DiplomaEleitoral : BaseEntity
    {
        public string NumeroRegistro { get; set; }
        public int EleicaoId { get; set; }
        public virtual Eleicao Eleicao { get; set; }
        
        public int ChapaEleicaoId { get; set; }
        public virtual ChapaEleicao ChapaEleicao { get; set; }
        
        public int MembroChapaId { get; set; }
        public virtual MembroChapa MembroChapa { get; set; }
        
        public TipoDiploma TipoDiploma { get; set; }
        public string Cargo { get; set; }
        public DateTime DataExpedicao { get; set; }
        public DateTime DataValidadeInicio { get; set; }
        public DateTime DataValidadeFim { get; set; }
        
        // Texto do diploma
        public string TextoDiploma { get; set; }
        public string Considerandos { get; set; }
        
        // Assinaturas
        public virtual ICollection<AssinaturaDiploma> Assinaturas { get; set; }
        
        // Status
        public StatusDiploma Status { get; set; }
        public DateTime? DataEntrega { get; set; }
        public string LocalEntrega { get; set; }
        public string RecebidoPor { get; set; }
        
        // Arquivo digital
        public string CaminhoArquivoPDF { get; set; }
        public string HashDocumento { get; set; }
        
        // Observações
        public string Observacoes { get; set; }
        
        public DiplomaEleitoral()
        {
            DataExpedicao = DateTime.Now;
            Status = StatusDiploma.EmElaboracao;
            Assinaturas = new HashSet<AssinaturaDiploma>();
            NumeroRegistro = GerarNumeroRegistro();
        }
        
        private string GerarNumeroRegistro()
        {
            var ano = DateTime.Now.Year;
            var sequencial = new Random().Next(1000, 9999);
            return $"DIP-{sequencial}/{ano}";
        }
        
        public void AssinarDigitalmente(int assinanteId, string certificadoDigital)
        {
            var assinatura = new AssinaturaDiploma
            {
                DiplomaEleitoralId = this.Id,
                AssinanteId = assinanteId,
                DataAssinatura = DateTime.Now,
                TipoAssinatura = TipoAssinatura.Digital,
                CertificadoDigital = certificadoDigital,
                HashAssinatura = GerarHashAssinatura(certificadoDigital)
            };
            
            Assinaturas.Add(assinatura);
            
            // Se todas as assinaturas necessárias foram coletadas
            if (VerificarAssinaturasCompletas())
            {
                Status = StatusDiploma.Assinado;
            }
        }
        
        public void RegistrarEntrega(string local, string recebidoPor)
        {
            if (Status != StatusDiploma.Assinado)
                throw new InvalidOperationException("Diploma deve estar assinado para ser entregue");
                
            DataEntrega = DateTime.Now;
            LocalEntrega = local;
            RecebidoPor = recebidoPor;
            Status = StatusDiploma.Entregue;
        }
        
        private bool VerificarAssinaturasCompletas()
        {
            // Verificar se tem pelo menos as assinaturas obrigatórias
            // Presidente da Comissão + Secretário
            return Assinaturas.Count >= 2;
        }
        
        private string GerarHashAssinatura(string certificado)
        {
            // Implementar geração de hash
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
    }
    
    public class AssinaturaDiploma : BaseEntity
    {
        public int DiplomaEleitoralId { get; set; }
        public virtual DiplomaEleitoral DiplomaEleitoral { get; set; }
        
        public int AssinanteId { get; set; }
        public virtual MembroComissao Assinante { get; set; }
        
        public DateTime DataAssinatura { get; set; }
        public TipoAssinatura TipoAssinatura { get; set; }
        public string CertificadoDigital { get; set; }
        public string HashAssinatura { get; set; }
        public string IPAssinatura { get; set; }
        public bool Valida { get; set; }
        
        public AssinaturaDiploma()
        {
            Valida = true;
        }
    }
    
    public enum TipoDiploma
    {
        Titular = 1,
        Suplente = 2,
        Substituto = 3
    }
    
    public enum StatusDiploma
    {
        EmElaboracao = 1,
        AguardandoAssinatura = 2,
        Assinado = 3,
        Registrado = 4,
        Entregue = 5,
        Cancelado = 6
    }
    
    public enum TipoAssinatura
    {
        Digital = 1,
        Fisica = 2,
        Eletronica = 3
    }
}