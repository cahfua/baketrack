using System.ComponentModel.DataAnnotations;

namespace BakeTrack.Models;

public class Ingredient
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ingredient name is required.")]
    [StringLength(100, ErrorMessage = "Ingredient name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit of measurement is required.")]
    [StringLength(30, ErrorMessage = "Unit cannot exceed 30 characters.")]
    public string Unit { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999999.999", ErrorMessage = "Quantity in stock must be 0 or greater.")]
    public decimal QuantityInStock { get; set; }

    [Range(typeof(decimal), "0", "999999999999.999", ErrorMessage = "Minimum stock level must be 0 or greater.")]
    public decimal LowStockThreshold { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public List<RecipeIngredient> RecipeIngredients { get; set; } = new();
    public List<InventoryTransaction> InventoryTransactions { get; set; } = new();
}
