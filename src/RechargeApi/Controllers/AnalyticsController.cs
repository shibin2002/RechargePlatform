using Microsoft.AspNetCore.Mvc;
using RechargePlatform.Common.DTOs;
using RechargePlatform.Data.Repositories;

namespace RechargeApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsRepository _analyticsRepo;

    public AnalyticsController(IAnalyticsRepository analyticsRepo)
    {
        _analyticsRepo = analyticsRepo;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var successfulToday = await _analyticsRepo.GetSuccessfulTransactionsTodayAsync();
        var failedToday = await _analyticsRepo.GetFailedTransactionsTodayAsync();
        var pending = await _analyticsRepo.GetPendingTransactionsAsync();
        var byOperator = await _analyticsRepo.GetTotalAmountByOperatorAsync();

        return Ok(ApiResponse<object>.Ok(new
        {
            SuccessfulToday = successfulToday,
            FailedToday = failedToday,
            Pending = pending,
            ByOperator = byOperator
        }));
    }

    [HttpGet("queries/{queryName}")]
    public async Task<IActionResult> RunQuery(
        string queryName,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-7);
        var end = endDate ?? DateTime.UtcNow;

        object result = queryName.ToLowerInvariant() switch
        {
            "successfultoday" => await _analyticsRepo.GetSuccessfulTransactionsTodayAsync(),
            "failedtoday" => await _analyticsRepo.GetFailedTransactionsTodayAsync(),
            "pending" => await _analyticsRepo.GetPendingTransactionsAsync(),
            "amountbyoperator" => await _analyticsRepo.GetTotalAmountByOperatorAsync(),
            "duplicatemobiles" => await _analyticsRepo.GetDuplicateMobileRechargesAsync(),
            "top10mobiles" => await _analyticsRepo.GetTop10MobilesByAmountAsync(),
            "daterange" => await _analyticsRepo.GetTransactionsBetweenDatesAsync(start, end),
            "cardsbyoperator" => await _analyticsRepo.GetAvailableCardsByOperatorAsync(),
            "cardsbydenomination" => await _analyticsRepo.GetAvailableCardsByDenominationAsync(),
            "usedcards" => await _analyticsRepo.GetUsedCardsHistoryAsync(),
            "expiredcards" => await _analyticsRepo.GetExpiredCardsAsync(),
            "cardsuseddaterange" => await _analyticsRepo.GetCardsUsedBetweenDatesAsync(start, end),
            _ => (object?)null!
        };

        if (result == null)
        {
            return BadRequest(ApiResponse<object>.Fail($"Query '{queryName}' is not recognized."));
        }

        return Ok(ApiResponse<object>.Ok(result));
    }
}
