using System.ComponentModel.DataAnnotations;

namespace BakeTrack.Models;

public class RecipeIngredient
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;

    [Range(typeof(decimal), "0.001", "999999999999.999", ErrorMessage = "Quantity required must be at least 0.001.")]
    public decimal QuantityRequired { get; set; }
}
