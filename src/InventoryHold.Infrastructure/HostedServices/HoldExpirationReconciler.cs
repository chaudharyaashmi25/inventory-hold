using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using InventoryHold.Domain.Models;
using InventoryHold.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryHold.Infrastructure.HostedServices;

/// <summary>
/// Background service that periodically scans for expired holds and reconciles them.
/// </summary>
public class HoldExpirationReconciler : BackgroundService
{
    private readonly IMongoDatabase _database;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpirationReconciler> _logger;

    public HoldExpirationReconciler(IMongoDatabase database, IServiceScopeFactory scopeFactory, ILogger<HoldExpirationReconciler> logger)
    {
        _database = database;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var holdsCollection = _database.GetCollection<Hold>("holds");

        const int batchSize = 50;
        var delay = TimeSpan.FromSeconds(15);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                var filter = Builders<Hold>.Filter.And(
                    Builders<Hold>.Filter.Eq(h => h.Status, HoldStatus.Active),
                    Builders<Hold>.Filter.Lte(h => h.ExpiresAt, now)
                );

                // Find a batch of expired active holds
                var expiredHolds = await holdsCollection.Find(filter).Limit(batchSize).ToListAsync(stoppingToken);

                if (expiredHolds.Count == 0)
                {
                    await Task.Delay(delay, stoppingToken);
                    continue;
                }

                foreach (var hold in expiredHolds)
                {
                    if (stoppingToken.IsCancellationRequested) break;

                    try
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var holdService = scope.ServiceProvider.GetRequiredService<IHoldService>();
                            var success = await holdService.ExpireHoldAsync(hold.Id);
                            if (success)
                            {
                                _logger.LogInformation("Successfully reconciled expired hold {HoldId}", hold.Id);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to reconcile expired hold {HoldId} (maybe already handled or failed)", hold.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing expired hold {HoldId}", hold.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hold expiration reconciliation");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }
}
