using System;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using InventoryHold.Domain.Caching;
using StackExchange.Redis;

namespace InventoryHold.Infrastructure.Caching;

/// <summary>
/// Redis-backed cache implementation. Uses a single ConnectionMultiplexer.
/// </summary>
public class RedisCache : ICache, IDisposable
{
    private readonly ConnectionMultiplexer _conn;
    private readonly IDatabase _db;

    public RedisCache(string connectionString)
    {
        _conn = ConnectionMultiplexer.Connect(connectionString);
        _db = _conn.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var v = await _db.StringGetAsync(key).ConfigureAwait(false);
        if (!v.HasValue) return default;
        var s = (string?)v;
        if (s is null) return default;
        return JsonSerializer.Deserialize<T>(s);
    }

    public Task RemoveAsync(string key)
    {
        return _db.KeyDeleteAsync(key);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(value);
        return _db.StringSetAsync(key, json, ttl);
    }

    public Task AddKeyToSetAsync(string setKey, string member)
    {
        return _db.SetAddAsync(setKey, member);
    }

    public async Task<string[]> GetSetMembersAsync(string setKey)
    {
        var members = await _db.SetMembersAsync(setKey).ConfigureAwait(false);
        if (members.Length == 0) return Array.Empty<string>();
        var res = members.Select(m => (string)m!).ToArray();
        return res;
    }

    public Task RemoveSetAsync(string setKey)
    {
        return _db.KeyDeleteAsync(setKey);
    }

    public void Dispose()
    {
        _conn?.Dispose();
    }
}
 
