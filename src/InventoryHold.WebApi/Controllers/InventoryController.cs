using Microsoft.AspNetCore.Mvc;
using InventoryHold.Domain.Repositories;
using InventoryHold.Contracts.Models;

namespace InventoryHold.WebApi.Controllers;

[ApiController]
[Route("api/inventory")]
/// <summary>
/// Controller exposing inventory read endpoints.
/// </summary>
public class InventoryController : ControllerBase
{
    private readonly IInventoryRepository _repo;
    private readonly InventoryHold.Domain.Caching.ICache _cache;

    /// <summary>
    /// Creates a new instance of <see cref="InventoryController"/>.
    /// </summary>
    public InventoryController(IInventoryRepository repo, InventoryHold.Domain.Caching.ICache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    /// <summary>
    /// Returns current inventory levels. Supports optional filters for SKU and location,
    /// and an <c>availableOnly</c> flag to return only in-stock items.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryProductDto>>> Get([FromQuery] string? sku, [FromQuery] string? location, [FromQuery] bool availableOnly = false)
    {
		// Dynamic cache key depending on filters
		var cacheKey = $"inventory:list:sku={sku ?? ""}:loc={location ?? ""}:avail={availableOnly}";
		var cached = await _cache.GetAsync<IEnumerable<InventoryHold.Domain.Models.ProductInventory>>(cacheKey);
		IEnumerable<InventoryHold.Domain.Models.ProductInventory> items;

		if (cached is not null)
		{
			items = cached;
		}
		else
		{
			items = await _repo.ListAsync(sku, location, availableOnly);
			try
			{
				await _cache.SetAsync(cacheKey, items, TimeSpan.FromSeconds(30));
				
				// Track this cache key against each SKU present so we can invalidate precisely
				foreach (var p in items)
				{
					try { await _cache.AddKeyToSetAsync($"inventory:keys:sku:{p.Sku}", cacheKey); } catch { }
				}

				// If we filtered by a specific SKU, associate cacheKey directly as well (handles empty results)
				if (!string.IsNullOrWhiteSpace(sku))
				{
					try { await _cache.AddKeyToSetAsync($"inventory:keys:sku:{sku}", cacheKey); } catch { }
				}
			}
			catch { }
		}
        var dto = items.Select(p => new InventoryProductDto
        {
            Sku = p.Sku,
            Name = p.Name,
            Location = p.Location,
            TotalQuantity = p.TotalQuantity,
            AvailableQuantity = p.AvailableQuantity,
            ReservedQuantity = p.ReservedQuantity
        });

        return Ok(dto);
    }
}
