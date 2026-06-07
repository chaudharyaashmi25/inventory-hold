namespace InventoryHold.Domain.Services;

using InventoryHold.Domain.Models;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Messaging;
using InventoryHold.Contracts.Events;
using System.Threading;

public class HoldService : IHoldService
{
    private readonly IHoldRepository _holdRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IMessageBus _bus;
    private readonly Domain.Caching.ICache _cache;
    private readonly Domain.Repositories.IOutboxRepository _outbox;

    public HoldService(IHoldRepository holdRepo, IInventoryRepository inventoryRepo, IMessageBus bus, Domain.Caching.ICache cache, Domain.Repositories.IOutboxRepository outbox)
    {
        _holdRepo = holdRepo;
        _inventoryRepo = inventoryRepo;
        _bus = bus;
        _cache = cache;
        _outbox = outbox;
    }

    /// <inheritdoc />
    public Task<Hold?> GetHoldAsync(string id) => _holdRepo.GetByIdAsync(id);

    /// <inheritdoc />
    public async Task<Hold?> CreateHoldAsync(string sku, int quantity, string? location = null, string? owner = null, int? holdSeconds = null)
    {
        if (string.IsNullOrWhiteSpace(sku)) throw new ArgumentException("sku");
        if (quantity <= 0) throw new ArgumentException("quantity must be > 0", nameof(quantity));

        // Ensure product exists
        var product = await _inventoryRepo.GetBySkuAsync(sku);
        if (product is null) return null;

        // Attempt atomic reserve on product document
        var reserved = await _inventoryRepo.TryReserveAsync(sku, quantity, location);
        if (reserved is null)
        {
            // insufficient stock
            return null;
        }

        // Prepare hold with generated ID to ensure event payload matches DB document ID
        var now = DateTime.UtcNow;
        var hold = new Hold
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Sku = sku,
            Quantity = quantity,
            Location = location,
            Owner = owner,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(holdSeconds ?? 15 * 60),
            Status = HoldStatus.Active
        };

        var prodSnapshot = reserved ?? await _inventoryRepo.GetBySkuAsync(sku);
        var evt = new HoldCreatedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            HoldId: hold.Id,
            Sku: hold.Sku,
            Quantity: hold.Quantity,
            Location: hold.Location,
            Owner: hold.Owner,
            CreatedAt: new DateTimeOffset(hold.CreatedAt),
            ExpiresAt: new DateTimeOffset(hold.ExpiresAt),
            ProductAvailableQuantity: prodSnapshot?.AvailableQuantity ?? 0,
            ProductTotalQuantity: prodSnapshot?.TotalQuantity ?? 0,
            CorrelationId: null
        );

        var payload = System.Text.Json.JsonSerializer.Serialize(evt);
        var entry = new Domain.Models.OutboxEntry
        {
            EventType = "hold.created",
            Payload = payload,
        };

        await _holdRepo.CreateWithOutboxAsync(hold, entry);

        // Invalidate relevant caches
        await InvalidateCacheAsync(hold.Id, hold.Sku);

        return hold;
    }

    /// <inheritdoc />
    public async Task<bool> ReleaseHoldAsync(string holdId)
    {
        if (string.IsNullOrWhiteSpace(holdId)) return false;

        var hold = await _holdRepo.GetByIdAsync(holdId);
        if (hold is null) return false;

        if (hold.Status != HoldStatus.Active)
        {
            // nothing to do
            return false;
        }

        // First restore inventory, then mark hold released
        var releasedInventory = await _inventoryRepo.ReleaseReservedQuantityAsync(hold.Sku, hold.Quantity, hold.Location);

        if (!releasedInventory)
        {
            return false;
        }

        var prodSnapshot = await _inventoryRepo.GetBySkuAsync(hold.Sku);
        var evt = new HoldReleasedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            HoldId: hold.Id,
            Sku: hold.Sku,
            Quantity: hold.Quantity,
            Location: hold.Location,
            Owner: hold.Owner,
            ReleasedAt: DateTimeOffset.UtcNow,
            ProductAvailableQuantity: prodSnapshot?.AvailableQuantity ?? 0,
            ProductTotalQuantity: prodSnapshot?.TotalQuantity ?? 0,
            Reason: "customer_released",
            CorrelationId: null
        );

        var payload = System.Text.Json.JsonSerializer.Serialize(evt);
        var entry = new Domain.Models.OutboxEntry { EventType = "hold.released", Payload = payload };

        var mark = await _holdRepo.ReleaseWithOutboxAsync(holdId, entry);

        if (!mark)
        {
            // Attempt to reverse inventory restore if marking failed
            await _inventoryRepo.TryReserveAsync(hold.Sku, hold.Quantity, hold.Location);
            return false;
        }

        // Invalidate caches
        await InvalidateCacheAsync(holdId, hold.Sku);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExpireHoldAsync(string holdId)
    {
        if (string.IsNullOrWhiteSpace(holdId)) return false;

        var hold = await _holdRepo.GetByIdAsync(holdId);
        if (hold is null) return false;

        if (hold.Status != HoldStatus.Active)
        {
            return false;
        }

        // Restore inventory
        var restored = await _inventoryRepo.ReleaseReservedQuantityAsync(hold.Sku, hold.Quantity, hold.Location);
        if (!restored)
        {
            return false;
        }

        var prodSnapshot = await _inventoryRepo.GetBySkuAsync(hold.Sku);
        var evt = new HoldExpiredEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTimeOffset.UtcNow,
            HoldId: hold.Id,
            Sku: hold.Sku,
            Quantity: hold.Quantity,
            Location: hold.Location,
            ExpiredAt: DateTimeOffset.UtcNow,
            ProductAvailableQuantity: prodSnapshot?.AvailableQuantity ?? 0,
            ProductTotalQuantity: prodSnapshot?.TotalQuantity ?? 0,
            Reason: "ttl_expired",
            CorrelationId: null
        );

        var payload = System.Text.Json.JsonSerializer.Serialize(evt);
        var entry = new Domain.Models.OutboxEntry { EventType = "hold.expired", Payload = payload };

        var mark = await _holdRepo.ExpireWithOutboxAsync(holdId, entry);

        if (!mark)
        {
            // Attempt to reverse inventory restore if marking failed
            await _inventoryRepo.TryReserveAsync(hold.Sku, hold.Quantity, hold.Location);
            return false;
        }

        // Invalidate caches
        await InvalidateCacheAsync(holdId, hold.Sku);

        return true;
    }

    /// <inheritdoc />
    public Task<IEnumerable<Hold>> GetActiveHoldsAsync() => _holdRepo.GetActiveHoldsAsync();

    private async Task InvalidateCacheAsync(string holdId, string sku)
    {
        try
        {
            await _cache.RemoveAsync($"hold:{holdId}");

            var setKey = $"inventory:keys:sku:{sku}";
            var members = await _cache.GetSetMembersAsync(setKey);
            foreach (var k in members)
            {
                try { await _cache.RemoveAsync(k); } catch { }
            }
            try { await _cache.RemoveSetAsync(setKey); } catch { }

            try { await _cache.RemoveAsync("inventory:list"); } catch { }
        }
        catch
        {
            // swallow cache errors
        }
    }
}
