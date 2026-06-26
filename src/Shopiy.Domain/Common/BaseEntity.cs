
namespace Shopiy.Domain.Common;

/// <summary>
/// Abstract base class for all domain entities.
/// Provides a strongly-typed <see cref="Guid"/> primary key and common audit/lifecycle timestamps.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Unique identifier for the entity, auto-generated on construction.</summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>UTC timestamp when the entity was first persisted.</summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the most recent update. <c>null</c> if the entity has never been modified.</summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>UTC timestamp when the entity was soft-deleted. <c>null</c> while the entity is active.</summary>
    public DateTime? DeletedAt { get; protected set; }

    /// <summary>
    /// Stamps <see cref="UpdatedAt"/> with the current UTC time.
    /// Call this in command handlers after mutating entity properties.
    /// </summary>
    public void MarkUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the entity as deleted by stamping <see cref="DeletedAt"/> with the current UTC time.
    /// The record is NOT removed from the database; EF query filters hide soft-deleted rows from normal queries.
    /// </summary>
    public void SoftDelete()
    {
        DeletedAt = DateTime.UtcNow;
    }
}