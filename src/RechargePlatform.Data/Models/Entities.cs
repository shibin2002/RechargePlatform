namespace RechargePlatform.Data.Models;

public class TelecomOperatorEntity
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class RechargeTransactionEntity
{
    public long Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public int OperatorId { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool IsDuplicate { get; set; }
}

public class TransactionStatusHistoryEntity
{
    public long Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class ProviderRequestEntity
{
    public long Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public string? RequestBody { get; set; }
    public DateTime SentDate { get; set; }
}

public class ProviderResponseEntity
{
    public long Id { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public int? LatencyMs { get; set; }
    public DateTime ReceivedDate { get; set; }
    public string? ErrorMessage { get; set; }
}

public class RechargeCardEntity
{
    public long Id { get; set; }
    public int? BatchId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public int OperatorId { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public decimal Denomination { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? UsedTransactionId { get; set; }
    public DateTime? ReservedDate { get; set; }
    public DateTime? UsedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CardImportBatchEntity
{
    public int Id { get; set; }
    public Guid BatchGuid { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public int DuplicateRows { get; set; }
    public string ImportedBy { get; set; } = string.Empty;
    public DateTime ImportedDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
