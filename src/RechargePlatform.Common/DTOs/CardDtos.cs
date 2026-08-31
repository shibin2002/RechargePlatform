using System.ComponentModel.DataAnnotations;

namespace RechargePlatform.Common.DTOs;

public class CardImportRowRaw
{
    public int RowNumber { get; set; }
    public string? CardNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Operator { get; set; }
    public string? Denomination { get; set; }
    public string? ExpiryDate { get; set; }
}

public class CardImportResultDto
{
    public int BatchId { get; set; }
    public Guid BatchGuid { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int Failed { get; set; }
    public int Duplicates { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<FailedCardRowDto> FailedRows { get; set; } = new();
}

public class FailedCardRowDto
{
    public int RowNumber { get; set; }
    public string? CardNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? Operator { get; set; }
    public string? Denomination { get; set; }
    public string? ExpiryDate { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
    public string ErrorReason { get; set; } = string.Empty;
}

public class CardReservationRequestDto
{
    [Required(ErrorMessage = "CardNumber is required.")]
    public string CardNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "TransactionId is required.")]
    public string TransactionId { get; set; } = string.Empty;
}

public class CardReservationResponseDto
{
    public long Id { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public int OperatorId { get; set; }
    public decimal Denomination { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? UsedTransactionId { get; set; }
    public DateTime? ReservedDate { get; set; }
}

public class CardInventoryDto
{
    public string OperatorCode { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public decimal Denomination { get; set; }
    public int AvailableCount { get; set; }
    public int ReservedCount { get; set; }
    public int UsedCount { get; set; }
    public int ExpiredCount { get; set; }
    public int BlockedCount { get; set; }
    public int TotalCount { get; set; }
}

public class CardBatchSummaryDto
{
    public int BatchId { get; set; }
    public Guid BatchGuid { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public int DuplicateRows { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ImportedDate { get; set; }
    public string ImportedBy { get; set; } = string.Empty;
}
