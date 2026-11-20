using Microsoft.EntityFrameworkCore;
using BakeTrack.Models;

namespace BakeTrack.Data;

public class BakeryDbContext : DbContext
{
    public BakeryDbContext(
        DbContextOptions<BakeryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.Property(i => i.QuantityInStock).HasPrecision(18, 3);
            entity.Property(i => i.LowStockThreshold).HasPrecision(18, 3);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });

        modelBuilder.Entity<RecipeIngredient>(entity =>
        {
            entity.Property(r => r.QuantityRequired).HasPrecision(18, 3);
            entity.HasIndex(r => new { r.ProductId, r.IngredientId }).IsUnique();
            entity.HasOne(r => r.Ingredient)
                .WithMany(i => i.RecipeIngredients)
                .HasForeignKey(r => r.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductionRecord>(entity =>
        {
            entity.HasOne(r => r.Product)
                .WithMany(p => p.ProductionRecords)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.Property(t => t.QuantityChange).HasPrecision(18, 3);
            entity.HasOne(t => t.Ingredient)
                .WithMany(i => i.InventoryTransactions)
                .HasForeignKey(t => t.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
