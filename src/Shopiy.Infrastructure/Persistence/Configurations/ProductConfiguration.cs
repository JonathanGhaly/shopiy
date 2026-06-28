using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shopiy.Domain.Entities;

namespace Shopiy.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(300)
            .IsRequired();

        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Price)
            .HasColumnName("price")
            .IsRequired();

        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_Products_Price",
                "\"price\" >= 0"));

        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("EGP")
            .IsRequired();

        builder.Property(x => x.SKU)
            .HasColumnName("sku")
            .HasMaxLength(100);

        builder.HasIndex(x => x.SKU)
            .IsUnique();

        builder.Property(x => x.StockQuantity)
            .HasColumnName("stock_quantity")
            .HasDefaultValue(0)
            .IsRequired();

        builder.ToTable(t =>
            t.HasCheckConstraint(
                "CK_Products_StockQuantity",
                "\"stock_quantity\" >= 0"));

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(x => x.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at");

        // --------------------------
        // Relationships
        // --------------------------

        builder.HasMany(x => x.ProductCategories)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.OrderItems)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // --------------------------
        // Global Query Filter
        // --------------------------

        builder.HasQueryFilter(x =>
            x.DeletedAt == null);

        // --------------------------
        // Indexes
        // --------------------------

        builder.HasIndex(x => x.Price)
            .HasDatabaseName("idx_products_price");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("idx_products_created_at");

        builder.HasIndex(x => new
        {
            x.IsActive,
            x.DeletedAt
        })
        .HasDatabaseName("idx_products_active");

        builder.HasIndex(x => x.Metadata)
            .HasMethod("GIN")
            .HasDatabaseName("idx_products_metadata_gin");
    }
}