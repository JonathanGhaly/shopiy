using System.Threading;
using System.Threading.Tasks;

namespace Shopiy.Domain.Interfaces;

/// <summary>
/// Marker interface for read‑only DbContext used with the read‑replica.
/// It inherits all queryable DbSets from <see cref="IApplicationDbContext"/>.
/// SaveChanges methods are not expected to be called.
/// </summary>
public interface IApplicationReadOnlyDbContext : IApplicationDbContext
{
    // No additional members – used for DI registration of a read‑only context.
}
