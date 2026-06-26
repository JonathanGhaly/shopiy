using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shopiy.Domain.Entities;

namespace Shopiy.Infrastructure.Persistence.Configurations;

public sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("product_categories");

        // --------------------------
        // Composite Primary Key
        // --------------------------

        builder.HasKey(pc => new
        {
            pc.ProductId,
            pc.CategoryId
        });

        builder.Property(pc => pc.ProductId)
            .HasColumnName("product_id");

        builder.Property(pc => pc.CategoryId)
            .HasColumnName("category_id");

        // --------------------------
        // Relationships
        // --------------------------

        builder.HasOne(pc => pc.Product)
            .WithMany(p => p.ProductCategories)
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Category)
            .WithMany(c => c.ProductCategories)
            .HasForeignKey(pc => pc.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // --------------------------
        // Indexes
        // --------------------------

        // Optimizes:
        // SELECT * FROM product_categories
        // WHERE category_id = ?

        builder.HasIndex(pc => pc.CategoryId)
            .HasDatabaseName("idx_product_categories_category_id");
    }
}
