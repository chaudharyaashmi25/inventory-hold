namespace InventoryHold.Domain.Repositories;

using InventoryHold.Domain.Models;

/// <summary>
/// Repository contract for hold lifecycle operations.
/// </summary>
public interface IHoldRepository
{
    /// <summary>
    /// Retrieves a hold by its identifier.
    /// </summary>
    Task<Hold?> GetByIdAsync(string id);

    /// <summary>
    /// Persists a new hold document.
    /// </summary>
    Task<Hold> CreateAsync(Hold hold);

    /// <summary>
    /// Marks an active hold as released. Returns true when release succeeded.
    /// </summary>
    Task<bool> ReleaseAsync(string id);

    /// <summary>
    /// Marks an active hold as expired. Returns true when the status was updated.
    /// </summary>
    Task<bool> MarkExpiredAsync(string id);

    /// <summary>
    /// Persists a new hold document and enqueues an outbox entry atomically if transactions are supported.
    /// </summary>
    Task<bool> CreateWithOutboxAsync(Hold hold, OutboxEntry outboxEntry);

    /// <summary>
    /// Marks an active hold as released and enqueues an outbox entry atomically if transactions are supported.
    /// </summary>
    Task<bool> ReleaseWithOutboxAsync(string holdId, OutboxEntry outboxEntry);

    /// <summary>
    /// Marks an active hold as expired and enqueues an outbox entry atomically if transactions are supported.
    /// </summary>
    Task<bool> ExpireWithOutboxAsync(string holdId, OutboxEntry outboxEntry);

    /// <summary>
    /// Retrieves all active holds.
    /// </summary>
    Task<IEnumerable<Hold>> GetActiveHoldsAsync();
}
