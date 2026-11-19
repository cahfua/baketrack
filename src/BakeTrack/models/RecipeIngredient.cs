
namespace BakeTrack.Models;

public class RecipeIngredient
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    public decimal QuantityRequired { get; set; }
}
