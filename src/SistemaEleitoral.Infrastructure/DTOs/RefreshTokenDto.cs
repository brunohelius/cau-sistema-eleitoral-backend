namespace SistemaEleitoral.Infrastructure.DTOs
{
    public class RefreshTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public string? UserId { get; set; }
    }
}