namespace InventoryHold.Contracts.Models;

/// <summary>
/// Request model for creating a new inventory hold.
/// </summary>
public sealed class HoldCreateRequest
{
    /// <summary>
    /// Product SKU to reserve.
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Quantity to reserve for the hold.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Optional inventory location for the hold.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Optional owner identifier for the hold, such as a customer or session id.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Optional hold duration in seconds. If omitted, the service default is applied.
    /// </summary>
    public int? HoldSeconds { get; set; }
}
