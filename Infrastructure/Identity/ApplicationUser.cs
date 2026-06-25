using Microsoft.AspNetCore.Identity;

namespace Shopiy.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}