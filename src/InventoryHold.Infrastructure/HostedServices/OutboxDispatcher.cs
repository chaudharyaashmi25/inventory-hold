using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Models;

using Microsoft.Extensions.DependencyInjection;

namespace InventoryHold.Infrastructure.HostedServices;

public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly InventoryHold.Domain.Messaging.IMessageBus _bus;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(IServiceScopeFactory scopeFactory, InventoryHold.Domain.Messaging.IMessageBus bus, ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                    var items = await outbox.GetUnpublishedAsync(50);
                    foreach (var item in items)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        try
                        {
                            // Deserialize to get event type (we publish raw payload as JSON)
                            await _bus.PublishAsync(item.EventType, JsonSerializer.Deserialize<object>(item.Payload) ?? item.Payload);
                            await outbox.MarkPublishedAsync(item.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to publish outbox entry {Id}", item.Id);
                            await outbox.IncrementAttemptAsync(item.Id, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox dispatcher error");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
