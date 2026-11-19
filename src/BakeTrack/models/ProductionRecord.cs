
namespace BakeTrack.Models;

public class ProductionRecord
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int QuantityProduced { get; set; }
    public DateTime ProductionDate { get; set; } = DateTime.UtcNow;
}
