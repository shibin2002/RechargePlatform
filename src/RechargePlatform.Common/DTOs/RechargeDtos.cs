using System.ComponentModel.DataAnnotations;

namespace RechargePlatform.Common.DTOs;

public class RechargeRequestDto
{
    [Required(ErrorMessage = "TransactionId is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "TransactionId must be between 3 and 50 characters.")]
    public string TransactionId { get; set; } = string.Empty;

    [Required(ErrorMessage = "MobileNumber is required.")]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "MobileNumber must be a valid 10-digit Indian mobile number starting with 6, 7, 8, or 9.")]
    public string MobileNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Operator is required.")]
    public string Operator { get; set; } = string.Empty;

    [Range(1, 50000, ErrorMessage = "Amount must be between 1 and 50000 INR.")]
    public decimal Amount { get; set; }
}

public class RechargeResponseDto
{
    public long Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDuplicate { get; set; }
    public List<StatusHistoryDto>? History { get; set; }
}

public class StatusHistoryDto
{
    public long Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class TransactionFilterDto
{
    public string? Status { get; set; }
    public string? Operator { get; set; }
    public string? MobileNumber { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class ReconcileResponseDto
{
    public string TransactionId { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ReconciledAt { get; set; } = DateTime.UtcNow;
}
