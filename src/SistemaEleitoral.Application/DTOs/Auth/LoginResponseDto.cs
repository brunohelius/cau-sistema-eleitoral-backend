namespace SistemaEleitoral.Application.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; }
        public UsuarioDto Usuario { get; set; }
    }
    
    public class UsuarioDto
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Email { get; set; }
        public string TipoUsuario { get; set; }
        public int? ProfissionalId { get; set; }
        public int? ConselheiroId { get; set; }
    }
}