using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Services;
using InventoryHold.Infrastructure.Repositories;
using InventoryHold.Domain.Models;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

// Development CORS policy - allow all origins for local/dev usage
builder.Services.AddCors(options =>
{
	options.AddPolicy("LocalDevPolicy", policy =>
	{
		policy.AllowAnyOrigin()
			  .AllowAnyHeader()
			  .AllowAnyMethod();
	});
});

// Mongo configuration (env or appsettings)
var mongoConn = builder.Configuration["Mongo:ConnectionString"] ?? Environment.GetEnvironmentVariable("MONGO_CONN") ?? "mongodb://localhost:27017";
var mongoDbName = builder.Configuration["Mongo:Database"] ?? "inventory";

// Register Mongo client and database
var mongoClient = new MongoClient(mongoConn);
builder.Services.AddSingleton<IMongoClient>(mongoClient);
builder.Services.AddSingleton(sp => mongoClient.GetDatabase(mongoDbName));

// Register repositories and services
builder.Services.AddScoped<IInventoryRepository, MongoInventoryRepository>();
builder.Services.AddScoped<IHoldRepository, MongoHoldRepository>();
// Cache registration: prefer Redis if configured, otherwise in-memory fallback in Development
var redisConn = builder.Configuration["Redis:ConnectionString"] ?? Environment.GetEnvironmentVariable("REDIS_CONN");
if (!string.IsNullOrWhiteSpace(redisConn))
{
	builder.Services.AddSingleton<InventoryHold.Domain.Caching.ICache>(sp => new InventoryHold.Infrastructure.Caching.RedisCache(redisConn));
}
else if (builder.Environment.IsDevelopment())
{
	builder.Services.AddSingleton<InventoryHold.Domain.Caching.ICache, InventoryHold.Infrastructure.Caching.InMemoryCache>();
}
else
{
	throw new InvalidOperationException("Redis is not configured in a non-Development environment. Please set Redis:ConnectionString or REDIS_CONN.");
}

builder.Services.AddScoped<IHoldService, HoldService>();

// Outbox repository + dispatcher
builder.Services.AddScoped<InventoryHold.Domain.Repositories.IOutboxRepository, InventoryHold.Infrastructure.Outbox.MongoOutboxRepository>();
builder.Services.AddHostedService<InventoryHold.Infrastructure.HostedServices.OutboxDispatcher>();

// RabbitMQ bus registration
var rabbitConn = builder.Configuration["RabbitMQ:ConnectionString"] ?? Environment.GetEnvironmentVariable("RABBITMQ_CONN") ?? "amqp://guest:guest@localhost:5672/";
var rabbitExchange = builder.Configuration["RabbitMQ:Exchange"] ?? "inventory.holds";
builder.Services.AddSingleton<InventoryHold.Domain.Messaging.IMessageBus>(sp => new InventoryHold.Infrastructure.MessageBus.RabbitMqBus(rabbitConn, rabbitExchange));
// Register expiration reconciler (it will take ICache via DI)
builder.Services.AddHostedService<InventoryHold.Infrastructure.HostedServices.HoldExpirationReconciler>();

var app = builder.Build();

// Warn when Redis is not configured and in-memory cache will be used (only possible in Development)
if (string.IsNullOrWhiteSpace(redisConn) && app.Environment.IsDevelopment())
{
	app.Logger.LogWarning("Redis not configured; falling back to local InMemoryCache for development. Set Redis:ConnectionString or REDIS_CONN to enable Redis.");
}

// Ensure indexes and seed sample products on startup
using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
	var products = db.GetCollection<ProductInventory>("products");

	// Ensure a simple index on sku
	var idxKeys = Builders<ProductInventory>.IndexKeys.Ascending(p => p.Sku);
	products.Indexes.CreateOne(new CreateIndexModel<ProductInventory>(idxKeys, new CreateIndexOptions { Unique = true }));

	// Ensure indexes for holds and outbox
	var holds = db.GetCollection<InventoryHold.Domain.Models.Hold>("holds");
	var idxHolds = Builders<InventoryHold.Domain.Models.Hold>.IndexKeys.Ascending(h => h.ExpiresAt).Ascending(h => h.Status);
	holds.Indexes.CreateOne(new CreateIndexModel<InventoryHold.Domain.Models.Hold>(idxHolds));

	var outbox = db.GetCollection<InventoryHold.Domain.Models.OutboxEntry>("outbox");
	var idxOutbox = Builders<InventoryHold.Domain.Models.OutboxEntry>.IndexKeys.Ascending(o => o.PublishedAt).Ascending(o => o.CreatedAt);
	outbox.Indexes.CreateOne(new CreateIndexModel<InventoryHold.Domain.Models.OutboxEntry>(idxOutbox));

	// Seed 5 products if collection empty
	var count = products.CountDocuments(Builders<ProductInventory>.Filter.Empty);
	if (count == 0)
	{
		var seed = new List<ProductInventory>
		{
			new ProductInventory { Sku = "SKU-1", Name = "Widget A", TotalQuantity = 100, AvailableQuantity = 100, ReservedQuantity = 0 },
			new ProductInventory { Sku = "SKU-2", Name = "Widget B", TotalQuantity = 50, AvailableQuantity = 50, ReservedQuantity = 0 },
			new ProductInventory { Sku = "SKU-3", Name = "Gadget C", TotalQuantity = 200, AvailableQuantity = 200, ReservedQuantity = 0 },
			new ProductInventory { Sku = "SKU-4", Name = "Gizmo D", TotalQuantity = 10, AvailableQuantity = 10, ReservedQuantity = 0 },
			new ProductInventory { Sku = "SKU-5", Name = "Thingamajig E", TotalQuantity = 5, AvailableQuantity = 5, ReservedQuantity = 0 }
		};
		products.InsertMany(seed);
	}
}

app.UseRouting();

// Enable CORS for the configured policy
app.UseCors("LocalDevPolicy");

app.MapControllers();
app.Run();
