namespace Shopiy.Domain.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}