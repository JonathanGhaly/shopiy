using Shopiy.Infrastructure;
using Shopiy.Infrastructure.DependencyInjection;
using Shopiy.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------
// Services
// ------------------------------------

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi();

builder.Services.AddPersistence(builder.Configuration);

builder.Services.AddIdentityServices(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

// Uncomment when implemented
// builder.Services.AddApplication();

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

var app = builder.Build();

// ------------------------------------
// Middleware
// ------------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await IdentitySeeder.SeedAsync(app.Services);

app.UseHttpsRedirection();

app.UseCors("Default");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();