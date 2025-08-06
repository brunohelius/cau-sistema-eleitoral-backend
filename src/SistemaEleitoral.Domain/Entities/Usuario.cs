using System;
using System.Collections.Generic;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que representa um usuário do sistema eleitoral
    /// </summary>
    public class Usuario : BaseEntity
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Cpf { get; set; }
        public string Senha { get; set; } // Hash
        public string Telefone { get; set; }
        public TipoUsuario TipoUsuario { get; set; }
        public StatusUsuario Status { get; set; }
        public DateTime? UltimoAcesso { get; set; }
        public string TokenRecuperacao { get; set; }
        public DateTime? ValidadeToken { get; set; }
        
        // Relacionamentos
        public int? ProfissionalId { get; set; }
        public virtual Profissional Profissional { get; set; }
        
        public int? ConselheiroId { get; set; }
        public virtual Conselheiro Conselheiro { get; set; }
        
        public virtual ICollection<HistoricoAcesso> HistoricosAcesso { get; set; }
        
        public Usuario()
        {
            Status = StatusUsuario.Ativo;
            HistoricosAcesso = new HashSet<HistoricoAcesso>();
        }
        
        public void AtualizarUltimoAcesso()
        {
            UltimoAcesso = DateTime.Now;
        }
        
        public void GerarTokenRecuperacao()
        {
            TokenRecuperacao = Guid.NewGuid().ToString();
            ValidadeToken = DateTime.Now.AddHours(24);
        }
        
        public bool TokenValido(string token)
        {
            return TokenRecuperacao == token && ValidadeToken > DateTime.Now;
        }
    }
    
    public enum TipoUsuario
    {
        Administrador = 1,
        ComissaoEleitoral = 2,
        Profissional = 3,
        Conselheiro = 4,
        Publico = 5
    }
    
    public enum StatusUsuario
    {
        Ativo = 1,
        Inativo = 2,
        Bloqueado = 3,
        PendenteConfirmacao = 4
    }
}