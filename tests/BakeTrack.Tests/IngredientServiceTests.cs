using BakeTrack.Models;
using BakeTrack.Services;
using BakeTrack.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Tests;

public sealed class IngredientServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidIngredient_PersistsAndNormalizesAcrossContexts()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var service = new IngredientService(factory);
        var expiry = new DateOnly(2027, 1, 15);

        var result = await service.CreateAsync(new Ingredient
        {
            Name = "  Bread Flour  ",
            Unit = "  kg ",
            QuantityInStock = 12.3456m,
            LowStockThreshold = 2.5m,
            ExpiryDate = expiry
        });

        Assert.True(result.Success);

        var saved = Assert.Single(await service.GetAllAsync());
        Assert.Equal("Bread Flour", saved.Name);
        Assert.Equal("kg", saved.Unit);
        Assert.Equal(12.346m, saved.QuantityInStock);
        Assert.Equal(2.5m, saved.LowStockThreshold);
        Assert.Equal(expiry, saved.ExpiryDate);
    }

    [Fact]
    public async Task CreateAsync_ZeroQuantityAndZeroThreshold_Succeeds()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var service = new IngredientService(factory);

        var result = await service.CreateAsync(new Ingredient
        {
            Name = "Sea Salt",
            Unit = "kg",
            QuantityInStock = 0m,
            LowStockThreshold = 0m
        });

        Assert.True(result.Success);
        var saved = Assert.Single(await service.GetAllAsync());
        Assert.Equal(0m, saved.QuantityInStock);
        Assert.Equal(0m, saved.LowStockThreshold);
    }

    [Theory]
    [InlineData("", "kg", 5, 1)]
    [InlineData("   ", "kg", 5, 1)]
    [InlineData("Flour", "", 5, 1)]
    [InlineData("Flour", "   ", 5, 1)]
    [InlineData("Flour", "kg", -0.01, 1)]
    [InlineData("Flour", "kg", 5, -1)]
    public async Task CreateAsync_InvalidInput_ReturnsValidationErrorAndDoesNotPersist(
        string name,
        string unit,
        decimal quantity,
        decimal threshold)
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var service = new IngredientService(factory);

        var result = await service.CreateAsync(new Ingredient
        {
            Name = name,
            Unit = unit,
            QuantityInStock = quantity,
            LowStockThreshold = threshold
        });

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task CreateAsync_ExceedingMaxLengths_ReturnsValidationError()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var service = new IngredientService(factory);

        var tooLongName = await service.CreateAsync(new Ingredient
        {
            Name = new string('A', 101),
            Unit = "kg",
            QuantityInStock = 1m,
            LowStockThreshold = 0m
        });

        var tooLongUnit = await service.CreateAsync(new Ingredient
        {
            Name = "Sugar",
            Unit = new string('u', 31),
            QuantityInStock = 1m,
            LowStockThreshold = 0m
        });

        Assert.False(tooLongName.Success);
        Assert.False(tooLongUnit.Success);
    }

    [Fact]
    public async Task UpdateAsync_ExistingIngredient_UpdatesWithoutDuplicating()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var service = new IngredientService(factory);

        await service.CreateAsync(new Ingredient
        {
            Name = "Butter",
            Unit = "kg",
            QuantityInStock = 4m,
            LowStockThreshold = 1m
        });

        var created = Assert.Single(await service.GetAllAsync());
        var updateResult = await service.UpdateAsync(new Ingredient
        {
            Id = created.Id,
            Name = "Unsalted Butter",
            Unit = "kg",
            QuantityInStock = 8.5m,
            LowStockThreshold = 2m
        });

        Assert.True(updateResult.Success);
        var updated = Assert.Single(await service.GetAllAsync());
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Unsalted Butter", updated.Name);
        Assert.Equal(8.5m, updated.QuantityInStock);
        Assert.Equal(2m, updated.LowStockThreshold);
    }

    [Fact]
    public async Task DeleteAsync_UnusedIngredient_DeletesSuccessfully()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var service = new IngredientService(factory);

        await service.CreateAsync(new Ingredient
        {
            Name = "Cinnamon",
            Unit = "g",
            QuantityInStock = 500m,
            LowStockThreshold = 50m
        });

        var created = Assert.Single(await service.GetAllAsync());
        var deleteResult = await service.DeleteAsync(created.Id);

        Assert.True(deleteResult.Success);
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task DeleteAsync_IngredientReferencedByRecipe_FailsAndPreservesIngredient()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var ingredientService = new IngredientService(factory);
        var productService = new ProductService(factory);

        await ingredientService.CreateAsync(new Ingredient
        {
            Name = "Whole Wheat Flour",
            Unit = "kg",
            QuantityInStock = 20m,
            LowStockThreshold = 5m
        });

        var flour = Assert.Single(await ingredientService.GetAllAsync());

        var productResult = await productService.CreateAsync(new ProductInput
        {
            Name = "Country Loaf",
            Category = "Bread",
            BatchYield = 6,
            Price = 5.50m,
            RecipeIngredients = [new RecipeIngredientInput { IngredientId = flour.Id, QuantityRequired = 1.5m }]
        });
        Assert.True(productResult.Success);

        var deleteResult = await ingredientService.DeleteAsync(flour.Id);

        Assert.False(deleteResult.Success);
        Assert.Contains("recipe", deleteResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await ingredientService.GetAllAsync());
    }

    [Fact]
    public async Task DeleteAsync_IngredientReferencedByInventoryTransaction_FailsAndPreservesIngredient()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var ingredientService = new IngredientService(factory);

        await ingredientService.CreateAsync(new Ingredient
        {
            Name = "Yeast",
            Unit = "g",
            QuantityInStock = 250m,
            LowStockThreshold = 50m
        });

        var yeast = Assert.Single(await ingredientService.GetAllAsync());

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                IngredientId = yeast.Id,
                QuantityChange = 250m,
                Reason = TransactionReasons.Restock
            });
            await db.SaveChangesAsync();
        }

        var deleteResult = await ingredientService.DeleteAsync(yeast.Id);

        Assert.False(deleteResult.Success);
        Assert.Contains("transaction", deleteResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await ingredientService.GetAllAsync());
    }
}
