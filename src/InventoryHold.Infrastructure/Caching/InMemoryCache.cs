using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryHold.Domain.Caching;

namespace InventoryHold.Infrastructure.Caching;

/// <summary>
/// Simple in-memory cache used as a fallback for development and tests.
/// Not distributed and not intended for production scale.
/// </summary>
public class InMemoryCache : ICache
{
    private readonly ConcurrentDictionary<string, (string Value, DateTime Expiry)> _store = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _sets = new();

    public Task<T?> GetAsync<T>(string key)
    {
        if (_store.TryGetValue(key, out var entry))
        {
            if (entry.Expiry > DateTime.UtcNow)
            {
                var obj = JsonSerializer.Deserialize<T>(entry.Value);
                return Task.FromResult(obj);
            }
            else
            {
                _store.TryRemove(key, out _);
            }
        }

        return Task.FromResult<T?>(default);
    }

    public Task RemoveAsync(string key)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task AddKeyToSetAsync(string setKey, string member)
    {
        var set = _sets.GetOrAdd(setKey, _ => new ConcurrentDictionary<string, byte>());
        set[member] = 0;
        return Task.CompletedTask;
    }

    public Task<string[]> GetSetMembersAsync(string setKey)
    {
        if (_sets.TryGetValue(setKey, out var set))
        {
            var keys = set.Keys.ToArray();
            return Task.FromResult(keys);
        }

        return Task.FromResult(Array.Empty<string>());
    }

    public Task RemoveSetAsync(string setKey)
    {
        _sets.TryRemove(setKey, out _);
        return Task.CompletedTask;
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(value);
        var expiry = DateTime.UtcNow.Add(ttl);
        _store[key] = (json, expiry);
        return Task.CompletedTask;
    }
}
