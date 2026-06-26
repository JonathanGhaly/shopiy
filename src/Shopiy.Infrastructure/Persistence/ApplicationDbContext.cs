using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Interfaces;
using Shopiy.Infrastructure.Identity;

namespace Shopiy.Infrastructure.Persistence;

public class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>,
      IApplicationDbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    #region DbSets

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    #endregion

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentity(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();

        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not Domain.Common.BaseEntity entity)
                continue;

            switch (entry.State)
            {
                case EntityState.Added:

                    entity.GetType()
                        .GetProperty(nameof(entity.CreatedAt))
                        ?.SetValue(entity, utcNow);

                    break;

                case EntityState.Modified:

                    entity.GetType()
                        .GetProperty(nameof(entity.UpdatedAt))
                        ?.SetValue(entity, utcNow);

                    break;
            }
        }
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("users");

            entity.Property(x => x.FullName)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.EmailVerifiedAt);

            entity.Property(x => x.DeletedAt);

            entity.Property(x => x.CreatedAt);

            entity.Property(x => x.UpdatedAt);

            entity.HasIndex(x => x.Email)
                .IsUnique();

            entity.HasQueryFilter(x => x.DeletedAt == null);
        });

        builder.Entity<ApplicationRole>()
            .ToTable("roles");

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>()
            .ToTable("user_roles");

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>()
            .ToTable("user_claims");

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>()
            .ToTable("user_logins");

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>()
            .ToTable("user_tokens");

        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>()
            .ToTable("role_claims");
    }
}