using Hangfire;
using Hangfire.SqlServer;
using SistemaEleitoral.Domain.Services;
using SistemaEleitoral.Infrastructure.Services;
using SistemaEleitoral.Infrastructure.Hubs;
using SistemaEleitoral.Domain.Jobs.EmailJobs;

namespace SistemaEleitoral.Api.Extensions;

public static class CommunicationExtensions
{
    public static IServiceCollection AddCommunicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Email Services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Background Jobs - Hangfire
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection"), new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = Environment.ProcessorCount * 2;
            options.Queues = new[] { "email", "critical", "default", "low" };
        });

        // SignalR
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        // Email Jobs
        services.AddTransient<EnviarEmailConviteMembroChapaJob>();
        services.AddTransient<EnviarEmailJulgamentoFinalJob>();
        services.AddTransient<GerarExtratoTodosProfissionaisJob>();
        services.AddTransient<EnviarEmailPendenciasMembroChapaJob>();
        services.AddTransient<EnviarEmailChapaConfirmadaJob>();
        services.AddTransient<EnviarEmailDefesaImpugnacaoJob>();
        services.AddTransient<EnviarEmailJulgamentoDenunciaJob>();
        services.AddTransient<EnviarEmailRecursoDenunciaJob>();

        // Email Configuration
        services.Configure<EmailConfiguration>(configuration.GetSection("EmailSettings"));

        return services;
    }

    public static IApplicationBuilder UseCommunicationServices(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Hangfire Dashboard
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() },
            DisplayStorageConnectionString = false,
            DashboardTitle = "Sistema Eleitoral - Jobs"
        });

        // SignalR Hubs
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<NotificationHub>("/api/notificationHub");
        });

        // Seed Email Templates
        using (var scope = app.ApplicationServices.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<EmailTemplateSeeder>();
            seeder.SeedDefaultTemplatesAsync().Wait();
        }

        // Schedule Recurring Jobs
        ScheduleRecurringJobs();

        return app;
    }

    private static void ScheduleRecurringJobs()
    {
        // Job para verificar pendências de chapas (diário às 8h)
        RecurringJob.AddOrUpdate<VerificarPendenciasChapaJob>(
            "verificar-pendencias-chapas",
            job => job.ExecuteAsync(),
            "0 8 * * *", // Cron: todos os dias às 8h
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));

        // Job para enviar lembretes de prazos (diário às 9h)
        RecurringJob.AddOrUpdate<EnviarLembretesPrazosJob>(
            "enviar-lembretes-prazos",
            job => job.ExecuteAsync(),
            "0 9 * * *", // Cron: todos os dias às 9h
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));

        // Job de limpeza de logs antigos (semanal aos domingos às 2h)
        RecurringJob.AddOrUpdate<LimpezaLogsEmailJob>(
            "limpeza-logs-email",
            job => job.ExecuteAsync(),
            "0 2 * * 0", // Cron: domingos às 2h
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));

        // Job de estatísticas de email (diário às 23h)
        RecurringJob.AddOrUpdate<GerarEstatisticasEmailJob>(
            "gerar-estatisticas-email",
            job => job.ExecuteAsync(),
            "0 23 * * *", // Cron: todos os dias às 23h
            TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
    }
}

/// <summary>
/// Filtro de autorização para o dashboard do Hangfire
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Em desenvolvimento, permitir acesso
        if (httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return true;
        }

        // Em produção, verificar se é admin
        return httpContext.User?.IsInRole("Admin") == true;
    }
}

/// <summary>
/// Jobs recorrentes para manutenção do sistema
/// </summary>
public class VerificarPendenciasChapaJob
{
    private readonly IChapaService _chapaService;
    private readonly IEmailService _emailService;
    private readonly ILogger<VerificarPendenciasChapaJob> _logger;

    public VerificarPendenciasChapaJob(
        IChapaService chapaService,
        IEmailService emailService,
        ILogger<VerificarPendenciasChapaJob> logger)
    {
        _chapaService = chapaService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando verificação de pendências de chapas");
            
            var chapasComPendencias = await _chapaService.GetChapasComPendenciasAsync();
            var emailsEnviados = 0;

            foreach (var chapa in chapasComPendencias)
            {
                var parameter = new EmailJobParameter
                {
                    Data = new Dictionary<string, object>
                    {
                        ["chapaId"] = chapa.Id,
                        ["baseUrl"] = "https://sistema.cau.br" // TODO: pegar da configuração
                    }
                };

                BackgroundJob.Enqueue<EnviarEmailPendenciasMembroChapaJob>(
                    job => job.ExecuteAsync(parameter));
                
                emailsEnviados++;
            }

            _logger.LogInformation("Verificação de pendências concluída. {Count} chapas com pendências processadas", emailsEnviados);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar pendências de chapas");
        }
    }
}

public class EnviarLembretesPrazosJob
{
    private readonly ICalendarioService _calendarioService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EnviarLembretesPrazosJob> _logger;

    public EnviarLembretesPrazosJob(
        ICalendarioService calendarioService,
        INotificationService notificationService,
        ILogger<EnviarLembretesPrazosJob> logger)
    {
        _calendarioService = calendarioService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando envio de lembretes de prazos");
            
            // Buscar prazos que vencem nos próximos 3 dias
            var prazosProximos = await _calendarioService.GetPrazosProximosAsync(3);
            var lembretesenviados = 0;

            foreach (var prazo in prazosProximos)
            {
                await _notificationService.NotifyDeadlineAlertAsync(prazo.CalendarioId, prazo.DataLimite);
                lembretesenviados++;
            }

            _logger.LogInformation("Lembretes de prazos enviados. {Count} lembretes processados", lembretesenviados);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar lembretes de prazos");
        }
    }
}

public class LimpezaLogsEmailJob
{
    private readonly IEmailLogRepository _emailLogRepository;
    private readonly ILogger<LimpezaLogsEmailJob> _logger;

    public LimpezaLogsEmailJob(
        IEmailLogRepository emailLogRepository,
        ILogger<LimpezaLogsEmailJob> logger)
    {
        _emailLogRepository = emailLogRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando limpeza de logs de email");
            
            // Remover logs com mais de 90 dias
            var dataLimite = DateTime.Now.AddDays(-90);
            var logsRemovidos = await _emailLogRepository.RemoveOldLogsAsync(dataLimite);

            _logger.LogInformation("Limpeza de logs concluída. {Count} logs removidos", logsRemovidos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na limpeza de logs de email");
        }
    }
}

public class GerarEstatisticasEmailJob
{
    private readonly IEmailLogRepository _emailLogRepository;
    private readonly ILogger<GerarEstatisticasEmailJob> _logger;

    public GerarEstatisticasEmailJob(
        IEmailLogRepository emailLogRepository,
        ILogger<GerarEstatisticasEmailJob> logger)
    {
        _emailLogRepository = emailLogRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        try
        {
            _logger.LogInformation("Gerando estatísticas diárias de email");
            
            var hoje = DateTime.Today;
            var stats = await _emailLogRepository.GetStatsAsync(hoje, hoje.AddDays(1));

            _logger.LogInformation("Estatísticas do dia {Date}: {Total} emails, {Success}% sucesso", 
                hoje.ToShortDateString(), stats.TotalEmails, stats.TaxaSucesso.ToString("F1"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar estatísticas de email");
        }
    }
}