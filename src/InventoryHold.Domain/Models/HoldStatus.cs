namespace InventoryHold.Domain.Models;

/// <summary>
/// Possible lifecycle states for an inventory hold.
/// </summary>
public enum HoldStatus
{
    /// <summary>
    /// Hold is currently active and inventory remains reserved.
    /// </summary>
    Active,

    /// <summary>
    /// Hold was released and reserved inventory was returned.
    /// </summary>
    Released,

    /// <summary>
    /// Hold expired and inventory should be restored.
    /// </summary>
    Expired,

    /// <summary>
    /// Hold completed and inventory was consumed by an order.
    /// </summary>
    Completed
}
