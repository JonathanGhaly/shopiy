using Microsoft.AspNetCore.Identity;

namespace Shopiy.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}
