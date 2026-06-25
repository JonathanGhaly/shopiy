using Shopiy.Domain.Common;

namespace Shopiy.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public bool Revoked { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public string? CreatedByIp { get; set; }

    public string? RevokedByIp { get; set; }

    public bool IsExpired =>
        DateTime.UtcNow >= ExpiresAt;

    public bool IsActive =>
        !Revoked && !IsExpired;
}