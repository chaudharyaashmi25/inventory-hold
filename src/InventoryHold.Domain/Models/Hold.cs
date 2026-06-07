using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace InventoryHold.Domain.Models;

/// <summary>
/// Domain entity representing a reservation hold on inventory.
/// </summary>
public sealed class Hold
{
    /// <summary>
    /// Unique hold identifier.
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Reserved product SKU.
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Quantity reserved by the hold.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Optional location for the reserved inventory.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Optional owner identifier for the hold.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// Current status of the hold.
    /// </summary>
    public HoldStatus Status { get; set; }

    /// <summary>
    /// Time the hold was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Time the hold expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
