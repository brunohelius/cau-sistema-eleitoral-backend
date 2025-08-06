using System;

namespace SistemaEleitoral.Domain.Entities
{
    /// <summary>
    /// Entidade que registra o histórico de acessos dos usuários
    /// </summary>
    public class HistoricoAcesso : BaseEntity
    {
        public int UsuarioId { get; set; }
        public virtual Usuario Usuario { get; set; }
        
        public DateTime DataHoraAcesso { get; set; }
        public string IpAcesso { get; set; }
        public string UserAgent { get; set; }
        public TipoAcesso TipoAcesso { get; set; }
        public bool Sucesso { get; set; }
        public string Observacao { get; set; }
        
        public HistoricoAcesso()
        {
            DataHoraAcesso = DateTime.Now;
        }
    }
    
    public enum TipoAcesso
    {
        Login = 1,
        Logout = 2,
        TentativaLogin = 3,
        RecuperacaoSenha = 4,
        AlteracaoSenha = 5
    }
}