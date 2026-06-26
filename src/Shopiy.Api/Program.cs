using Shopiy.Application.DependencyInjection;
using Shopiy.Infrastructure;
using Shopiy.Infrastructure.DependencyInjection;
using Shopiy.Infrastructure.Identity;
using Shopiy.Api.Extensions;
using Shopiy.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// ------------------------------------
// Middleware
// ------------------------------------

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();
}

await IdentitySeeder.SeedAsync(app.Services);

app.UseHttpsRedirection();

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();