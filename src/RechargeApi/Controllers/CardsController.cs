using Microsoft.AspNetCore.Mvc;
using RechargeApi.Services;
using RechargePlatform.Common.DTOs;

namespace RechargeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CardsController : ControllerBase
{
    private readonly ICardImportService _cardImportService;
    private readonly ILogger<CardsController> _logger;

    public CardsController(ICardImportService cardImportService, ILogger<CardsController> logger)
    {
        _cardImportService = cardImportService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/cards/import
    /// High-throughput CSV card batch import via SqlBulkCopy and set-based merge
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportCards([FromForm] IFormFile file, [FromForm] string? importedBy, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("No CSV file provided or file is empty."));
        }

        try
        {
            var uploader = string.IsNullOrWhiteSpace(importedBy) ? "POS_USER" : importedBy;
            var result = await _cardImportService.ImportCardsCsvAsync(file, uploader, cancellationToken);
            return Ok(ApiResponse<CardImportResultDto>.Ok(result, "CSV file processed successfully."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import card CSV file: {FileName}", file.FileName);
            return StatusCode(500, ApiResponse<object>.Fail("Failed to process card CSV import: " + ex.Message));
        }
    }

    /// <summary>
    /// POST /api/cards/reserve
    /// Atomic conditional card reservation (prevents race conditions)
    /// </summary>
    [HttpPost("reserve")]
    public async Task<IActionResult> ReserveCard([FromBody] CardReservationRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Validation failed", ModelState));
        }

        try
        {
            var result = await _cardImportService.ReserveCardAsync(request);
            if (result == null)
            {
                // Concurrency loss or already claimed
                return Conflict(ApiResponse<object>.Fail($"Card '{request.CardNumber}' is not available or has already been claimed."));
            }

            return Ok(ApiResponse<CardReservationResponseDto>.Ok(result, "Card reserved successfully."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving card {CardNumber}", request.CardNumber);
            return StatusCode(500, ApiResponse<object>.Fail("Error during card reservation: " + ex.Message));
        }
    }

    /// <summary>
    /// GET /api/cards/inventory
    /// Returns card stock counts grouped by Operator and Denomination
    /// </summary>
    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventory()
    {
        var inventory = await _cardImportService.GetInventorySummaryAsync();
        return Ok(ApiResponse<List<CardInventoryDto>>.Ok(inventory));
    }

    /// <summary>
    /// GET /api/cards/batches
    /// Returns card import batch history
    /// </summary>
    [HttpGet("batches")]
    public async Task<IActionResult> GetBatches()
    {
        var batches = await _cardImportService.GetImportBatchesAsync();
        return Ok(ApiResponse<List<CardBatchSummaryDto>>.Ok(batches));
    }
}
