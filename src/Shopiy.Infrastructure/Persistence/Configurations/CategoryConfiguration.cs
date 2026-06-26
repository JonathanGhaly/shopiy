using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shopiy.Domain.Entities;

namespace Shopiy.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        // --------------------------
        // Primary Key
        // --------------------------

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        // --------------------------
        // Properties
        // --------------------------

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasColumnName("slug")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.Property(x => x.ParentId)
            .HasColumnName("parent_id");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

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

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ProductCategories)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // NOTE:
        // ApplicationUser navigation can be added later.
        // Keeping only the FK for now avoids coupling
        // the Domain layer to ASP.NET Identity.

        // --------------------------
        // Query Filter
        // --------------------------

        builder.HasQueryFilter(x => x.DeletedAt == null);

        // --------------------------
        // Indexes
        // --------------------------

        builder.HasIndex(x => x.ParentId)
            .HasDatabaseName("idx_categories_parent_id");

        builder.HasIndex(x => x.SortOrder)
            .HasDatabaseName("idx_categories_sort_order");

        builder.HasIndex(x => x.CreatedBy)
            .HasDatabaseName("idx_categories_created_by");
    }
}