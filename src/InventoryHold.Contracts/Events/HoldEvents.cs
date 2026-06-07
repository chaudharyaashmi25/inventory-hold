using System;

namespace InventoryHold.Contracts.Events;

/// <summary>
/// Events published by the InventoryHold service related to hold lifecycle.
/// </summary>
public static class HoldEvents
{
}

public record HoldCreatedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string HoldId,
    string Sku,
    int Quantity,
    string? Location,
    string? Owner,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    int ProductAvailableQuantity,
    int ProductTotalQuantity,
    string? CorrelationId = null
);

public record HoldReleasedEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string HoldId,
    string Sku,
    int Quantity,
    string? Location,
    string? Owner,
    DateTimeOffset ReleasedAt,
    int ProductAvailableQuantity,
    int ProductTotalQuantity,
    string? Reason = null,
    string? CorrelationId = null
);

public record HoldExpiredEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string HoldId,
    string Sku,
    int Quantity,
    string? Location,
    DateTimeOffset ExpiredAt,
    int ProductAvailableQuantity,
    int ProductTotalQuantity,
    string? Reason = null,
    string? CorrelationId = null
);
