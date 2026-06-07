namespace InventoryHold.Domain.Caching;

using System;
using System.Threading.Tasks;

/// <summary>
/// Simple cache abstraction used for read-heavy paths. Implementations should
/// be mockable for unit tests and support basic get/set/remove operations.
/// </summary>
public interface ICache
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
    Task RemoveAsync(string key);
    // Track keys in a named set (used to record which cached list/results reference a SKU)
    Task AddKeyToSetAsync(string setKey, string member);
    Task<string[]> GetSetMembersAsync(string setKey);
    Task RemoveSetAsync(string setKey);
}
