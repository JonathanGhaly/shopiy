using Microsoft.EntityFrameworkCore;
using Shopiy.Domain.Interfaces;
using System.Threading.Tasks; // Needed for async SaveChangesAsync

/// <summary>
/// Read‑only DbContext that throws on any attempt to persist changes.
/// Implements IApplicationReadOnlyDbContext for DI registration.
/// </summary>

namespace Shopiy.Infrastructure.Persistence;

public sealed class ReadOnlyApplicationDbContext : ApplicationDbContext, IApplicationReadOnlyDbContext
{
    public ReadOnlyApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public override int SaveChanges()
    {
        throw new System.NotSupportedException("ReadOnlyApplicationDbContext does not support SaveChanges.");
    }

    public override async System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        throw new System.NotSupportedException("ReadOnlyApplicationDbContext does not support SaveChangesAsync.");
    }
}
