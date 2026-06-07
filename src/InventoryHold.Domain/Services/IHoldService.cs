namespace InventoryHold.Domain.Services;

using InventoryHold.Domain.Models;

public interface IHoldService
{
    /// <summary>
    /// Retrieves a hold by id.
    /// </summary>
    Task<Hold?> GetHoldAsync(string id);

    /// <summary>
    /// Creates a new hold by attempting to reserve inventory and persisting a hold document.
    /// Returns the created <see cref="Hold"/> when successful; returns <c>null</c> when
    /// the SKU does not exist or insufficient stock is available.
    /// </summary>
    Task<Hold?> CreateHoldAsync(string sku, int quantity, string? location = null, string? owner = null, int? holdSeconds = null);

    /// <summary>
    /// Releases an active hold and restores inventory. Returns true when a release occurred.
    /// </summary>
    Task<bool> ReleaseHoldAsync(string holdId);

    /// <summary>
    /// Marks an active hold as expired, restores inventory, and registers the event in outbox.
    /// Returns true when expiration occurred.
    /// </summary>
    Task<bool> ExpireHoldAsync(string holdId);

    /// <summary>
    /// Gets all active holds.
    /// </summary>
    Task<IEnumerable<Hold>> GetActiveHoldsAsync();
}
