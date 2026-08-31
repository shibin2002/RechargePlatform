using Microsoft.AspNetCore.Mvc;
using RechargeApi.Services;
using RechargePlatform.Common.DTOs;

namespace RechargeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RechargeController : ControllerBase
{
    private readonly IRechargeService _rechargeService;
    private readonly ILogger<RechargeController> _logger;

    public RechargeController(IRechargeService rechargeService, ILogger<RechargeController> logger)
    {
        _rechargeService = rechargeService;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/recharge
    /// Initiates a mobile recharge transaction with concurrency-safe duplicate protection
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Recharge([FromBody] RechargeRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Validation failed", ModelState));
        }

        try
        {
            var result = await _rechargeService.ProcessRechargeAsync(request, cancellationToken);

            if (result.IsDuplicate)
            {
                // Returns existing transaction status cleanly with 200 OK
                return Ok(ApiResponse<RechargeResponseDto>.Ok(result, "Duplicate transaction detected; returning existing record state."));
            }

            return Ok(ApiResponse<RechargeResponseDto>.Ok(result, "Recharge processed successfully."));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error during recharge: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing recharge for TransactionId={TransactionId}", request.TransactionId);
            return StatusCode(500, ApiResponse<object>.Fail("Internal database or service error occurred: " + ex.Message));
        }
    }

    /// <summary>
    /// GET /api/recharge/{transactionId}
    /// Returns current transaction state and full status transition history
    /// </summary>
    [HttpGet("{transactionId}")]
    public async Task<IActionResult> GetTransaction(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return BadRequest(ApiResponse<object>.Fail("TransactionId is required."));

        var result = await _rechargeService.GetTransactionAsync(transactionId);
        if (result == null)
        {
            return NotFound(ApiResponse<object>.Fail($"Transaction '{transactionId}' was not found."));
        }

        return Ok(ApiResponse<RechargeResponseDto>.Ok(result));
    }

    /// <summary>
    /// GET /api/recharge
    /// List and filter transactions (by status, operator, date range, mobile number)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTransactions([FromQuery] TransactionFilterDto filter)
    {
        var result = await _rechargeService.GetFilteredTransactionsAsync(filter);
        return Ok(ApiResponse<PagedResult<RechargeResponseDto>>.Ok(result));
    }

    /// <summary>
    /// POST /api/recharge/{transactionId}/reconcile
    /// Explicitly triggers status enquiry reconciliation with the telecom provider
    /// </summary>
    [HttpPost("{transactionId}/reconcile")]
    public async Task<IActionResult> ReconcileTransaction(string transactionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return BadRequest(ApiResponse<object>.Fail("TransactionId is required."));

        var result = await _rechargeService.ReconcileTransactionAsync(transactionId, cancellationToken);
        return Ok(ApiResponse<ReconcileResponseDto>.Ok(result, result.Message));
    }
}
