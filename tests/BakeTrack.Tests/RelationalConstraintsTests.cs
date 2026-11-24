using BakeTrack.Models;
using BakeTrack.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Tests;

public sealed class RelationalConstraintsTests
{
    [Fact]
    public async Task DbContext_DuplicateProductIngredientPair_ThrowsDbUpdateException()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();

        await using var db = await factory.CreateDbContextAsync();
        var ingredient = new Ingredient { Name = "Sugar", Unit = "kg", QuantityInStock = 10m, LowStockThreshold = 1m };
        var product = new Product { Name = "Pound Cake", Category = "Cake", BatchYield = 2, Price = 12m };
        db.Ingredients.Add(ingredient);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        db.RecipeIngredients.AddRange(
            new RecipeIngredient { ProductId = product.Id, IngredientId = ingredient.Id, QuantityRequired = 0.5m },
            new RecipeIngredient { ProductId = product.Id, IngredientId = ingredient.Id, QuantityRequired = 0.25m });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task DbContext_DeleteIngredientUsedInRecipe_EnforcesRestrictBehavior()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();

        int ingredientId;
        await using (var setupDb = await factory.CreateDbContextAsync())
        {
            var ingredient = new Ingredient { Name = "Cocoa", Unit = "kg", QuantityInStock = 5m, LowStockThreshold = 1m };
            var product = new Product { Name = "Brownie", Category = "Cake", BatchYield = 16, Price = 3m };
            setupDb.Ingredients.Add(ingredient);
            setupDb.Products.Add(product);
            await setupDb.SaveChangesAsync();

            setupDb.RecipeIngredients.Add(new RecipeIngredient
            {
                ProductId = product.Id,
                IngredientId = ingredient.Id,
                QuantityRequired = 0.3m
            });
            await setupDb.SaveChangesAsync();
            ingredientId = ingredient.Id;
        }

        await using var deleteDb = await factory.CreateDbContextAsync();
        var target = await deleteDb.Ingredients.FindAsync(ingredientId);
        Assert.NotNull(target);
        deleteDb.Ingredients.Remove(target);

        await Assert.ThrowsAsync<DbUpdateException>(() => deleteDb.SaveChangesAsync());
    }

    [Fact]
    public async Task DbContext_DeleteProductWithoutProduction_EnforcesCascadeToRecipes()
    {
        await using var factory = await SqliteBakeryDbFactory.CreateAsync();

        int productId;
        int ingredientId;
        await using (var setupDb = await factory.CreateDbContextAsync())
        {
            var ingredient = new Ingredient { Name = "Milk", Unit = "L", QuantityInStock = 10m, LowStockThreshold = 2m };
            var product = new Product { Name = "Brioche", Category = "Bread", BatchYield = 8, Price = 4m };
            setupDb.Ingredients.Add(ingredient);
            setupDb.Products.Add(product);
            await setupDb.SaveChangesAsync();

            setupDb.RecipeIngredients.Add(new RecipeIngredient
            {
                ProductId = product.Id,
                IngredientId = ingredient.Id,
                QuantityRequired = 0.5m
            });
            await setupDb.SaveChangesAsync();
            productId = product.Id;
            ingredientId = ingredient.Id;
        }

        await using (var deleteDb = await factory.CreateDbContextAsync())
        {
            var target = await deleteDb.Products.FindAsync(productId);
            Assert.NotNull(target);
            deleteDb.Products.Remove(target);
            await deleteDb.SaveChangesAsync();
        }

        await using var verifyDb = await factory.CreateDbContextAsync();
        Assert.Empty(await verifyDb.RecipeIngredients.ToListAsync());
        Assert.True(await verifyDb.Ingredients.AnyAsync(i => i.Id == ingredientId));
    }
}
