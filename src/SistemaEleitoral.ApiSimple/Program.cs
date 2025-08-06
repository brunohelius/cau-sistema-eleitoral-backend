var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Sistema Eleitoral CAU API", 
        Version = "v1",
        Description = "API para o Sistema Eleitoral do CAU - Versão 60% Migrada"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sistema Eleitoral CAU API v1");
    c.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

// Map health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Log startup
app.Logger.LogInformation($"Sistema Eleitoral CAU API started at {DateTime.Now}");
app.Logger.LogInformation("Swagger UI available at: /");

app.Run();