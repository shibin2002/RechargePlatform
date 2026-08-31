using Microsoft.AspNetCore.Mvc;
using MockProviderApi.Services;
using RechargePlatform.Common.Constants;
using RechargePlatform.Common.DTOs;

namespace MockProviderApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProviderController : ControllerBase
{
    private readonly IProviderStateStore _stateStore;
    private readonly ILogger<ProviderController> _logger;

    public ProviderController(IProviderStateStore stateStore, ILogger<ProviderController> logger)
    {
        _stateStore = stateStore;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/provider/recharge
    /// Simulates provider behavior based on amount rules:
    /// 100 -> Immediate SUCCESS
    /// 200 -> Immediate FAILED
    /// 300 -> Delay 15s before responding SUCCESS (tests caller's 10s client timeout)
    /// 400 -> Internally records SUCCESS but abruptly drops connection (tests PENDING + reconciliation)
    /// 500 -> Immediate HTTP 500 Internal Server Error
    /// any other amount -> Immediate SUCCESS
    /// </summary>
    [HttpPost("recharge")]
    public async Task<IActionResult> Recharge([FromBody] ProviderRechargeRequestDto request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received provider recharge request: ReferenceId={ReferenceId}, Mobile={Mobile}, Operator={Operator}, Amount={Amount}",
            request.ReferenceId, request.MobileNumber, request.Operator, request.Amount);

        if (string.IsNullOrWhiteSpace(request.ReferenceId))
        {
            return BadRequest(new { message = "ReferenceId is required." });
        }

        var providerRef = $"PROV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

        // RULE: 100 -> Immediate SUCCESS
        if (request.Amount == 100m)
        {
            _stateStore.RecordTransaction(request.ReferenceId, providerRef, TransactionStatuses.Success, request.Amount, request.MobileNumber, request.Operator);
            _logger.LogInformation("[Rule 100] Returning immediate SUCCESS for ReferenceId={ReferenceId}, ProviderRef={ProviderRef}", request.ReferenceId, providerRef);
            return Ok(new ProviderRechargeResponseDto
            {
                ReferenceId = request.ReferenceId,
                ProviderReference = providerRef,
                Status = TransactionStatuses.Success,
                Timestamp = DateTime.UtcNow
            });
        }

        // RULE: 200 -> Immediate FAILED
        if (request.Amount == 200m)
        {
            const string failReason = "Subscriber SIM balance insufficient or plan expired.";
            _stateStore.RecordTransaction(request.ReferenceId, providerRef, TransactionStatuses.Failed, request.Amount, request.MobileNumber, request.Operator, failReason);
            _logger.LogInformation("[Rule 200] Returning immediate FAILED for ReferenceId={ReferenceId}, ProviderRef={ProviderRef}", request.ReferenceId, providerRef);
            return Ok(new ProviderRechargeResponseDto
            {
                ReferenceId = request.ReferenceId,
                ProviderReference = providerRef,
                Status = TransactionStatuses.Failed,
                ErrorMessage = failReason,
                Timestamp = DateTime.UtcNow
            });
        }

        // RULE: 300 -> Delay 15 seconds before responding SUCCESS (tests caller 10s timeout)
        if (request.Amount == 300m)
        {
            _logger.LogWarning("[Rule 300] Simulating 15-second provider latency for ReferenceId={ReferenceId}...", request.ReferenceId);
            try
            {
                await Task.Delay(15000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("[Rule 300] Client disconnected/cancelled while waiting for 15s delay. ReferenceId={ReferenceId}", request.ReferenceId);
            }

            _stateStore.RecordTransaction(request.ReferenceId, providerRef, TransactionStatuses.Success, request.Amount, request.MobileNumber, request.Operator);
            _logger.LogInformation("[Rule 300] 15s delay finished. Recorded SUCCESS internally for ReferenceId={ReferenceId}", request.ReferenceId);
            
            return Ok(new ProviderRechargeResponseDto
            {
                ReferenceId = request.ReferenceId,
                ProviderReference = providerRef,
                Status = TransactionStatuses.Success,
                Timestamp = DateTime.UtcNow
            });
        }

        // RULE: 400 -> Record internally that it succeeded, but deliberately abort/drop connection
        if (request.Amount == 400m)
        {
            _stateStore.RecordTransaction(request.ReferenceId, providerRef, TransactionStatuses.Success, request.Amount, request.MobileNumber, request.Operator);
            _logger.LogWarning("[Rule 400] Recorded SUCCESS internally, but deliberately aborting HTTP connection to simulate network drop for ReferenceId={ReferenceId}", request.ReferenceId);
            
            // Abort the HTTP connection abruptly
            HttpContext.Abort();
            return new EmptyResult();
        }

        // RULE: 500 -> Immediate HTTP 500 Server Error
        if (request.Amount == 500m)
        {
            _logger.LogError("[Rule 500] Simulating provider 500 Internal Server Error for ReferenceId={ReferenceId}", request.ReferenceId);
            return StatusCode(500, new
            {
                referenceId = request.ReferenceId,
                errorCode = "PROVIDER_GATEWAY_DOWN",
                message = "Telecom core switch is currently unreachable or undergoing maintenance."
            });
        }

        // DEFAULT: Any other amount (e.g. 299, 199, 499) -> Immediate SUCCESS
        _stateStore.RecordTransaction(request.ReferenceId, providerRef, TransactionStatuses.Success, request.Amount, request.MobileNumber, request.Operator);
        _logger.LogInformation("[Default Rule] Returning immediate SUCCESS for ReferenceId={ReferenceId}, ProviderRef={ProviderRef}", request.ReferenceId, providerRef);
        
        return Ok(new ProviderRechargeResponseDto
        {
            ReferenceId = request.ReferenceId,
            ProviderReference = providerRef,
            Status = TransactionStatuses.Success,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// GET /api/provider/status/{referenceId}
    /// Status enquiry source of truth used during reconciliation
    /// </summary>
    [HttpGet("status/{referenceId}")]
    public IActionResult GetStatus(string referenceId)
    {
        _logger.LogInformation("Status enquiry received for ReferenceId={ReferenceId}", referenceId);
        
        var record = _stateStore.GetStatus(referenceId);
        if (record == null)
        {
            _logger.LogWarning("Status enquiry: ReferenceId={ReferenceId} not found in provider database", referenceId);
            return NotFound(new
            {
                referenceId,
                status = "NOT_FOUND",
                message = "Transaction reference not found in telecom provider records.",
                timestamp = DateTime.UtcNow
            });
        }

        return Ok(record);
    }

    /// <summary>
    /// GET /api/provider/all
    /// Diagnostic endpoint to inspect all provider records
    /// </summary>
    [HttpGet("all")]
    public IActionResult GetAll()
    {
        return Ok(_stateStore.GetAll());
    }
}
