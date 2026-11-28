using System.ComponentModel.DataAnnotations;

namespace BakeTrack.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product name is required.")]
    [StringLength(100, ErrorMessage = "Product name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    [StringLength(60, ErrorMessage = "Category cannot exceed 60 characters.")]
    public string Category { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Batch yield must be at least 1.")]
    public int BatchYield { get; set; } = 1;

    [Range(typeof(decimal), "0", "999999999999.99", ErrorMessage = "Price must be 0 or greater.")]
    public decimal Price { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string Description { get; set; } = string.Empty;

    public List<RecipeIngredient> RecipeIngredients { get; set; } = new();
    public List<ProductionRecord> ProductionRecords { get; set; } = new();
}
