using BakeTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Services;

public sealed record DashboardSummary(
    int TotalIngredients,
    int LowStockCount,
    int TotalProducts,
    int TodayProductionCount);

public sealed record DashboardIngredientAlert(
    int Id,
    string Name,
    decimal QuantityInStock,
    string Unit,
    decimal LowStockThreshold,
    DateOnly? ExpiryDate);

public sealed record DashboardAlerts(
    List<DashboardIngredientAlert> LowStock,
    List<DashboardIngredientAlert> ExpiringSoon);

/// <summary>
/// Provides metrics and ingredient alerts for the dashboard.
/// </summary>
public class DashboardService
{
    // Confirm this window with the ingredient expiry feature owner.
    public const int ExpiringSoonDays = 7;

    private readonly IDbContextFactory<BakeryDbContext> _dbFactory;

    public DashboardService(
        IDbContextFactory<BakeryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        var totalIngredients =
            await db.Ingredients.CountAsync(cancellationToken);

        var lowStockCount = await db.Ingredients
            .CountAsync(
                i => i.QuantityInStock <= i.LowStockThreshold,
                cancellationToken);

        var totalProducts =
            await db.Products.CountAsync(cancellationToken);

        var utcToday = DateTime.SpecifyKind(
            DateTime.UtcNow.Date,
            DateTimeKind.Utc);

        var utcTomorrow = utcToday.AddDays(1);

        var todayProduction = await db.ProductionRecords
            .Where(p =>
                p.ProductionDate >= utcToday &&
                p.ProductionDate < utcTomorrow)
            .SumAsync(
                p => (int?)p.QuantityProduced,
                cancellationToken) ?? 0;

        return new DashboardSummary(
            totalIngredients,
            lowStockCount,
            totalProducts,
            todayProduction);
    }

    public async Task<DashboardAlerts> GetAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(ExpiringSoonDays);

        var lowStock = await db.Ingredients
            .AsNoTracking()
            .Where(i => i.QuantityInStock <= i.LowStockThreshold)
            .OrderBy(i => i.QuantityInStock)
            .ThenBy(i => i.Name)
            .Select(i => new DashboardIngredientAlert(
                i.Id,
                i.Name,
                i.QuantityInStock,
                i.Unit,
                i.LowStockThreshold,
                i.ExpiryDate))
            .ToListAsync(cancellationToken);

        var expiringSoon = await db.Ingredients
            .AsNoTracking()
            .Where(i =>
                i.ExpiryDate.HasValue &&
                i.ExpiryDate.Value >= today &&
                i.ExpiryDate.Value <= cutoff)
            .OrderBy(i => i.ExpiryDate)
            .ThenBy(i => i.Name)
            .Select(i => new DashboardIngredientAlert(
                i.Id,
                i.Name,
                i.QuantityInStock,
                i.Unit,
                i.LowStockThreshold,
                i.ExpiryDate))
            .ToListAsync(cancellationToken);

        return new DashboardAlerts(lowStock, expiringSoon);
    }
}