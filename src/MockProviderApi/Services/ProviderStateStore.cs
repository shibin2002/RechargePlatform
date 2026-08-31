using System.Collections.Concurrent;
using RechargePlatform.Common.DTOs;

namespace MockProviderApi.Services;

public interface IProviderStateStore
{
    void RecordTransaction(string referenceId, string providerReference, string status, decimal amount, string mobileNumber, string @operator, string? errorMessage = null);
    ProviderStatusResponseDto? GetStatus(string referenceId);
    IEnumerable<ProviderStatusResponseDto> GetAll();
}

public class ProviderStateStore : IProviderStateStore
{
    private readonly ConcurrentDictionary<string, ProviderStatusResponseDto> _store = new(StringComparer.OrdinalIgnoreCase);

    public void RecordTransaction(string referenceId, string providerReference, string status, decimal amount, string mobileNumber, string @operator, string? errorMessage = null)
    {
        var record = new ProviderStatusResponseDto
        {
            ReferenceId = referenceId,
            ProviderReference = providerReference,
            Status = status,
            Amount = amount,
            MobileNumber = mobileNumber,
            Operator = @operator,
            ErrorMessage = errorMessage,
            Timestamp = DateTime.UtcNow
        };

        _store.AddOrUpdate(referenceId, record, (_, _) => record);
    }

    public ProviderStatusResponseDto? GetStatus(string referenceId)
    {
        _store.TryGetValue(referenceId, out var record);
        return record;
    }

    public IEnumerable<ProviderStatusResponseDto> GetAll()
    {
        return _store.Values.OrderByDescending(v => v.Timestamp);
    }
}
