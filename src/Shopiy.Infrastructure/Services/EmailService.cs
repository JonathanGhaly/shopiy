using Microsoft.Extensions.Logging;
using Shopiy.Domain.Interfaces;

namespace Shopiy.Infrastructure.Services;

/// <summary>
/// Stub email service — logs emails for development. Replace with real SMTP/SendGrid in production.
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        _logger.LogInformation(
            "[EmailService] Sending email to {To} | Subject: {Subject}",
            to, subject);

        return Task.CompletedTask;
    }
}
