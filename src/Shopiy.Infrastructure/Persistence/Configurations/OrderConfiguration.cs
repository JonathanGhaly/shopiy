using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shopiy.Domain.Entities;
using Shopiy.Domain.Enums;

namespace Shopiy.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        // --------------------------
        // Primary Key
        // --------------------------

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id");

        // --------------------------
        // Properties
        // --------------------------

        builder.Property(o => o.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(OrderStatus.Pending)
            .IsRequired();

        builder.Property(o => o.Subtotal)
            .HasColumnName("subtotal")
            .IsRequired();

        builder.Property(o => o.Tax)
            .HasColumnName("tax")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(o => o.Shipping)
            .HasColumnName("shipping")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(o => o.Total)
            .HasColumnName("total")
            .IsRequired();

        builder.Property(o => o.Currency)
            .HasColumnName("currency")
            .HasMaxLength(3)
            .HasDefaultValue("EGP")
            .IsRequired();

        builder.Property(o => o.ShippingAddress)
            .HasColumnName("shipping_address")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(o => o.BillingAddress)
            .HasColumnName("billing_address")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(o => o.Notes)
            .HasColumnName("notes")
            .HasColumnType("text");

        builder.Property(o => o.PlacedAt)
            .HasColumnName("placed_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(o => o.ShippedAt)
            .HasColumnName("shipped_at");

        builder.Property(o => o.DeliveredAt)
            .HasColumnName("delivered_at");

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(o => o.DeletedAt)
            .HasColumnName("deleted_at");

        // --------------------------
        // Check Constraints
        // --------------------------

        builder.ToTable(t =>
        {
            t.HasCheckConstraint(
                "CK_Orders_Subtotal",
                "\"subtotal\" >= 0");

            t.HasCheckConstraint(
                "CK_Orders_Tax",
                "\"tax\" >= 0");

            t.HasCheckConstraint(
                "CK_Orders_Shipping",
                "\"shipping\" >= 0");

            t.HasCheckConstraint(
                "CK_Orders_Total",
                "\"total\" >= 0");
        });

        // --------------------------
        // Relationships
        // --------------------------

        builder.HasMany(o => o.Items)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // If you later add:
        // public ApplicationUser User { get; set; }
        // you can configure the FK here.

        // --------------------------
        // Global Query Filter
        // --------------------------

        builder.HasQueryFilter(o => o.DeletedAt == null);

        // --------------------------
        // Indexes
        // --------------------------

        builder.HasIndex(o => o.UserId)
            .HasDatabaseName("idx_orders_user_id");

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("idx_orders_status");

        builder.HasIndex(o => o.PlacedAt)
            .HasDatabaseName("idx_orders_placed_at");

        // PostgreSQL partial index:
        // WHERE status = 'Pending'
        builder.HasIndex(o => o.Status)
            .HasDatabaseName("idx_orders_status_pending")
            .HasFilter("\"status\" = 'Pending'");
    }
}
