using Shopiy.Application.DependencyInjection;
using Shopiy.Infrastructure;
using Shopiy.Infrastructure.DependencyInjection;
using Shopiy.Infrastructure.Identity;
using Shopiy.Api.Extensions;
using Shopiy.Api.Middleware;
using Shopiy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ------------------------------------
// Services
// ------------------------------------

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddSwaggerWithJwt();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<RedisHealthCheck>("redis");

var app = builder.Build();

// ------------------------------------
// Middleware
// ------------------------------------

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();

    await IdentitySeeder.SeedAsync(services);
}

app.UseHttpsRedirection();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

try
{
    Log.Information("Starting web host...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public class DatabaseHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly ApplicationDbContext _context;
    public DatabaseHealthCheck(ApplicationDbContext context) => _context = context;

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            return canConnect 
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Database is healthy.")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database connection failed.");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Database check failed.", ex);
        }
    }
}

public class RedisHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly StackExchange.Redis.IConnectionMultiplexer? _redis;
    public RedisHealthCheck(IServiceProvider serviceProvider)
    {
        _redis = serviceProvider.GetService<StackExchange.Redis.IConnectionMultiplexer>();
    }

    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_redis is null)
            {
                return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Redis is not registered.");
            }
            var db = _redis.GetDatabase();
            var ping = await db.PingAsync();
            return ping != TimeSpan.Zero
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Redis is healthy.")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Redis ping failed.");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("Redis check failed.", ex);
        }
    }
}