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

    public async Task<Ingredient?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Ingredients
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<OperationResult> CreateAsync(Ingredient input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

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
            QuantityInStock = decimal.Round(input.QuantityInStock, 3, MidpointRounding.AwayFromZero),
            LowStockThreshold = decimal.Round(input.LowStockThreshold, 3, MidpointRounding.AwayFromZero),
            ExpiryDate = input.ExpiryDate
        };

        db.Ingredients.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new OperationResult(true);
    }

    public async Task<OperationResult> UpdateAsync(Ingredient input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

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
        existing.QuantityInStock = decimal.Round(input.QuantityInStock, 3, MidpointRounding.AwayFromZero);
        existing.LowStockThreshold = decimal.Round(input.LowStockThreshold, 3, MidpointRounding.AwayFromZero);
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

        var usedInRecipes = await db.RecipeIngredients
            .AnyAsync(r => r.IngredientId == id, cancellationToken);
        if (usedInRecipes)
        {
            return new OperationResult(
                false,
                "Cannot delete this ingredient because it is used in one or more product recipes.");
        }

        var hasTransactions = await db.InventoryTransactions
            .AnyAsync(t => t.IngredientId == id, cancellationToken);
        if (hasTransactions)
        {
            return new OperationResult(
                false,
                "Cannot delete this ingredient because it is linked to existing inventory transactions.");
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

        if (name.Length > 100)
        {
            return "Ingredient name cannot exceed 100 characters.";
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            return "Unit of measurement is required.";
        }

        if (unit.Length > 30)
        {
            return "Unit cannot exceed 30 characters.";
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
