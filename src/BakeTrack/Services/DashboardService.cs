using BakeTrack.Data;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Services;

public sealed record DashboardSummary(
    int TotalIngredients,
    int LowStockCount,
    int TotalProducts,
    int TodayProductionCount);

/// <summary>
/// Provides aggregated metrics for the main BakeTrack dashboard.
/// </summary>
public class DashboardService
{
    private readonly IDbContextFactory<BakeryDbContext> _dbFactory;

    public DashboardService(IDbContextFactory<BakeryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var totalIngredients = await db.Ingredients.CountAsync(cancellationToken);
        var lowStockCount = await db.Ingredients
            .CountAsync(i => i.QuantityInStock <= i.LowStockThreshold, cancellationToken);
        var totalProducts = await db.Products.CountAsync(cancellationToken);

        var utcToday = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var utcTomorrow = utcToday.AddDays(1);

        var todayProduction = await db.ProductionRecords
            .Where(p => p.ProductionDate >= utcToday && p.ProductionDate < utcTomorrow)
            .SumAsync(p => (int?)p.QuantityProduced, cancellationToken) ?? 0;

        return new DashboardSummary(
            totalIngredients,
            lowStockCount,
            totalProducts,
            todayProduction);
    }
}
