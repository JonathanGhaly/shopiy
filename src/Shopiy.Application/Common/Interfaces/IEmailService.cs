namespace Shopiy.Domain.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody);
}