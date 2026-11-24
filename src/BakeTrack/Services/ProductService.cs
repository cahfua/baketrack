using System.ComponentModel.DataAnnotations;
using BakeTrack.Data;
using BakeTrack.Models;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Services;

public sealed record OperationResult<T>(bool Success, T? Value = default, string? ErrorMessage = null);

public sealed record ProductSummary(
    int Id,
    string Name,
    string Category,
    int BatchYield,
    decimal Price,
    string Description,
    int IngredientCount);

public sealed record RecipeIngredientDto(
    int Id,
    int IngredientId,
    string IngredientName,
    string Unit,
    decimal QuantityRequired);

public sealed record ProductDetail(
    int Id,
    string Name,
    string Category,
    int BatchYield,
    decimal Price,
    string Description,
    IReadOnlyList<RecipeIngredientDto> RecipeIngredients);

public sealed class RecipeIngredientInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Please select an ingredient.")]
    public int IngredientId { get; set; }

    [Range(typeof(decimal), "0.001", "999999999999.999", ErrorMessage = "Quantity required must be greater than 0.")]
    public decimal QuantityRequired { get; set; }
}

public sealed class ProductInput
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(60, ErrorMessage = "Category cannot exceed 60 characters.")]
    public string Category { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Batch yield must be at least 1.")]
    public int BatchYield { get; set; } = 1;

    [Range(typeof(decimal), "0", "999999999999.99", ErrorMessage = "Price must be 0 or greater.")]
    public decimal Price { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    public List<RecipeIngredientInput> RecipeIngredients { get; set; } = new();
}

/// <summary>
/// Manages bakery products, batch recipe definitions, and server-side validation rules.
/// </summary>
public class ProductService
{
    private const int MaxNameLength = 100;
    private const int MaxCategoryLength = 60;
    private const int MaxDescriptionLength = 500;
    private const decimal MaxPrice = 999999999999.99m;
    private const decimal MaxRecipeQuantity = 999999999999.999m;

    private readonly IDbContextFactory<BakeryDbContext> _dbFactory;

    public ProductService(IDbContextFactory<BakeryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public static IReadOnlyList<string> SuggestedCategories { get; } =
    [
        "Bread",
        "Pastry",
        "Cake",
        "Cookies",
        "Muffins & Scones",
        "Pies & Tarts",
        "Other"
    ];

    public async Task<List<ProductSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.Products
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new ProductSummary(
                p.Id,
                p.Name,
                p.Category,
                p.BatchYield,
                p.Price,
                p.Description,
                p.RecipeIngredients.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductDetail?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDetail(
                p.Id,
                p.Name,
                p.Category,
                p.BatchYield,
                p.Price,
                p.Description,
                p.RecipeIngredients
                    .OrderBy(r => r.Ingredient.Name)
                    .Select(r => new RecipeIngredientDto(
                        r.Id,
                        r.IngredientId,
                        r.Ingredient.Name,
                        r.Ingredient.Unit,
                        r.QuantityRequired))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return product;
    }

    public async Task<OperationResult<int>> CreateAsync(ProductInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var validationError = ValidateAndNormalizeProduct(input, out var name, out var category, out var description);
        if (validationError is not null)
        {
            return new OperationResult<int>(false, ErrorMessage: validationError);
        }

        var recipeItems = input.RecipeIngredients ?? [];
        var recipeError = ValidateRecipeItems(recipeItems);
        if (recipeError is not null)
        {
            return new OperationResult<int>(false, ErrorMessage: recipeError);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var normalizedNameLower = name.ToLower();
        var duplicateExists = await db.Products
            .AnyAsync(p => p.Name.ToLower() == normalizedNameLower, cancellationToken);
        if (duplicateExists)
        {
            return new OperationResult<int>(false, ErrorMessage: $"A product named '{name}' already exists.");
        }

        var missingIngredientError = await EnsureIngredientsExistAsync(db, recipeItems, cancellationToken);
        if (missingIngredientError is not null)
        {
            return new OperationResult<int>(false, ErrorMessage: missingIngredientError);
        }

        var entity = new Product
        {
            Name = name,
            Category = category,
            BatchYield = input.BatchYield,
            Price = decimal.Round(input.Price, 2, MidpointRounding.AwayFromZero),
            Description = description,
            RecipeIngredients = recipeItems.Select(item => new RecipeIngredient
            {
                IngredientId = item.IngredientId,
                QuantityRequired = decimal.Round(item.QuantityRequired, 3, MidpointRounding.AwayFromZero)
            }).ToList()
        };

        db.Products.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return new OperationResult<int>(true, entity.Id);
    }

    public async Task<OperationResult> UpdateAsync(ProductInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var validationError = ValidateAndNormalizeProduct(input, out var name, out var category, out var description);
        if (validationError is not null)
        {
            return new OperationResult(false, validationError);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.Products.FindAsync([input.Id], cancellationToken);
        if (existing is null)
        {
            return new OperationResult(false, "The selected product no longer exists.");
        }

        var normalizedNameLower = name.ToLower();
        var duplicateExists = await db.Products
            .AnyAsync(p => p.Id != input.Id && p.Name.ToLower() == normalizedNameLower, cancellationToken);
        if (duplicateExists)
        {
            return new OperationResult(false, $"A product named '{name}' already exists.");
        }

        existing.Name = name;
        existing.Category = category;
        existing.BatchYield = input.BatchYield;
        existing.Price = decimal.Round(input.Price, 2, MidpointRounding.AwayFromZero);
        existing.Description = description;

        await db.SaveChangesAsync(cancellationToken);
        return new OperationResult(true);
    }

    public async Task<OperationResult> SaveRecipeAsync(
        int productId,
        IReadOnlyCollection<RecipeIngredientInput> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        var recipeError = ValidateRecipeItems(items);
        if (recipeError is not null)
        {
            return new OperationResult(false, recipeError);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var product = await db.Products
            .Include(p => p.RecipeIngredients)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return new OperationResult(false, "The selected product no longer exists.");
        }

        var missingIngredientError = await EnsureIngredientsExistAsync(db, items, cancellationToken);
        if (missingIngredientError is not null)
        {
            return new OperationResult(false, missingIngredientError);
        }

        var desiredByIngredient = items.ToDictionary(
            i => i.IngredientId,
            i => decimal.Round(i.QuantityRequired, 3, MidpointRounding.AwayFromZero));

        var toRemove = product.RecipeIngredients
            .Where(existing => !desiredByIngredient.ContainsKey(existing.IngredientId))
            .ToList();
        if (toRemove.Count > 0)
        {
            db.RecipeIngredients.RemoveRange(toRemove);
        }

        foreach (var existing in product.RecipeIngredients.Where(r => desiredByIngredient.ContainsKey(r.IngredientId)))
        {
            existing.QuantityRequired = desiredByIngredient[existing.IngredientId];
        }

        var existingIngredientIds = product.RecipeIngredients
            .Select(r => r.IngredientId)
            .ToHashSet();

        foreach (var (ingredientId, quantityRequired) in desiredByIngredient)
        {
            if (!existingIngredientIds.Contains(ingredientId))
            {
                product.RecipeIngredients.Add(new RecipeIngredient
                {
                    ProductId = product.Id,
                    IngredientId = ingredientId,
                    QuantityRequired = quantityRequired
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new OperationResult(true);
    }

    public async Task<OperationResult> AddRecipeIngredientAsync(
        int productId,
        RecipeIngredientInput item,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var itemError = ValidateSingleRecipeItem(item);
        if (itemError is not null)
        {
            return new OperationResult(false, itemError);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var productExists = await db.Products.AnyAsync(p => p.Id == productId, cancellationToken);
        if (!productExists)
        {
            return new OperationResult(false, "The selected product no longer exists.");
        }

        var ingredientExists = await db.Ingredients.AnyAsync(i => i.Id == item.IngredientId, cancellationToken);
        if (!ingredientExists)
        {
            return new OperationResult(false, "The selected ingredient no longer exists.");
        }

        var alreadyInRecipe = await db.RecipeIngredients
            .AnyAsync(r => r.ProductId == productId && r.IngredientId == item.IngredientId, cancellationToken);
        if (alreadyInRecipe)
        {
            return new OperationResult(false, "This ingredient is already part of the product recipe.");
        }

        db.RecipeIngredients.Add(new RecipeIngredient
        {
            ProductId = productId,
            IngredientId = item.IngredientId,
            QuantityRequired = decimal.Round(item.QuantityRequired, 3, MidpointRounding.AwayFromZero)
        });

        await db.SaveChangesAsync(cancellationToken);
        return new OperationResult(true);
    }

    public async Task<OperationResult> UpdateRecipeIngredientQuantityAsync(
        int productId,
        int ingredientId,
        decimal quantityRequired,
        CancellationToken cancellationToken = default)
    {
        var itemError = ValidateRecipeQuantity(quantityRequired);
        if (itemError is not null)
        {
            return new OperationResult(false, itemError);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.RecipeIngredients
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.IngredientId == ingredientId, cancellationToken);
        if (existing is null)
        {
            return new OperationResult(false, "The selected recipe ingredient no longer exists.");
        }

        existing.QuantityRequired = decimal.Round(quantityRequired, 3, MidpointRounding.AwayFromZero);
        await db.SaveChangesAsync(cancellationToken);

        return new OperationResult(true);
    }

    public async Task<OperationResult> RemoveRecipeIngredientAsync(
        int productId,
        int ingredientId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.RecipeIngredients
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.IngredientId == ingredientId, cancellationToken);
        if (existing is null)
        {
            return new OperationResult(false, "The selected recipe ingredient was already removed.");
        }

        db.RecipeIngredients.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);

        return new OperationResult(true);
    }

    public async Task<OperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.Products.FindAsync([id], cancellationToken);
        if (existing is null)
        {
            return new OperationResult(false, "The product was already deleted.");
        }

        var hasProductionHistory = await db.ProductionRecords
            .AnyAsync(r => r.ProductId == id, cancellationToken);
        if (hasProductionHistory)
        {
            return new OperationResult(
                false,
                "Cannot delete this product because it has existing production records.");
        }

        try
        {
            db.Products.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
            return new OperationResult(true);
        }
        catch (DbUpdateException)
        {
            return new OperationResult(
                false,
                "Cannot delete this product because it is linked to existing production records.");
        }
    }

    private static string? ValidateAndNormalizeProduct(
        ProductInput input,
        out string name,
        out string category,
        out string description)
    {
        name = input.Name?.Trim() ?? string.Empty;
        category = input.Category?.Trim() ?? string.Empty;
        description = input.Description?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Product name is required and cannot contain only spaces.";
        }

        if (name.Length > MaxNameLength)
        {
            return $"Product name cannot exceed {MaxNameLength} characters.";
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return "Category is required.";
        }

        if (category.Length > MaxCategoryLength)
        {
            return $"Category cannot exceed {MaxCategoryLength} characters.";
        }

        if (input.BatchYield < 1 || input.BatchYield > 100000)
        {
            return "Batch yield must be between 1 and 100,000.";
        }

        if (input.Price < 0 || input.Price > MaxPrice)
        {
            return "Price must be 0 or greater.";
        }

        if (description.Length > MaxDescriptionLength)
        {
            return $"Description cannot exceed {MaxDescriptionLength} characters.";
        }

        return null;
    }

    private static string? ValidateRecipeItems(IReadOnlyCollection<RecipeIngredientInput> items)
    {
        var seenIngredientIds = new HashSet<int>();

        foreach (var item in items)
        {
            var itemError = ValidateSingleRecipeItem(item);
            if (itemError is not null)
            {
                return itemError;
            }

            if (!seenIngredientIds.Add(item.IngredientId))
            {
                return "Each ingredient can only appear once in a product recipe.";
            }
        }

        return null;
    }

    private static string? ValidateSingleRecipeItem(RecipeIngredientInput item)
    {
        if (item.IngredientId <= 0)
        {
            return "Please select a valid ingredient.";
        }

        return ValidateRecipeQuantity(item.QuantityRequired);
    }

    private static string? ValidateRecipeQuantity(decimal quantityRequired)
    {
        if (quantityRequired <= 0)
        {
            return "Recipe ingredient quantity must be greater than 0.";
        }

        if (quantityRequired > MaxRecipeQuantity)
        {
            return $"Recipe ingredient quantity cannot exceed {MaxRecipeQuantity:0.###}.";
        }

        return null;
    }

    private static async Task<string?> EnsureIngredientsExistAsync(
        BakeryDbContext db,
        IReadOnlyCollection<RecipeIngredientInput> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return null;
        }

        var requestedIds = items.Select(i => i.IngredientId).Distinct().ToList();
        var existingCount = await db.Ingredients
            .CountAsync(i => requestedIds.Contains(i.Id), cancellationToken);

        if (existingCount != requestedIds.Count)
        {
            return "One or more selected ingredients no longer exist.";
        }

        return null;
    }
}
