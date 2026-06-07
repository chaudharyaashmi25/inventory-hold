using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventoryHold.Domain.Models;

/// <summary>
/// Domain entity representing inventory levels for a product.
/// </summary>
public sealed class ProductInventory
{
    /// <summary>
    /// Unique product document identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Product SKU.
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Product name or description.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional inventory location.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Total available stock for this product.
    /// </summary>
    public int TotalQuantity { get; set; }

    /// <summary>
    /// Quantity currently available for new holds.
    /// </summary>
    public int AvailableQuantity { get; set; }

    /// <summary>
    /// Quantity reserved by active holds.
    /// </summary>
    public int ReservedQuantity { get; set; }

    /// <summary>
    /// Concurrency version used for optimistic updates.
    /// </summary>
    public int Version { get; set; }
}
