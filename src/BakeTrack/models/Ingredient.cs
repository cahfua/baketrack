
namespace BakeTrack.Models;

public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal QuantityInStock { get; set; }
    public decimal LowStockThreshold { get; set; }

    public List<RecipeIngredient> RecipeIngredients { get; set; } = new();
    public List<InventoryTransaction> InventoryTransactions { get; set; } = new();
}
