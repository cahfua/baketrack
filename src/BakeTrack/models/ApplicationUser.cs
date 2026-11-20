using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BakeTrack.Models;

/// <summary>
/// Represents an authenticated BakeTrack user account managed by an administrator.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<InventoryTransaction> InventoryTransactions { get; set; } = new();
    public List<ProductionRecord> ProductionRecords { get; set; } = new();
}
