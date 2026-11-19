
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
        modelBuilder.Entity<RecipeIngredient>()
            .HasIndex(r => new { r.ProductId, r.IngredientId })
            .IsUnique();

        modelBuilder.Entity<RecipeIngredient>()
            .HasOne(r => r.Ingredient)
            .WithMany(i => i.RecipeIngredients)
            .HasForeignKey(r => r.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductionRecord>()
            .HasOne(r => r.Product)
            .WithMany(p => p.ProductionRecords)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryTransaction>()
            .HasOne(t => t.Ingredient)
            .WithMany(i => i.InventoryTransactions)
            .HasForeignKey(t => t.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
