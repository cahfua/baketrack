using BakeTrack.Models;
using BakeTrack.Services;
using BakeTrack.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Tests;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class IngredientServiceTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public IngredientServiceTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static string UniqueName(string prefix)
        => $"{prefix} {Guid.NewGuid():N}";

    [Fact]
    public async Task CreateAsync_ValidIngredient_PersistsAndNormalizesAcrossContexts()
    {
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);
        var service = new IngredientService(factory);
        var expiry = new DateOnly(2027, 1, 15);
        var name = UniqueName("Bread Flour");

        var result = await service.CreateAsync(new Ingredient
        {
            Name = $"  {name}  ",
            Unit = "  kg ",
            QuantityInStock = 12.3456m,
            LowStockThreshold = 2.5m,
            ExpiryDate = expiry
        });

        Assert.True(result.Success);

        var saved = Assert.Single(await service.GetAllAsync(), i => i.Name == name);
        Assert.Equal(name, saved.Name);
        Assert.Equal("kg", saved.Unit);
        Assert.Equal(12.346m, saved.QuantityInStock);
        Assert.Equal(2.5m, saved.LowStockThreshold);
        Assert.Equal(expiry, saved.ExpiryDate);
    }

    [Fact]
    public async Task CreateAsync_ZeroQuantityAndZeroThreshold_Succeeds()
    {
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);
        var service = new IngredientService(factory);
        var name = UniqueName("Sea Salt");

        var result = await service.CreateAsync(new Ingredient
        {
            Name = name,
            Unit = "kg",
            QuantityInStock = 0m,
            LowStockThreshold = 0m
        });

        Assert.True(result.Success);
        var saved = Assert.Single(await service.GetAllAsync(), i => i.Name == name);
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
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);
        var service = new IngredientService(factory);
        var initialCount = (await service.GetAllAsync()).Count;

        var result = await service.CreateAsync(new Ingredient
        {
            Name = name,
            Unit = unit,
            QuantityInStock = quantity,
            LowStockThreshold = threshold
        });

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
        Assert.Equal(initialCount, (await service.GetAllAsync()).Count);
    }

    [Fact]
    public async Task CreateAsync_ExceedingMaxLengths_ReturnsValidationError()
    {
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);
        var service = new IngredientService(factory);
        var initialCount = (await service.GetAllAsync()).Count;

        var tooLongName = await service.CreateAsync(new Ingredient
        {
            Name = new string('A', 101),
            Unit = "kg",
            QuantityInStock = 1m,
            LowStockThreshold = 0m
        });

        var tooLongUnit = await service.CreateAsync(new Ingredient
        {
            Name = UniqueName("Sugar"),
            Unit = new string('u', 31),
            QuantityInStock = 1m,
            LowStockThreshold = 0m
        });

        Assert.False(tooLongName.Success);
        Assert.False(tooLongUnit.Success);
        Assert.Equal(initialCount, (await service.GetAllAsync()).Count);
    }

    [Fact]
    public async Task UpdateAsync_ExistingIngredient_UpdatesWithoutDuplicating()
    {
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);
        var service = new IngredientService(factory);
        var initialCount = (await service.GetAllAsync()).Count;
        var initialName = UniqueName("Butter");
        var updatedName = UniqueName("Unsalted Butter");

        await service.CreateAsync(new Ingredient
        {
            Name = initialName,
            Unit = "kg",
            QuantityInStock = 4m,
            LowStockThreshold = 1m
        });

        var created = Assert.Single(await service.GetAllAsync(), i => i.Name == initialName);
        var updateResult = await service.UpdateAsync(new Ingredient
        {
            Id = created.Id,
            Name = updatedName,
            Unit = "kg",
            QuantityInStock = 8.5m,
            LowStockThreshold = 2m
        });

        Assert.True(updateResult.Success);
        Assert.Equal(initialCount + 1, (await service.GetAllAsync()).Count);

        var updated = await service.GetByIdAsync(created.Id);
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal(updatedName, updated.Name);
        Assert.Equal(8.5m, updated.QuantityInStock);
        Assert.Equal(2m, updated.LowStockThreshold);
    }

    [Fact]
    public async Task DeleteAsync_UnusedIngredient_DeletesSuccessfully()
    {
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);
        var service = new IngredientService(factory);
        var name = UniqueName("Cinnamon");

        await service.CreateAsync(new Ingredient
        {
            Name = name,
            Unit = "g",
            QuantityInStock = 500m,
            LowStockThreshold = 50m
        });

        var created = Assert.Single(await service.GetAllAsync(), i => i.Name == name);
        var deleteResult = await service.DeleteAsync(created.Id);

        Assert.True(deleteResult.Success);
        Assert.Null(await service.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task DeleteAsync_IngredientReferencedByRecipe_FailsAndPreservesIngredient()
    {
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);
        var ingredientService = new IngredientService(factory);
        var productService = new ProductService(factory);
        var flourName = UniqueName("Whole Wheat Flour");

        await ingredientService.CreateAsync(new Ingredient
        {
            Name = flourName,
            Unit = "kg",
            QuantityInStock = 20m,
            LowStockThreshold = 5m
        });

        var flour = Assert.Single(await ingredientService.GetAllAsync(), i => i.Name == flourName);

        var productResult = await productService.CreateAsync(new ProductInput
        {
            Name = UniqueName("Country Loaf"),
            Category = "Bread",
            BatchYield = 6,
            Price = 5.50m,
            RecipeIngredients = [new RecipeIngredientInput { IngredientId = flour.Id, QuantityRequired = 1.5m }]
        });
        Assert.True(productResult.Success);

        var deleteResult = await ingredientService.DeleteAsync(flour.Id);

        Assert.False(deleteResult.Success);
        Assert.Contains("recipe", deleteResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(await ingredientService.GetByIdAsync(flour.Id));
    }

    [Fact]
    public async Task DeleteAsync_IngredientReferencedByInventoryTransaction_FailsAndPreservesIngredient()
    {
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);
        var ingredientService = new IngredientService(factory);
        var yeastName = UniqueName("Yeast");

        await ingredientService.CreateAsync(new Ingredient
        {
            Name = yeastName,
            Unit = "g",
            QuantityInStock = 250m,
            LowStockThreshold = 50m
        });

        var yeast = Assert.Single(await ingredientService.GetAllAsync(), i => i.Name == yeastName);

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
        Assert.NotNull(await ingredientService.GetByIdAsync(yeast.Id));
    }
}
