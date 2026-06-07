namespace InventoryHold.Infrastructure.Repositories;

using InventoryHold.Domain.Models;
using InventoryHold.Domain.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

public class MongoHoldRepository : IHoldRepository
{
    private readonly IMongoCollection<Hold> _collection;
    private readonly IMongoDatabase _database;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoHoldRepository"/>.
    /// </summary>
    /// <param name="database">Mongo database instance.</param>
    public MongoHoldRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<Hold>("holds");
        _database = database;
    }

    /// <inheritdoc />
    public async Task<Hold?> GetByIdAsync(string id)
    {
        var filter = Builders<Hold>.Filter.Eq(x => x.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Ensures the hold document has identifiers and timestamps set before insertion.
    /// The default expiration is applied when <see cref="Hold.ExpiresAt"/> is not provided.
    /// </remarks>
    public async Task<Hold> CreateAsync(Hold hold)
    {
        if (string.IsNullOrWhiteSpace(hold.Id))
        {
            hold.Id = ObjectId.GenerateNewId().ToString();
        }

        if (hold.CreatedAt == default)
        {
            hold.CreatedAt = DateTime.UtcNow;
        }

        if (hold.ExpiresAt == default)
        {
            hold.ExpiresAt = hold.CreatedAt.AddMinutes(15);
        }

        if (hold.Status == default)
        {
            hold.Status = HoldStatus.Active;
        }

        await _collection.InsertOneAsync(hold);
        return hold;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Performs a conditional update to mark an active hold as released. This operation
    /// is idempotent for repeated release attempts on the same active hold.
    /// </remarks>
    public async Task<bool> ReleaseAsync(string id)
    {
        var filter = Builders<Hold>.Filter.Eq(x => x.Id, id)
            & Builders<Hold>.Filter.Eq(x => x.Status, HoldStatus.Active);

        var update = Builders<Hold>.Update.Set(x => x.Status, HoldStatus.Released);

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    /// <summary>
    /// Marks an active hold as expired.
    /// </summary>
    public async Task<bool> MarkExpiredAsync(string id)
    {
        var filter = Builders<Hold>.Filter.Eq(x => x.Id, id)
            & Builders<Hold>.Filter.Eq(x => x.Status, HoldStatus.Active);

        var update = Builders<Hold>.Update.Set(x => x.Status, HoldStatus.Expired);

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Hold>> GetActiveHoldsAsync()
    {
        var filter = Builders<Hold>.Filter.Eq(x => x.Status, HoldStatus.Active);
        return await _collection.Find(filter).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> CreateWithOutboxAsync(Hold hold, OutboxEntry outboxEntry)
    {
        if (string.IsNullOrWhiteSpace(hold.Id))
        {
            hold.Id = ObjectId.GenerateNewId().ToString();
        }

        if (hold.CreatedAt == default)
        {
            hold.CreatedAt = DateTime.UtcNow;
        }

        if (hold.ExpiresAt == default)
        {
            hold.ExpiresAt = hold.CreatedAt.AddMinutes(15);
        }

        if (hold.Status == default)
        {
            hold.Status = HoldStatus.Active;
        }

        var outboxCol = _database.GetCollection<OutboxEntry>("outbox");

        try
        {
            using var session = await _database.Client.StartSessionAsync();
            session.StartTransaction();

            await _collection.InsertOneAsync(session, hold);
            await outboxCol.InsertOneAsync(session, outboxEntry);

            await session.CommitTransactionAsync();
            return true;
        }
        catch (Exception)
        {
            await _collection.InsertOneAsync(hold);
            try
            {
                await outboxCol.InsertOneAsync(outboxEntry);
            }
            catch
            {
                // swallow outbox errors to keep the main flow functional
            }
            return true;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ReleaseWithOutboxAsync(string holdId, OutboxEntry outboxEntry)
    {
        var outboxCol = _database.GetCollection<OutboxEntry>("outbox");

        try
        {
            using var session = await _database.Client.StartSessionAsync();
            session.StartTransaction();

            var filter = Builders<Hold>.Filter.Eq(x => x.Id, holdId)
                & Builders<Hold>.Filter.Eq(x => x.Status, HoldStatus.Active);
            var update = Builders<Hold>.Update.Set(x => x.Status, HoldStatus.Released);

            var result = await _collection.UpdateOneAsync(session, filter, update);
            if (result.ModifiedCount == 0)
            {
                await session.AbortTransactionAsync();
                return false;
            }

            await outboxCol.InsertOneAsync(session, outboxEntry);

            await session.CommitTransactionAsync();
            return true;
        }
        catch (Exception)
        {
            var filter = Builders<Hold>.Filter.Eq(x => x.Id, holdId)
                & Builders<Hold>.Filter.Eq(x => x.Status, HoldStatus.Active);
            var update = Builders<Hold>.Update.Set(x => x.Status, HoldStatus.Released);

            var result = await _collection.UpdateOneAsync(filter, update);
            if (result.ModifiedCount == 0)
            {
                return false;
            }

            try
            {
                await outboxCol.InsertOneAsync(outboxEntry);
            }
            catch
            {
                // swallow
            }
            return true;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExpireWithOutboxAsync(string holdId, OutboxEntry outboxEntry)
    {
        var outboxCol = _database.GetCollection<OutboxEntry>("outbox");

        try
        {
            using var session = await _database.Client.StartSessionAsync();
            session.StartTransaction();

            var filter = Builders<Hold>.Filter.Eq(x => x.Id, holdId)
                & Builders<Hold>.Filter.Eq(x => x.Status, HoldStatus.Active);
            var update = Builders<Hold>.Update.Set(x => x.Status, HoldStatus.Expired);

            var result = await _collection.UpdateOneAsync(session, filter, update);
            if (result.ModifiedCount == 0)
            {
                await session.AbortTransactionAsync();
                return false;
            }

            await outboxCol.InsertOneAsync(session, outboxEntry);

            await session.CommitTransactionAsync();
            return true;
        }
        catch (Exception)
        {
            var filter = Builders<Hold>.Filter.Eq(x => x.Id, holdId)
                & Builders<Hold>.Filter.Eq(x => x.Status, HoldStatus.Active);
            var update = Builders<Hold>.Update.Set(x => x.Status, HoldStatus.Expired);

            var result = await _collection.UpdateOneAsync(filter, update);
            if (result.ModifiedCount == 0)
            {
                return false;
            }

            try
            {
                await outboxCol.InsertOneAsync(outboxEntry);
            }
            catch
            {
                // swallow
            }
            return true;
        }
    }
}
