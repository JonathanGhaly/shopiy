
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shopiy.Domain.Interfaces;
using Shopiy.Infrastructure.Authentication;
using System.Text;

namespace Shopiy.Infrastructure.DependencyInjection;

public static class AuthenticationServiceRegistration
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT configuration section is missing.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme =
                JwtBearerDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme =
                JwtBearerDefaults.AuthenticationScheme;

            options.DefaultScheme =
                JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;

            options.SaveToken = false;

            options.TokenValidationParameters =
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,

                    ValidIssuer = jwtOptions.Issuer,

                    ValidAudience = jwtOptions.Audience,

                    IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtOptions.Secret)),

                    ClockSkew = TimeSpan.Zero
                };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var authorization =
                        context.Request.Headers.Authorization
                            .FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(authorization) &&
                        authorization.StartsWith("Bearer "))
                    {
                        context.Token =
                            authorization["Bearer ".Length..].Trim();
                    }

                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy =>
            {
                policy.RequireRole("Admin");
            });

            options.AddPolicy("Customer", policy =>
            {
                policy.RequireRole("Customer");
            });

            options.AddPolicy("AdminOrCustomer", policy =>
            {
                policy.RequireRole("Admin", "Customer");
            });
        });

        // Application Services

        services.AddScoped<IJwtService, JwtService>();

        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}
