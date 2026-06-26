using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shopiy.Domain.Entities;

namespace Shopiy.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        // --------------------------
        // Primary Key
        // --------------------------

        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Id)
            .HasColumnName("id");

        // --------------------------
        // Properties
        // --------------------------

        builder.Property(oi => oi.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(oi => oi.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(oi => oi.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(oi => oi.UnitPrice)
            .HasColumnName("unit_price")
            .IsRequired();

        builder.Property(oi => oi.Total)
            .HasColumnName("total")
            .IsRequired();

        builder.Property(oi => oi.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(oi => oi.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(oi => oi.DeletedAt)
            .HasColumnName("deleted_at");

        // --------------------------
        // Check Constraints
        // --------------------------

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_OrderItems_Quantity",
                "\"quantity\" > 0");

            t.HasCheckConstraint(
                "CK_OrderItems_UnitPrice",
                "\"unit_price\" >= 0");

            t.HasCheckConstraint(
                "CK_OrderItems_Total",
                "\"total\" >= 0");
        });

        // --------------------------
        // Relationships
        // --------------------------

        builder.HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(oi => oi.Product)
            .WithMany(p => p.OrderItems)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // --------------------------
        // Global Query Filter
        // --------------------------

        builder.HasQueryFilter(oi => oi.DeletedAt == null);

        // --------------------------
        // Indexes
        // --------------------------

        builder.HasIndex(oi => oi.OrderId)
            .HasDatabaseName("idx_order_items_order_id");

        builder.HasIndex(oi => oi.ProductId)
            .HasDatabaseName("idx_order_items_product_id");

        builder.HasIndex(oi => new
        {
            oi.OrderId,
            oi.ProductId
        })
        .HasDatabaseName("idx_order_items_order_product");

        // Prevent duplicate products within the same order
        builder.HasIndex(oi => new
        {
            oi.OrderId,
            oi.ProductId
        })
        .IsUnique()
        .HasDatabaseName("uq_order_items_order_product");
    }
}
