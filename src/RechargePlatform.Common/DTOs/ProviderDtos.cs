namespace RechargePlatform.Common.DTOs;

public class ProviderRechargeRequestDto
{
    public string ReferenceId { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class ProviderRechargeResponseDto
{
    public string ReferenceId { get; set; } = string.Empty;
    public string ProviderReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ProviderStatusResponseDto
{
    public string ReferenceId { get; set; } = string.Empty;
    public string ProviderReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public decimal Amount { get; set; }
    public string MobileNumber { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
