using BakeTrack.Data;
using BakeTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Services;

public sealed record OperationResult(bool Success, string? ErrorMessage = null);

/// <summary>
/// Manages bakery ingredient CRUD operations and server-side validation.
/// </summary>
public class IngredientService
{
    private readonly IDbContextFactory<BakeryDbContext> _dbFactory;

    public IngredientService(IDbContextFactory<BakeryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Ingredient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Ingredients
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult> CreateAsync(Ingredient input, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateAndNormalize(input, out var name, out var unit);
        if (validationError is not null)
        {
            return new OperationResult(false, validationError);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = new Ingredient
        {
            Name = name,
            Unit = unit,
            QuantityInStock = input.QuantityInStock,
            LowStockThreshold = input.LowStockThreshold,
            ExpiryDate = input.ExpiryDate
        };

        db.Ingredients.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new OperationResult(true);
    }

    public async Task<OperationResult> UpdateAsync(Ingredient input, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateAndNormalize(input, out var name, out var unit);
        if (validationError is not null)
        {
            return new OperationResult(false, validationError);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.Ingredients.FindAsync([input.Id], cancellationToken);
        if (existing is null)
        {
            return new OperationResult(false, "The selected ingredient no longer exists.");
        }

        existing.Name = name;
        existing.Unit = unit;
        existing.QuantityInStock = input.QuantityInStock;
        existing.LowStockThreshold = input.LowStockThreshold;
        existing.ExpiryDate = input.ExpiryDate;

        await db.SaveChangesAsync(cancellationToken);
        return new OperationResult(true);
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.Ingredients.FindAsync([id], cancellationToken);
        if (existing is null)
        {
            return new OperationResult(false, "The ingredient was already deleted.");
        }

        try
        {
            db.Ingredients.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
            return new OperationResult(true);
        }
        catch (DbUpdateException)
        {
            return new OperationResult(
                false,
                "Cannot delete this ingredient because it is linked to existing recipes or inventory transactions.");
        }
    }

    private static string? ValidateAndNormalize(Ingredient input, out string name, out string unit)
    {
        name = input.Name?.Trim() ?? string.Empty;
        unit = input.Unit?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Ingredient name is required and cannot contain only spaces.";
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            return "Unit of measurement is required.";
        }

        if (input.QuantityInStock < 0)
        {
            return "Quantity in stock must be 0 or greater.";
        }

        if (input.LowStockThreshold < 0)
        {
            return "Minimum stock level must be 0 or greater.";
        }

        return null;
    }
}
