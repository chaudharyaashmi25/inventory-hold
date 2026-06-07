using Microsoft.AspNetCore.Mvc;
using InventoryHold.Contracts.Models;
using MongoDB.Bson;
using InventoryHold.Domain.Services;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Models;

namespace InventoryHold.WebApi.Controllers;

[ApiController]
[Route("api/holds")]
/// <summary>
/// Controller that manages hold lifecycle operations: create, read and release holds.
/// </summary>
public class HoldController : ControllerBase
{
    private readonly IHoldService _service;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly InventoryHold.Domain.Caching.ICache _cache;

    /// <summary>
    /// Initializes a new instance of <see cref="HoldController"/>.
    /// </summary>
    public HoldController(IHoldService service, IInventoryRepository inventoryRepo, InventoryHold.Domain.Caching.ICache cache)
    {
        _service = service;
        _inventoryRepo = inventoryRepo;
        _cache = cache;
    }

    /// <summary>
    /// Processes a new hold request. Validates input, checks SKU existence,
    /// attempts to reserve inventory atomically and persists a hold document.
    /// Returns 201 Created with hold details on success.
    /// </summary>
    /// <param name="req">Hold creation request payload.</param>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] HoldCreateRequest req)
    {
        if (req == null) return BadRequest();
        if (string.IsNullOrWhiteSpace(req.Sku) || req.Quantity <= 0) return BadRequest(new ApiError { Code = "invalid_request", Message = "Sku and positive Quantity are required." });

        // Ensure product exists
        var product = await _inventoryRepo.GetBySkuAsync(req.Sku);
        if (product is null) return NotFound(new ApiError { Code = "sku_not_found", Message = "Product SKU not found." });

        var hold = await _service.CreateHoldAsync(req.Sku, req.Quantity, req.Location, req.Owner, req.HoldSeconds);
        if (hold is null) return Conflict(new ApiError { Code = "insufficient_stock", Message = "Insufficient available stock for SKU." });

        var resp = new HoldResponse
        {
            HoldId = hold.Id,
            Sku = hold.Sku,
            Quantity = hold.Quantity,
            Location = hold.Location,
            Owner = hold.Owner,
            Status = hold.Status.ToString(),
            CreatedAt = hold.CreatedAt,
            ExpiresAt = hold.ExpiresAt
        };

        return CreatedAtAction(nameof(Get), new { id = resp.HoldId }, resp);
    }

    /// <summary>
    /// Retrieves a hold by id. Returns 200 OK with hold details or 404 if not found.
    /// </summary>
    /// <param name="id">Hold identifier.</param>
    [HttpGet("{id}")]
    public async Task<ActionResult<HoldResponse>> Get(string id)
    {
        if (!ObjectId.TryParse(id, out _)) return BadRequest(new ApiError { Code = "invalid_id", Message = "Invalid hold id format." });
        // Try cache first (short TTL)
        var cached = await _cache.GetAsync<Domain.Models.Hold>($"hold:{id}");
        Domain.Models.Hold? res = null;
        if (cached is not null)
        {
            res = cached;
        }
        else
        {
            res = await _service.GetHoldAsync(id);
            if (res is not null)
            {
                // cache hold for a short period
                try { await _cache.SetAsync($"hold:{id}", res, TimeSpan.FromSeconds(10)); } catch { }
            }
        }
        if (res is null) return NotFound();

        var resp = new HoldResponse
        {
            HoldId = res.Id,
            Sku = res.Sku,
            Quantity = res.Quantity,
            Location = res.Location,
            Owner = res.Owner,
            Status = res.Status.ToString(),
            CreatedAt = res.CreatedAt,
            ExpiresAt = res.ExpiresAt
        };

        return Ok(resp);
    }

    /// <summary>
    /// Releases an active hold and restores inventory. Returns 204 No Content on success.
    /// </summary>
    /// <param name="id">Hold identifier.</param>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!ObjectId.TryParse(id, out _)) return BadRequest(new ApiError { Code = "invalid_id", Message = "Invalid hold id format." });
        var existing = await _service.GetHoldAsync(id);
        if (existing is null) return NotFound();

        var ok = await _service.ReleaseHoldAsync(id);
        if (!ok) return Conflict(new ApiError { Code = "release_failed", Message = "Unable to release hold." });

        return NoContent();
    }

    /// <summary>
    /// Retrieves all active holds.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HoldResponse>>> GetActiveHolds()
    {
        var holds = await _service.GetActiveHoldsAsync();
        var resp = holds.Select(h => new HoldResponse
        {
            HoldId = h.Id,
            Sku = h.Sku,
            Quantity = h.Quantity,
            Location = h.Location,
            Owner = h.Owner,
            Status = h.Status.ToString(),
            CreatedAt = h.CreatedAt,
            ExpiresAt = h.ExpiresAt
        });
        return Ok(resp);
    }
}
