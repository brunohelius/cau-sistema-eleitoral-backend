using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace SistemaEleitoral.Domain.Entities;

[Table("tb_usuarios", Schema = "public")]
public class Usuario : BaseEntity
{
    [Required]
    [MaxLength(100)]
    [Column("nome")]
    public string Nome { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(150)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(14)]
    [Column("cpf")]
    public string? Cpf { get; set; }
    
    [MaxLength(20)]
    [Column("telefone")]
    public string? Telefone { get; set; }
    
    [Column("usuario_cau_id")]
    public int? UsuarioCAUId { get; set; }
    
    [MaxLength(50)]
    [Column("numero_registro")]
    public string? NumeroRegistro { get; set; }
    
    [Column("data_ultimo_login")]
    public DateTime? DataUltimoLogin { get; set; }
    
    [Column("email_verificado")]
    public bool EmailVerificado { get; set; } = false;
    
    [MaxLength(500)]
    [Column("senha_hash")]
    public string? SenhaHash { get; set; }
    
    [MaxLength(100)]
    [Column("salt")]
    public string? Salt { get; set; }
    
    [Column("data_expiracao")]
    public DateTime? DataExpiracao { get; set; }
    
    [Column("conta_bloqueada")]
    public bool ContaBloqueada { get; set; } = false;
    
    [Column("tentativas_login")]
    public int TentativasLogin { get; set; } = 0;
    
    [Column("data_ultimo_bloqueio")]
    public DateTime? DataUltimoBloqueio { get; set; }
    
    // Claims personalizadas para sistema eleitoral
    [MaxLength(50)]
    [Column("uf_origem")]
    public string? UfOrigem { get; set; }
    
    [Column("filial_id")]
    public int? FilialId { get; set; }
    
    [MaxLength(20)]
    [Column("nivel_acesso")]
    public string? NivelAcesso { get; set; } // "NACIONAL", "ESTADUAL", "REGIONAL"
    
    // Relacionamentos
    public virtual ICollection<UsuarioPermissao> Permissoes { get; set; } = new List<UsuarioPermissao>();
    public virtual ICollection<LogUsuario> LogsUsuario { get; set; } = new List<LogUsuario>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<UsuarioRole> Roles { get; set; } = new List<UsuarioRole>();
}

[Table("tb_log_usuarios", Schema = "public")]
public class LogUsuario : BaseEntity
{
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;
    
    [MaxLength(50)]
    [Column("acao")]
    public string Acao { get; set; } = string.Empty;
    
    [MaxLength(500)]
    [Column("detalhes")]
    public string? Detalhes { get; set; }
    
    [MaxLength(50)]
    [Column("endereco_ip")]
    public string? EnderecoIp { get; set; }
    
    [MaxLength(200)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }
}

// RefreshToken movido para arquivo separado RefreshToken.cs

[Table("tb_roles", Schema = "public")]
public class Role : BaseEntity
{
    [Required]
    [MaxLength(100)]
    [Column("nome")]
    public string Nome { get; set; } = string.Empty;
    
    [MaxLength(200)]
    [Column("descricao")]
    public string? Descricao { get; set; }
    
    [Required]
    [MaxLength(50)]
    [Column("codigo")]
    public string Codigo { get; set; } = string.Empty;
    
    [Column("nivel")]
    public int Nivel { get; set; } = 0; // Hierarquia de permissões
    
    [MaxLength(50)]
    [Column("contexto")]
    public string? Contexto { get; set; } // "NACIONAL", "ESTADUAL", "REGIONAL"
    
    // Relacionamentos
    public virtual ICollection<UsuarioRole> UsuarioRoles { get; set; } = new List<UsuarioRole>();
    public virtual ICollection<RolePermissao> RolePermissoes { get; set; } = new List<RolePermissao>();
}

[Table("tb_usuario_roles", Schema = "public")]
public class UsuarioRole : BaseEntity
{
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;
    
    [Column("role_id")]
    public int RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;
    
    [Column("data_expiracao")]
    public DateTime? DataExpiracao { get; set; }
    
    [MaxLength(50)]
    [Column("uf_escopo")]
    public string? UfEscopo { get; set; } // Para roles estaduais
    
    [Column("filial_escopo")]
    public int? FilialEscopo { get; set; } // Para roles regionais
}

[Table("tb_role_permissoes", Schema = "public")]
public class RolePermissao : BaseEntity
{
    [Column("role_id")]
    public int RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;
    
    [Column("permissao_id")]
    public int PermissaoId { get; set; }
    public virtual Permissao Permissao { get; set; } = null!;
    
    [Column("concedido")]
    public bool Concedido { get; set; } = true;
    
    [Column("data_expiracao")]
    public DateTime? DataExpiracao { get; set; }
}

[Table("tb_sessao_login", Schema = "public")]
public class SessaoLogin : BaseEntity
{
    [Column("usuario_id")]
    public int UsuarioId { get; set; }
    public virtual Usuario Usuario { get; set; } = null!;
    
    [Required]
    [MaxLength(500)]
    [Column("jwt_id")]
    public string JwtId { get; set; } = string.Empty; // JTI do JWT
    
    [Column("data_inicio")]
    public DateTime DataInicio { get; set; } = DateTime.UtcNow;
    
    [Column("data_fim")]
    public DateTime? DataFim { get; set; }
    
    [MaxLength(50)]
    [Column("endereco_ip")]
    public string? EnderecoIp { get; set; }
    
    [MaxLength(200)]
    [Column("user_agent")]
    public string? UserAgent { get; set; }
    
    [Column("sessao_ativa")]
    public bool SessaoAtiva { get; set; } = true;
    
    [MaxLength(100)]
    [Column("tipo_logout")]
    public string? TipoLogout { get; set; } // "MANUAL", "EXPIRACAO", "REVOGACAO", "ADMIN"
}