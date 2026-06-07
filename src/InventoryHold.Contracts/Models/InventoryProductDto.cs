namespace InventoryHold.Contracts.Models;

/// <summary>
/// Data transfer object representing inventory levels for a product.
/// </summary>
public sealed class InventoryProductDto
{
    /// <summary>
    /// Product SKU.
    /// </summary>
    public string Sku { get; init; } = string.Empty;

    /// <summary>
    /// Product name or description.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional inventory location.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Total stock quantity for the product.
    /// </summary>
    public int TotalQuantity { get; init; }

    /// <summary>
    /// Currently available quantity for new holds.
    /// </summary>
    public int AvailableQuantity { get; init; }

    /// <summary>
    /// Quantity already reserved by active holds.
    /// </summary>
    public int ReservedQuantity { get; init; }
}
