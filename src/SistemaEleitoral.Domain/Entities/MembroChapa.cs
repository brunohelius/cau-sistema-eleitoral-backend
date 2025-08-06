using System;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa um membro de uma chapa eleitoral
    /// </summary>
    public class MembroChapa : BaseEntity
    {
        public int ChapaEleicaoId { get; set; }
        public virtual ChapaEleicao ChapaEleicao { get; set; }
        
        public int ProfissionalId { get; set; }
        public virtual Profissional Profissional { get; set; }
        
        public TipoMembroChapa TipoMembro { get; set; }
        public int Ordem { get; set; }
        public string Cargo { get; set; }
        public bool Titular { get; set; }
        
        // Informações adicionais
        public string MiniCurriculo { get; set; }
        public string PropostasIndividuais { get; set; }
        public string FotoUrl { get; set; }
        
        // Validações
        public bool DocumentacaoCompleta { get; set; }
        public bool Elegivel { get; set; }
        public string MotivoInelegibilidade { get; set; }
        public DateTime? DataValidacao { get; set; }
        
        public MembroChapa()
        {
            Titular = true;
            DocumentacaoCompleta = false;
            Elegivel = true;
        }
        
        public void ValidarDocumentacao(bool completa, bool elegivel, string motivoInelegibilidade = null)
        {
            DocumentacaoCompleta = completa;
            Elegivel = elegivel;
            MotivoInelegibilidade = motivoInelegibilidade;
            DataValidacao = DateTime.Now;
        }
    }
    
    public enum TipoMembroChapa
    {
        Presidente = 1,
        VicePresidente = 2,
        Conselheiro = 3,
        Suplente = 4,
        Coordenador = 5,
        Secretario = 6,
        Tesoureiro = 7,
        Vogal = 8
    }
}