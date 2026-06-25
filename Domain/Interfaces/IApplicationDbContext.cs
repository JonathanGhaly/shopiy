using Microsoft.EntityFrameworkCore;
using Shopiy.Domain.Entities;

namespace Shopiy.Domain.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }

    DbSet<Category> Categories { get; }

    DbSet<Order> Orders { get; }

    DbSet<OrderItem> OrderItems { get; }

    DbSet<ProductCategory> ProductCategories { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}