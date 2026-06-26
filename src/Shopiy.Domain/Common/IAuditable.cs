namespace Shopiy.Domain.Common;

/// <summary>
/// Marks a domain entity as auditable, requiring it to expose standard lifecycle timestamps.
/// Implementations typically inherit <see cref="BaseEntity"/> which fulfils this contract.
/// </summary>
public interface IAuditable
{
    /// <summary>UTC timestamp when the record was first created.</summary>
    DateTime CreatedAt { get; }

    /// <summary>UTC timestamp of the last modification. <c>null</c> if never updated.</summary>
    DateTime? UpdatedAt { get; }

    /// <summary>UTC timestamp of soft-deletion. <c>null</c> while the record is active.</summary>
    DateTime? DeletedAt { get; }
}