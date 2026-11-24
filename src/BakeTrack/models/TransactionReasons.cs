namespace BakeTrack.Models;

/// <summary>
/// Standard reason conventions for <see cref="InventoryTransaction.Reason"/> across BakeTrack workflows.
/// </summary>
public static class TransactionReasons
{
    public const string Restock = "Restock";
    public const string Adjustment = "Adjustment";
    public const string Production = "Production";
    public const string Waste = "Waste";
}
