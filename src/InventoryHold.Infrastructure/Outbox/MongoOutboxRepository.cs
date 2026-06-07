using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Models;

namespace InventoryHold.Infrastructure.Outbox;

public class MongoOutboxRepository : IOutboxRepository
{
    private readonly IMongoCollection<OutboxEntry> _col;

    public MongoOutboxRepository(IMongoDatabase db)
    {
        _col = db.GetCollection<OutboxEntry>("outbox");
    }

    public Task CreateAsync(OutboxEntry entry)
    {
        return _col.InsertOneAsync(entry);
    }

    public async Task<IEnumerable<OutboxEntry>> GetUnpublishedAsync(int limit)
    {
        var filter = Builders<OutboxEntry>.Filter.Eq(e => e.PublishedAt, null);
        var sort = Builders<OutboxEntry>.Sort.Ascending(e => e.CreatedAt);
        var res = await _col.Find(filter).Sort(sort).Limit(limit).ToListAsync();
        return res;
    }

    public Task MarkPublishedAsync(string id)
    {
        var filter = Builders<OutboxEntry>.Filter.Eq(e => e.Id, id);
        var update = Builders<OutboxEntry>.Update.Set(e => e.PublishedAt, System.DateTime.UtcNow);
        return _col.UpdateOneAsync(filter, update);
    }

    public Task IncrementAttemptAsync(string id, string? lastError = null)
    {
        var filter = Builders<OutboxEntry>.Filter.Eq(e => e.Id, id);
        var update = Builders<OutboxEntry>.Update.Inc(e => e.Attempts, 1).Set(e => e.LastError, lastError);
        return _col.UpdateOneAsync(filter, update);
    }
}
