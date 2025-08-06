using Microsoft.AspNetCore.Identity;

namespace SistemaEleitoral.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? NomeCompleto { get; set; }
        public string? CPF { get; set; }
        public int? ProfissionalId { get; set; }
        public virtual Profissional? Profissional { get; set; }
        public DateTime DataCadastro { get; set; }
        public DateTime? UltimoAcesso { get; set; }
        public bool Ativo { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}