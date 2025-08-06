using Microsoft.Extensions.DependencyInjection;
using SistemaEleitoral.Application.Services;
using SistemaEleitoral.Domain.Interfaces.Repositories;
using SistemaEleitoral.Domain.Interfaces.Services;
using SistemaEleitoral.Infrastructure.Repositories;
using SistemaEleitoral.Infrastructure.Services;

namespace SistemaEleitoral.Api.Configuration
{
    /// <summary>
    /// Configuração de injeção de dependência dos services
    /// </summary>
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Services de Domínio (Business Logic)
            services.AddScoped<ICalendarioService, CalendarioService>();
            services.AddScoped<IChapaEleicaoService, ChapaEleicaoService>();
            services.AddScoped<IComissaoEleitoralService, ComissaoEleitoralService>();
            services.AddScoped<IMembroChapaService, MembroChapaService>();
            services.AddScoped<IMembroComissaoService, MembroComissaoService>();
            services.AddScoped<IValidacaoElegibilidadeService, ValidacaoElegibilidadeService>();
            services.AddScoped<IDenunciaService, DenunciaService>();
            services.AddScoped<IImpugnacaoService, ImpugnacaoService>();
            services.AddScoped<IImpugnacaoResultadoService, ImpugnacaoResultadoService>();
            services.AddScoped<IJulgamentoService, JulgamentoService>();
            services.AddScoped<IRecursoService, RecursoService>();
            services.AddScoped<ISubstituicaoService, SubstituicaoService>();
            services.AddScoped<IVotacaoService, VotacaoService>();
            services.AddScoped<IResultadoService, ResultadoService>();
            services.AddScoped<IEleicaoService, EleicaoService>();
            services.AddScoped<IParametroConselheiroService, ParametroConselheiroService>();
            services.AddScoped<IDocumentoEleitoralService, DocumentoEleitoralService>();
            services.AddScoped<IRelatorioService, RelatorioService>();
            services.AddScoped<IExportacaoService, ExportacaoService>();
            services.AddScoped<IProfissionalService, ProfissionalService>();
            
            // Services de Infraestrutura (já existentes)
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtService, JwtService>();
            
            // Services de Arquivo e Storage
            services.AddScoped<IArquivoService, ArquivoService>();
            services.AddScoped<IStorageService, StorageService>();
            
            // Services de Integração Externa
            services.AddScoped<ICorporativoService, CorporativoService>();
            services.AddScoped<IIntegracaoCAUService, IntegracaoCAUService>();
            services.AddScoped<IIntegracaoRNAService, IntegracaoRNAService>();
            
            // Services de Background Jobs
            services.AddHostedService<EmailJobService>();
            services.AddHostedService<NotificacaoJobService>();
            services.AddHostedService<CalendarioJobService>();
            
            return services;
        }
        
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            // Repositórios específicos
            services.AddScoped<IImpugnacaoResultadoRepository, ImpugnacaoResultadoRepository>();
            
            // Adicionar outros repositórios conforme necessário
            
            return services;
        }
        
        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            // Adicionar validadores FluentValidation se necessário
            
            return services;
        }
    }
}