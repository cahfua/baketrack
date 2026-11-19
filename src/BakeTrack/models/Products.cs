
namespace BakeTrack.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    public List<RecipeIngredient> RecipeIngredients { get; set; } = new();
    public List<ProductionRecord> ProductionRecords { get; set; } = new();
}
