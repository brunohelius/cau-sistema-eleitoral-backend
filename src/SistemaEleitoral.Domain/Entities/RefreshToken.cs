using System;

namespace SistemaEleitoral.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public string Token { get; set; } = "";
        public string UserId { get; set; } = "";
        public ApplicationUser User { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}