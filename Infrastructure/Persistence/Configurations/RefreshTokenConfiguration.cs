using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shopiy.Domain.Entities;

namespace Shopiy.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        // --------------------------
        // Primary Key
        // --------------------------

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
            .HasColumnName("id");

        // --------------------------
        // Properties
        // --------------------------

        builder.Property(rt => rt.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(rt => rt.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(rt => rt.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(rt => rt.Revoked)
            .HasColumnName("revoked")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(rt => rt.RevokedAt)
            .HasColumnName("revoked_at");

        builder.Property(rt => rt.ReplacedByTokenId)
            .HasColumnName("replaced_by_token_id");

        builder.Property(rt => rt.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasMaxLength(45);

        builder.Property(rt => rt.RevokedByIp)
            .HasColumnName("revoked_by_ip")
            .HasMaxLength(45);

        builder.Property(rt => rt.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(rt => rt.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(rt => rt.DeletedAt)
            .HasColumnName("deleted_at");

        // --------------------------
        // Check Constraints
        // --------------------------

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_RefreshTokens_ExpiresAt",
                "\"expires_at\" > \"created_at\"");
        });

        // --------------------------
        // Relationships
        // --------------------------

        // Assumes ApplicationUser does not expose a
        // RefreshTokens navigation property.
        builder.HasOne<Infrastructure.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(rt => rt.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.SetNull);

        // --------------------------
        // Global Query Filter
        // --------------------------

        builder.HasQueryFilter(rt => rt.DeletedAt == null);

        // --------------------------
        // Indexes
        // --------------------------

        builder.HasIndex(rt => rt.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_refresh_tokens_token_hash");

        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("idx_refresh_tokens_user_id");

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("idx_refresh_tokens_expires_at");

        builder.HasIndex(rt => rt.Revoked)
            .HasDatabaseName("idx_refresh_tokens_revoked");

        builder.HasIndex(rt => new
        {
            rt.UserId,
            rt.Revoked
        })
        .HasDatabaseName("idx_refresh_tokens_user_revoked");

        builder.HasIndex(rt => rt.ReplacedByTokenId)
            .HasDatabaseName("idx_refresh_tokens_replaced_by");
    }
}
