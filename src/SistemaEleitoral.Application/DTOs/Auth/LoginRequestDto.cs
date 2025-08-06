using System.ComponentModel.DataAnnotations;

namespace SistemaEleitoral.Application.DTOs.Auth
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email é obrigatório")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Senha { get; set; }
        
        public bool ManterConectado { get; set; }
    }
}