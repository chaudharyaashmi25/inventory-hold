namespace InventoryHold.Domain.Repositories;

using InventoryHold.Domain.Models;

/// <summary>
/// Repository contract for inventory read and update operations.
/// Implementations must ensure atomicity for reservation operations.
/// </summary>
public interface IInventoryRepository
{
    /// <summary>
    /// Retrieves a product inventory document by SKU.
    /// </summary>
    /// <param name="sku">Product SKU.</param>
    /// <returns>The <see cref="ProductInventory"/> or <c>null</c> if not found.</returns>
    Task<ProductInventory?> GetBySkuAsync(string sku);

    /// <summary>
    /// Lists product inventory documents optionally filtered by sku or location.
    /// </summary>
    /// <param name="sku">Optional SKU filter.</param>
    /// <param name="location">Optional location filter.</param>
    /// <param name="availableOnly">If true, only returns products with AvailableQuantity &gt; 0.</param>
    Task<IEnumerable<ProductInventory>> ListAsync(string? sku = null, string? location = null, bool availableOnly = false);

    /// <summary>
    /// Attempts to reserve (decrement) available quantity for a SKU using an atomic operation.
    /// Returns the updated <see cref="ProductInventory"/> when successful, or <c>null</c> when insufficient stock.
    /// </summary>
    /// <param name="sku">Product SKU.</param>
    /// <param name="quantity">Quantity to reserve.</param>
    /// <param name="location">Optional location for scoped reservation.</param>
    Task<ProductInventory?> TryReserveAsync(string sku, int quantity, string? location = null);

    /// <summary>
    /// Releases previously reserved quantity back to available stock.
    /// Returns true when the release modified a product document.
    /// </summary>
    Task<bool> ReleaseReservedQuantityAsync(string sku, int quantity, string? location = null);
}
