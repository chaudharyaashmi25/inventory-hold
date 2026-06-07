namespace InventoryHold.Contracts.Models;

/// <summary>
/// Standard API error model returned to clients.
/// </summary>
public sealed class ApiError
{
    /// <summary>
    /// Application-specific error code.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Optional field-level validation details.
    /// </summary>
    public IDictionary<string, string[]>? Details { get; init; }
}
