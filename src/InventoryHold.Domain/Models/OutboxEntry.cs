using System;

namespace InventoryHold.Domain.Models
{
    public class OutboxEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
        public int Attempts { get; set; }
        public string? LastError { get; set; }
    }
}
