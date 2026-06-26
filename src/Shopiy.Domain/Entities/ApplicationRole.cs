using Microsoft.AspNetCore.Identity;

namespace Shopiy.Domain.Entities; 
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
}
