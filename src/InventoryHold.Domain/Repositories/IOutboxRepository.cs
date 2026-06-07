using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryHold.Domain.Repositories
{
    public interface IOutboxRepository
    {
        Task CreateAsync(InventoryHold.Domain.Models.OutboxEntry entry);
        Task<IEnumerable<InventoryHold.Domain.Models.OutboxEntry>> GetUnpublishedAsync(int limit);
        Task MarkPublishedAsync(string id);
        Task IncrementAttemptAsync(string id, string? lastError = null);
    }
}
