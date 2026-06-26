namespace Shopiy.Domain.Common;

public interface IAuditable
{
    DateTime CreatedAt { get; }

    DateTime? UpdatedAt { get; }

    DateTime? DeletedAt { get; }
}