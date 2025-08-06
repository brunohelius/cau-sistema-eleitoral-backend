using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaEleitoral.Domain.Entities;

namespace SistemaEleitoral.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
    {
    }

        // Calendários e Atividades
    public DbSet<Calendario> Calendarios { get; set; }
        public DbSet<AtividadeCalendario> AtividadesCalendario { get; set; }
        public DbSet<AtividadePrincipalCalendario> AtividadesPrincipaisCalendario { get; set; }
        public DbSet<AtividadeSecundariaCalendario> AtividadesSecundariasCalendario { get; set; }

        // Chapas e Membros
    public DbSet<ChapaEleicao> ChapasEleicao { get; set; }
    public DbSet<MembroChapa> MembrosChapa { get; set; }
        public DbSet<ChapaDocumento> ChapasDocumentos { get; set; }

        // Comissões
    public DbSet<ComissaoEleitoral> ComissoesEleitorais { get; set; }
    public DbSet<MembroComissao> MembrosComissao { get; set; }
        public DbSet<TipoMembroComissao> TiposMembrosComissao { get; set; }

        // Denúncias
    public DbSet<Denuncia> Denuncias { get; set; }
        public DbSet<DenunciaEncaminhamento> DenunciasEncaminhamentos { get; set; }
        public DbSet<DenunciaDocumento> DenunciasDocumentos { get; set; }
        public DbSet<DenunciaDefesa> DenunciasDefesas { get; set; }

        // Alegação Final
        public DbSet<AlegacaoFinal> AlegacaoFinal { get; set; }

        // Impugnações
    public DbSet<PedidoImpugnacao> PedidosImpugnacao { get; set; }
        public DbSet<PedidoImpugnacaoDefesa> PedidosImpugnacaoDefesas { get; set; }
        public DbSet<PedidoImpugnacaoDocumento> PedidosImpugnacaoDocumentos { get; set; }
        public DbSet<ImpugnacaoResultado> ImpugnacoesResultado { get; set; }
        public DbSet<AlegacaoImpugnacaoResultado> AlegacoesImpugnacaoResultado { get; set; }
        public DbSet<RecursoImpugnacaoResultado> RecursosImpugnacaoResultado { get; set; }
        public DbSet<ContrarrazaoImpugnacaoResultado> ContrarrazoesImpugnacaoResultado { get; set; }
        public DbSet<JulgamentoAlegacaoImpugResultado> JulgamentosAlegacaoImpugResultado { get; set; }
        public DbSet<JulgamentoRecursoImpugResultado> JulgamentosRecursoImpugResultado { get; set; }

        // Votação
        public DbSet<SessaoVotacao> SessoesVotacao { get; set; }
        public DbSet<Voto> Votos { get; set; }
        public DbSet<ComprovanteVotacao> ComprovantesVotacao { get; set; }

        // Resultados
        public DbSet<ResultadoApuracao> ResultadosApuracao { get; set; }
        public DbSet<ResultadoChapaApuracao> ResultadosChapasApuracao { get; set; }

        // Profissionais e Organizações
        public DbSet<Profissional> Profissionais { get; set; }
        public DbSet<OrganizacaoRegistroRegional> OrganizacoesRegistroRegional { get; set; }

        // Comunicação
        public DbSet<EmailLog> EmailLogs { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }
        public DbSet<Notificacao> Notificacoes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configurações das entidades
            builder.Entity<Calendario>()
                .HasKey(c => c.Id);

            builder.Entity<ChapaEleicao>()
                .HasKey(c => c.Id);

            builder.Entity<MembroChapa>()
                .HasKey(m => m.Id);

            builder.Entity<MembroChapa>()
                .HasOne(m => m.Chapa)
                .WithMany(c => c.Membros)
                .HasForeignKey(m => m.ChapaId);

            builder.Entity<Denuncia>()
                .HasKey(d => d.Id);

            builder.Entity<PedidoImpugnacao>()
                .HasKey(p => p.Id);

            builder.Entity<SessaoVotacao>()
                .HasKey(s => s.Id);

            builder.Entity<Voto>()
                .HasKey(v => v.Id);

            builder.Entity<ResultadoApuracao>()
                .HasKey(r => r.Id);

            builder.Entity<AlegacaoFinal>()
                .HasKey(a => a.Id);

            // Índices
            builder.Entity<ChapaEleicao>()
                .HasIndex(c => c.NumeroChapa)
                .IsUnique();

            builder.Entity<Voto>()
                .HasIndex(v => new { v.EleitorId, v.SessaoVotacaoId })
                .IsUnique();

            builder.Entity<EmailLog>()
                .HasIndex(e => e.Sucesso);

            builder.Entity<Notificacao>()
                .HasIndex(n => new { n.UsuarioId, n.Lida });
        }
    }

    public class ApplicationDbContextMinimal : ApplicationDbContext
    {
        public ApplicationDbContextMinimal(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}