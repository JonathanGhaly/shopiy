using Shopiy.Domain.Entities;
namespace Shopiy.Domain.Interfaces;
public interface IJwtService
{
    Task<string> GenerateTokenAsync(Guid userId, string email, IEnumerable<string> roles);
}