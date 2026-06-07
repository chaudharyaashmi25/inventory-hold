namespace InventoryHold.Infrastructure.Repositories;

using InventoryHold.Domain.Models;
using InventoryHold.Domain.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

public class MongoInventoryRepository : IInventoryRepository
{
    private readonly IMongoCollection<ProductInventory> _collection;

    /// <summary>
    /// Creates a new instance of the repository using the provided Mongo database.
    /// </summary>
    /// <param name="database">Mongo database instance.</param>
    public MongoInventoryRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<ProductInventory>("products");
    }

    /// <inheritdoc />
    public async Task<ProductInventory?> GetBySkuAsync(string sku)
    {
        var filter = Builders<ProductInventory>.Filter.Eq(x => x.Sku, sku);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductInventory>> ListAsync(string? sku = null, string? location = null, bool availableOnly = false)
    {
        var filter = Builders<ProductInventory>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(sku))
        {
            filter &= Builders<ProductInventory>.Filter.Eq(x => x.Sku, sku);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            filter &= Builders<ProductInventory>.Filter.Eq(x => x.Location, location);
        }

        if (availableOnly)
        {
            filter &= Builders<ProductInventory>.Filter.Gt(x => x.AvailableQuantity, 0);
        }

        return await _collection.Find(filter).ToListAsync();
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method performs a conditional <c>findOneAndUpdate</c> on the product document
    /// so that the reservation is atomic on the product document. If the product's
    /// <c>AvailableQuantity</c> is less than the requested quantity the call returns <c>null</c>.
    /// </remarks>
    public async Task<ProductInventory?> TryReserveAsync(string sku, int quantity, string? location = null)
    {
        var filter = Builders<ProductInventory>.Filter.Eq(x => x.Sku, sku)
            & Builders<ProductInventory>.Filter.Gte(x => x.AvailableQuantity, quantity);

        if (!string.IsNullOrWhiteSpace(location))
        {
            filter &= Builders<ProductInventory>.Filter.Eq(x => x.Location, location);
        }

        var update = Builders<ProductInventory>.Update
            .Inc(x => x.AvailableQuantity, -quantity)
            .Inc(x => x.ReservedQuantity, quantity)
            .Inc(x => x.Version, 1);

        var options = new FindOneAndUpdateOptions<ProductInventory>
        {
            ReturnDocument = ReturnDocument.After
        };

        return await _collection.FindOneAndUpdateAsync(filter, update, options);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Releases reserved quantity by increasing <c>AvailableQuantity</c> and decreasing <c>ReservedQuantity</c>.
    /// The update uses a conditional filter to ensure reserved quantity is sufficient.
    /// </remarks>
    public async Task<bool> ReleaseReservedQuantityAsync(string sku, int quantity, string? location = null)
    {
        var filter = Builders<ProductInventory>.Filter.Eq(x => x.Sku, sku)
            & Builders<ProductInventory>.Filter.Gte(x => x.ReservedQuantity, quantity);

        if (!string.IsNullOrWhiteSpace(location))
        {
            filter &= Builders<ProductInventory>.Filter.Eq(x => x.Location, location);
        }

        var update = Builders<ProductInventory>.Update
            .Inc(x => x.AvailableQuantity, quantity)
            .Inc(x => x.ReservedQuantity, -quantity)
            .Inc(x => x.Version, 1);

        var result = await _collection.UpdateOneAsync(filter, update);
        return result.ModifiedCount > 0;
    }
}
