namespace InventoryHold.Contracts.Models;

/// <summary>
/// Response model returned for a created or retrieved hold.
/// </summary>
public sealed class HoldResponse
{
    /// <summary>
    /// Unique hold identifier.
    /// </summary>
    public string HoldId { get; init; } = string.Empty;

    /// <summary>
    /// Reserved product SKU.
    /// </summary>
    public string Sku { get; init; } = string.Empty;

    /// <summary>
    /// Reserved quantity.
    /// </summary>
    public int Quantity { get; init; }

    /// <summary>
    /// Inventory location associated with the hold.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Optional owner identifier for the hold.
    /// </summary>
    public string? Owner { get; init; }

    /// <summary>
    /// Current hold status.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// When the hold was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the hold expires.
    /// </summary>
    public DateTime ExpiresAt { get; init; }
}
