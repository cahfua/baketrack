using BakeTrack.Data;
using BakeTrack.Models;
using BakeTrack.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace BakeTrack.Tests;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class RelationalConstraintsTests
{
    private readonly PostgresDatabaseFixture _fixture;

    public RelationalConstraintsTests(PostgresDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private static string UniqueName(string prefix)
        => $"{prefix} {Guid.NewGuid():N}";

    [Fact]
    public async Task DbContext_DuplicateProductIngredientPair_ThrowsDbUpdateException()
    {
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);

        await using var db = await factory.CreateDbContextAsync();
        var ingredient = new Ingredient { Name = UniqueName("Sugar"), Unit = "kg", QuantityInStock = 10m, LowStockThreshold = 1m };
        var product = new Product { Name = UniqueName("Pound Cake"), Category = "Cake", BatchYield = 2, Price = 12m };
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
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);

        int ingredientId;
        await using (var setupDb = await factory.CreateDbContextAsync())
        {
            var ingredient = new Ingredient { Name = UniqueName("Cocoa"), Unit = "kg", QuantityInStock = 5m, LowStockThreshold = 1m };
            var product = new Product { Name = UniqueName("Brownie"), Category = "Cake", BatchYield = 16, Price = 3m };
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
        await using var factory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);

        int productId;
        int ingredientId;
        await using (var setupDb = await factory.CreateDbContextAsync())
        {
            var ingredient = new Ingredient { Name = UniqueName("Milk"), Unit = "L", QuantityInStock = 10m, LowStockThreshold = 2m };
            var product = new Product { Name = UniqueName("Brioche"), Category = "Bread", BatchYield = 8, Price = 4m };
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
        Assert.Empty(await verifyDb.RecipeIngredients.Where(r => r.ProductId == productId).ToListAsync());
        Assert.True(await verifyDb.Ingredients.AnyAsync(i => i.Id == ingredientId));
    }

    [Fact]
    public async Task PostgresBakeryDbFactory_Disposal_RollsBackUncommittedChangesWithoutAffectingDatabase()
    {
        var uniqueIngredientName = UniqueName("Rollback Verification");
        int createdIngredientId;

        await using (var scopedFactory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString))
        {
            await using (var writeDb = await scopedFactory.CreateDbContextAsync())
            {
                var ingredient = new Ingredient
                {
                    Name = uniqueIngredientName,
                    Unit = "kg",
                    QuantityInStock = 3.5m,
                    LowStockThreshold = 1m
                };
                writeDb.Ingredients.Add(ingredient);
                await writeDb.SaveChangesAsync();
                createdIngredientId = ingredient.Id;
            }

            await using var readDb = await scopedFactory.CreateDbContextAsync();
            Assert.True(await readDb.Ingredients.AnyAsync(i => i.Id == createdIngredientId));
        }

        await using var freshFactory = await PostgresBakeryDbFactory.CreateAsync(_fixture.ConnectionString);
        await using var verifyDb = await freshFactory.CreateDbContextAsync();
        Assert.False(await verifyDb.Ingredients.AnyAsync(i => i.Id == createdIngredientId || i.Name == uniqueIngredientName));
    }

    [Theory]
    [InlineData("Host=dpg-abc123-a.oregon-postgres.render.com;Port=5432;Database=baketrack;Username=user;Password=pass")]
    [InlineData("postgres://user:pass@dpg-abc123-a/baketrack")]
    public void DatabaseConfiguration_RenderHostInTestContext_ThrowsInvalidOperationException(string renderConnection)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            DatabaseConfiguration.EnsureLocalConnectionString(renderConnection, "test suite"));

        Assert.Contains("Blocked attempt", ex.Message, StringComparison.Ordinal);
    }
}
