using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using RechargePlatform.Common.Constants;
using RechargePlatform.Common.DTOs;
using RechargePlatform.Data.Repositories;

namespace RechargeApi.Services;

public interface IProviderClient
{
    Task<(bool Success, ProviderRechargeResponseDto? Response, string? ErrorMessage, bool IsTimeout)> RechargeAsync(
        string transactionId, string mobileNumber, string @operator, decimal amount, CancellationToken cancellationToken = default);

    Task<ProviderStatusResponseDto?> GetStatusAsync(string referenceId, CancellationToken cancellationToken = default);
}

public class ProviderClient : IProviderClient
{
    private readonly HttpClient _httpClient;
    private readonly IRechargeRepository _rechargeRepository;
    private readonly ILogger<ProviderClient> _logger;

    public ProviderClient(HttpClient httpClient, IRechargeRepository rechargeRepository, ILogger<ProviderClient> logger)
    {
        _httpClient = httpClient;
        _rechargeRepository = rechargeRepository;
        _logger = logger;
    }

    public async Task<(bool Success, ProviderRechargeResponseDto? Response, string? ErrorMessage, bool IsTimeout)> RechargeAsync(
        string transactionId, string mobileNumber, string @operator, decimal amount, CancellationToken cancellationToken = default)
    {
        var requestDto = new ProviderRechargeRequestDto
        {
            ReferenceId = transactionId,
            MobileNumber = mobileNumber,
            Operator = @operator,
            Amount = amount
        };

        var requestJson = JsonSerializer.Serialize(requestDto);
        var requestUrl = $"{_httpClient.BaseAddress}api/provider/recharge";

        // Audit Log Outbound Request
        try
        {
            await _rechargeRepository.LogProviderRequestAsync(transactionId, requestUrl, requestJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for provider request {TransactionId}", transactionId);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Dispatching HTTP request to provider for TransactionId={TransactionId}, Amount={Amount}", transactionId, amount);

            using var response = await _httpClient.PostAsJsonAsync("api/provider/recharge", requestDto, cancellationToken);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var statusCode = (int)response.StatusCode;

            _logger.LogInformation("Provider HTTP response received: Status={StatusCode}, Latency={Latency}ms for {TransactionId}",
                statusCode, stopwatch.ElapsedMilliseconds, transactionId);

            // Audit Log Inbound Response
            await _rechargeRepository.LogProviderResponseAsync(transactionId, statusCode, responseBody, (int)stopwatch.ElapsedMilliseconds);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ProviderRechargeResponseDto>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return (result?.Status == TransactionStatuses.Success, result, result?.ErrorMessage, false);
            }
            else
            {
                var errorMsg = $"Provider responded with HTTP status {statusCode}: {responseBody}";
                _logger.LogWarning("Provider error response for {TransactionId}: {ErrorMsg}", transactionId, errorMsg);
                return (false, null, errorMsg, false);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Client-side HTTP timeout (10s limit exceeded by provider e.g. amount 300)
            stopwatch.Stop();
            var errorMsg = $"Client HTTP request timed out after {stopwatch.ElapsedMilliseconds}ms waiting for provider response.";
            _logger.LogWarning("Timeout calling provider for TransactionId={TransactionId}: {ErrorMsg}", transactionId, errorMsg);

            await _rechargeRepository.LogProviderResponseAsync(transactionId, null, null, (int)stopwatch.ElapsedMilliseconds, errorMsg);

            return (false, null, errorMsg, true);
        }
        catch (HttpRequestException ex)
        {
            // Network connection aborted / reset (e.g. amount 400 dropped connection)
            stopwatch.Stop();
            var errorMsg = $"Network error communicating with provider: {ex.Message}";
            _logger.LogWarning("HttpRequestException for {TransactionId}: {ErrorMsg}", transactionId, errorMsg);

            await _rechargeRepository.LogProviderResponseAsync(transactionId, null, null, (int)stopwatch.ElapsedMilliseconds, errorMsg);

            // A dropped connection must transition to PENDING so reconciliation can resolve it
            return (false, null, errorMsg, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorMsg = $"Unexpected exception calling provider: {ex.Message}";
            _logger.LogError(ex, "Unexpected error for {TransactionId}", transactionId);

            await _rechargeRepository.LogProviderResponseAsync(transactionId, null, null, (int)stopwatch.ElapsedMilliseconds, errorMsg);

            return (false, null, errorMsg, true);
        }
    }

    public async Task<ProviderStatusResponseDto?> GetStatusAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("Querying provider status enquiry for ReferenceId={ReferenceId}", referenceId);

            using var response = await _httpClient.GetAsync($"api/provider/status/{referenceId}", cancellationToken);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var statusCode = (int)response.StatusCode;

            _logger.LogInformation("Provider status enquiry response: Status={StatusCode}, Latency={Latency}ms for {ReferenceId}",
                statusCode, stopwatch.ElapsedMilliseconds, referenceId);

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<ProviderStatusResponseDto>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            _logger.LogWarning("Provider status enquiry returned non-success HTTP {StatusCode}: {ResponseBody}", statusCode, responseBody);
            return null;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during provider status enquiry for ReferenceId={ReferenceId}", referenceId);
            return null;
        }
    }
}
