namespace InventoryHold.Contracts.Models;

public record HoldDto(string Id, string Sku, int Quantity, string Location);
