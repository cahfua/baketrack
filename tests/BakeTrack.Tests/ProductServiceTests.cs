using BakeTrack.Models;
using BakeTrack.Services;
using BakeTrack.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Tests;

public sealed class ProductServiceTests
{
    private static async Task<int> SeedIngredientAsync(
        SqliteBakeryDbFactory factory,
        string name,
        string unit = "kg",
        decimal stock = 25m)
    {
        await using var db = await factory.CreateDbContextAsync();
        var ingredient = new Ingredient
        {
            Name = name,
            Unit = unit,
            QuantityInStock = stock,
            LowStockThreshold = 2m
        };
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync();
        return ingredient.Id;
    }

    [Fact]
    public async Task CreateAsync_ValidProductWithRecipe_PersistsAcrossContexts()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var flourId = await SeedIngredientAsync(factory, "Bread Flour", "kg");
        var waterId = await SeedIngredientAsync(factory, "Water", "L");
        var service = new ProductService(factory);

        var result = await service.CreateAsync(new ProductInput
        {
            Name = "  Sourdough Boule  ",
            Category = "  Bread ",
            BatchYield = 8,
            Price = 6.755m,
            Description = "  Naturally leavened loaf  ",
            RecipeIngredients =
            [
                new RecipeIngredientInput { IngredientId = waterId, QuantityRequired = 1.25m },
                new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 2.5m }
            ]
        });

        Assert.True(result.Success);
        Assert.True(result.Value > 0);

        var list = await service.GetAllAsync();
        var summary = Assert.Single(list);
        Assert.Equal("Sourdough Boule", summary.Name);
        Assert.Equal("Bread", summary.Category);
        Assert.Equal(8, summary.BatchYield);
        Assert.Equal(6.76m, summary.Price);
        Assert.Equal("Naturally leavened loaf", summary.Description);
        Assert.Equal(2, summary.IngredientCount);

        var detail = await service.GetByIdAsync(result.Value);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.RecipeIngredients.Count);
        Assert.Equal("Bread Flour", detail.RecipeIngredients[0].IngredientName);
        Assert.Equal("kg", detail.RecipeIngredients[0].Unit);
        Assert.Equal(2.5m, detail.RecipeIngredients[0].QuantityRequired);
        Assert.Equal("Water", detail.RecipeIngredients[1].IngredientName);
        Assert.Equal("L", detail.RecipeIngredients[1].Unit);
    }

    [Fact]
    public async Task CreateAsync_ZeroPrice_Succeeds()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var service = new ProductService(factory);

        var result = await service.CreateAsync(new ProductInput
        {
            Name = "Baker Sample Roll",
            Category = "Bread",
            BatchYield = 12,
            Price = 0m
        });

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CreateAsync_DuplicateProductNameCaseInsensitive_ReturnsError()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var service = new ProductService(factory);

        var first = await service.CreateAsync(new ProductInput
        {
            Name = "Croissant",
            Category = "Pastry",
            BatchYield = 24,
            Price = 3.50m
        });
        Assert.True(first.Success);

        var duplicate = await service.CreateAsync(new ProductInput
        {
            Name = "  croissant ",
            Category = "Pastry",
            BatchYield = 12,
            Price = 4.00m
        });

        Assert.False(duplicate.Success);
        Assert.Contains("already exists", duplicate.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Single(await service.GetAllAsync());
    }

    [Theory]
    [InlineData("", "Bread", 1, 5.00)]
    [InlineData("   ", "Bread", 1, 5.00)]
    [InlineData("Baguette", "", 1, 5.00)]
    [InlineData("Baguette", "   ", 1, 5.00)]
    [InlineData("Baguette", "Bread", 0, 5.00)]
    [InlineData("Baguette", "Bread", -3, 5.00)]
    [InlineData("Baguette", "Bread", 100001, 5.00)]
    [InlineData("Baguette", "Bread", 4, -0.01)]
    public async Task CreateAsync_InvalidProductFields_ReturnsValidationError(
        string name,
        string category,
        int batchYield,
        decimal price)
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var service = new ProductService(factory);

        var result = await service.CreateAsync(new ProductInput
        {
            Name = name,
            Category = category,
            BatchYield = batchYield,
            Price = price
        });

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task CreateAsync_InvalidRecipeItems_ReturnsValidationError()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var flourId = await SeedIngredientAsync(factory, "Flour");
        var service = new ProductService(factory);

        var zeroQty = await service.CreateAsync(new ProductInput
        {
            Name = "Brioche",
            Category = "Bread",
            BatchYield = 10,
            Price = 4.50m,
            RecipeIngredients = [new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 0m }]
        });

        var duplicateIngredient = await service.CreateAsync(new ProductInput
        {
            Name = "Brioche",
            Category = "Bread",
            BatchYield = 10,
            Price = 4.50m,
            RecipeIngredients =
            [
                new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 1m },
                new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 2m }
            ]
        });

        var missingIngredient = await service.CreateAsync(new ProductInput
        {
            Name = "Brioche",
            Category = "Bread",
            BatchYield = 10,
            Price = 4.50m,
            RecipeIngredients = [new RecipeIngredientInput { IngredientId = 9999, QuantityRequired = 1m }]
        });

        Assert.False(zeroQty.Success);
        Assert.False(duplicateIngredient.Success);
        Assert.False(missingIngredient.Success);
        Assert.Empty(await service.GetAllAsync());
    }

    [Fact]
    public async Task UpdateAsync_ValidChanges_UpdatesMetadataWithoutWipingRecipe()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var flourId = await SeedIngredientAsync(factory, "Flour");
        var service = new ProductService(factory);

        var created = await service.CreateAsync(new ProductInput
        {
            Name = "Focaccia",
            Category = "Bread",
            BatchYield = 6,
            Price = 7.00m,
            RecipeIngredients = [new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 1.8m }]
        });

        var updateResult = await service.UpdateAsync(new ProductInput
        {
            Id = created.Value,
            Name = "Rosemary Focaccia",
            Category = "Artisan Bread",
            BatchYield = 8,
            Price = 8.50m,
            Description = "Topped with sea salt and rosemary"
        });

        Assert.True(updateResult.Success);
        var detail = await service.GetByIdAsync(created.Value);
        Assert.NotNull(detail);
        Assert.Equal("Rosemary Focaccia", detail.Name);
        Assert.Equal("Artisan Bread", detail.Category);
        Assert.Equal(8, detail.BatchYield);
        Assert.Equal(8.50m, detail.Price);
        Assert.Single(detail.RecipeIngredients);
    }

    [Fact]
    public async Task SaveRecipeAsync_SyncsAddsUpdatesAndRemovals()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var flourId = await SeedIngredientAsync(factory, "Flour", "kg");
        var butterId = await SeedIngredientAsync(factory, "Butter", "kg");
        var sugarId = await SeedIngredientAsync(factory, "Sugar", "kg");
        var service = new ProductService(factory);

        var created = await service.CreateAsync(new ProductInput
        {
            Name = "Butter Cookie",
            Category = "Cookies",
            BatchYield = 24,
            Price = 1.50m,
            RecipeIngredients =
            [
                new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 1.0m },
                new RecipeIngredientInput { IngredientId = butterId, QuantityRequired = 0.5m }
            ]
        });

        var saveResult = await service.SaveRecipeAsync(
            created.Value,
            [
                new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 1.25m },
                new RecipeIngredientInput { IngredientId = sugarId, QuantityRequired = 0.4m }
            ]);

        Assert.True(saveResult.Success);

        var detail = await service.GetByIdAsync(created.Value);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.RecipeIngredients.Count);
        Assert.Collection(
            detail.RecipeIngredients,
            first =>
            {
                Assert.Equal(flourId, first.IngredientId);
                Assert.Equal(1.25m, first.QuantityRequired);
            },
            second =>
            {
                Assert.Equal(sugarId, second.IngredientId);
                Assert.Equal(0.4m, second.QuantityRequired);
            });
    }

    [Fact]
    public async Task SingleItemRecipeOperations_AddUpdateRemove_EnforceRules()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var flourId = await SeedIngredientAsync(factory, "Flour", "kg");
        var service = new ProductService(factory);

        var created = await service.CreateAsync(new ProductInput
        {
            Name = "Baguette",
            Category = "Bread",
            BatchYield = 10,
            Price = 3.25m
        });

        var addOk = await service.AddRecipeIngredientAsync(
            created.Value,
            new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 2.0m });
        var addDup = await service.AddRecipeIngredientAsync(
            created.Value,
            new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 1.0m });
        var updateBad = await service.UpdateRecipeIngredientQuantityAsync(created.Value, flourId, 0m);
        var updateOk = await service.UpdateRecipeIngredientQuantityAsync(created.Value, flourId, 2.75m);

        Assert.True(addOk.Success);
        Assert.False(addDup.Success);
        Assert.False(updateBad.Success);
        Assert.True(updateOk.Success);

        var detailAfterUpdate = await service.GetByIdAsync(created.Value);
        Assert.Equal(2.75m, Assert.Single(detailAfterUpdate!.RecipeIngredients).QuantityRequired);

        var removeOk = await service.RemoveRecipeIngredientAsync(created.Value, flourId);
        Assert.True(removeOk.Success);

        var detailAfterRemove = await service.GetByIdAsync(created.Value);
        Assert.Empty(detailAfterRemove!.RecipeIngredients);

        await using var db = await factory.CreateDbContextAsync();
        Assert.True(await db.Ingredients.AnyAsync(i => i.Id == flourId));
    }

    [Fact]
    public async Task DeleteAsync_ProductWithRecipeAndNoProduction_CascadesRecipeAndKeepsIngredients()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var flourId = await SeedIngredientAsync(factory, "Flour", "kg");
        var service = new ProductService(factory);

        var created = await service.CreateAsync(new ProductInput
        {
            Name = "Cinnamon Roll",
            Category = "Pastry",
            BatchYield = 12,
            Price = 3.75m,
            RecipeIngredients = [new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 1.5m }]
        });

        var deleteResult = await service.DeleteAsync(created.Value);

        Assert.True(deleteResult.Success);
        Assert.Null(await service.GetByIdAsync(created.Value));

        await using var db = await factory.CreateDbContextAsync();
        Assert.Empty(await db.RecipeIngredients.ToListAsync());
        Assert.True(await db.Ingredients.AnyAsync(i => i.Id == flourId));
    }

    [Fact]
    public async Task DeleteAsync_ProductWithProductionRecords_IsBlockedAndPreservesData()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();
        var flourId = await SeedIngredientAsync(factory, "Flour", "kg");
        var service = new ProductService(factory);

        var created = await service.CreateAsync(new ProductInput
        {
            Name = "Muffin",
            Category = "Muffins & Scones",
            BatchYield = 12,
            Price = 2.50m,
            RecipeIngredients = [new RecipeIngredientInput { IngredientId = flourId, QuantityRequired = 0.8m }]
        });

        await using (var db = await factory.CreateDbContextAsync())
        {
            db.ProductionRecords.Add(new ProductionRecord
            {
                ProductId = created.Value,
                QuantityProduced = 12
            });
            await db.SaveChangesAsync();
        }

        var deleteResult = await service.DeleteAsync(created.Value);

        Assert.False(deleteResult.Success);
        Assert.Contains("production", deleteResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var detail = await service.GetByIdAsync(created.Value);
        Assert.NotNull(detail);
        Assert.Single(detail.RecipeIngredients);
    }
}
