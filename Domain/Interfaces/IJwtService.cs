namespace Shopiy.Domain.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId, string email, IList<string> roles);
}