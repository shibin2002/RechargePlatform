-- ============================================================================
-- 05_analytics_queries.sql: Production Analytical & Operational Queries
-- Telecom Prepaid Recharge Platform
-- ============================================================================

USE RechargeDb;
GO

-- ============================================================================
-- PART 1: RECHARGE TRANSACTION QUERIES
-- ============================================================================

-- 1. Successful Transactions Today (UTC)
SELECT 
    t.TransactionId,
    t.MobileNumber,
    o.Code AS Operator,
    t.Amount,
    t.ProviderReference,
    t.CreatedDate,
    t.UpdatedDate
FROM dbo.RechargeTransactions t
INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
WHERE t.Status = 'SUCCESS'
  AND CAST(t.CreatedDate AS DATE) = CAST(SYSUTCDATETIME() AS DATE)
ORDER BY t.CreatedDate DESC;

-- 2. Failed Transactions Today
SELECT 
    t.TransactionId,
    t.MobileNumber,
    o.Code AS Operator,
    t.Amount,
    t.ErrorMessage,
    t.CreatedDate
FROM dbo.RechargeTransactions t
INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
WHERE t.Status = 'FAILED'
  AND CAST(t.CreatedDate AS DATE) = CAST(SYSUTCDATETIME() AS DATE)
ORDER BY t.CreatedDate DESC;

-- 3. Pending Transactions Requiring Reconciliation
SELECT 
    t.TransactionId,
    t.MobileNumber,
    o.Code AS Operator,
    t.Amount,
    t.ProviderReference,
    t.CreatedDate,
    DATEDIFF(MINUTE, t.UpdatedDate, SYSUTCDATETIME()) AS MinutesPending
FROM dbo.RechargeTransactions t
INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
WHERE t.Status = 'PENDING'
ORDER BY t.CreatedDate ASC;

-- 4. Total Recharge Amount & Volume by Operator (All Time & Today)
SELECT 
    o.Code AS OperatorCode,
    o.Name AS OperatorName,
    COUNT(CASE WHEN t.Status = 'SUCCESS' THEN 1 END) AS SuccessfulCount,
    COALESCE(SUM(CASE WHEN t.Status = 'SUCCESS' THEN t.Amount ELSE 0 END), 0) AS TotalSuccessfulAmount,
    COUNT(CASE WHEN t.Status = 'FAILED' THEN 1 END) AS FailedCount,
    COUNT(CASE WHEN t.Status = 'PENDING' THEN 1 END) AS PendingCount,
    COUNT(1) AS TotalAttemptedCount
FROM dbo.TelecomOperators o
LEFT JOIN dbo.RechargeTransactions t ON o.Id = t.OperatorId
GROUP BY o.Code, o.Name
ORDER BY TotalSuccessfulAmount DESC;

-- 5. Duplicate / Multiple Recharges for the Same Mobile Number
SELECT 
    t.MobileNumber,
    COUNT(1) AS TotalRechargeAttempts,
    COUNT(CASE WHEN t.Status = 'SUCCESS' THEN 1 END) AS SuccessfulRecharges,
    SUM(CASE WHEN t.Status = 'SUCCESS' THEN t.Amount ELSE 0 END) AS TotalRechargedAmount,
    MIN(t.CreatedDate) AS FirstRechargeDate,
    MAX(t.CreatedDate) AS LatestRechargeDate
FROM dbo.RechargeTransactions t
GROUP BY t.MobileNumber
HAVING COUNT(1) > 1
ORDER BY TotalRechargeAttempts DESC;

-- 6. Top 10 Mobile Numbers by Total Successful Recharge Amount
SELECT TOP 10
    t.MobileNumber,
    COUNT(1) AS RechargeCount,
    SUM(t.Amount) AS TotalAmountSpent,
    MAX(t.CreatedDate) AS LastRechargedAt
FROM dbo.RechargeTransactions t
WHERE t.Status = 'SUCCESS'
GROUP BY t.MobileNumber
ORDER BY TotalAmountSpent DESC;

-- 7. Transactions Between Two Dates (Parameterized Example)
DECLARE @StartDate DATETIME2 = DATEADD(DAY, -7, SYSUTCDATETIME());
DECLARE @EndDate DATETIME2 = SYSUTCDATETIME();

SELECT 
    t.TransactionId,
    t.MobileNumber,
    o.Code AS Operator,
    t.Amount,
    t.Status,
    t.ProviderReference,
    t.ErrorMessage,
    t.CreatedDate
FROM dbo.RechargeTransactions t
INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
WHERE t.CreatedDate >= @StartDate 
  AND t.CreatedDate <= @EndDate
ORDER BY t.CreatedDate DESC;


-- ============================================================================
-- PART 2: CARD / VOUCHER INVENTORY QUERIES
-- ============================================================================

-- 8. Available Cards by Operator
SELECT 
    o.Code AS OperatorCode,
    o.Name AS OperatorName,
    COUNT(1) AS AvailableCardsCount,
    SUM(c.Denomination) AS TotalInventoryValue
FROM dbo.RechargeCards c
INNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id
WHERE c.Status = 'AVAILABLE'
GROUP BY o.Code, o.Name
ORDER BY AvailableCardsCount DESC;

-- 9. Available Cards by Denomination
SELECT 
    c.Denomination,
    COUNT(1) AS AvailableCardsCount,
    SUM(c.Denomination) AS TotalStockValue
FROM dbo.RechargeCards c
WHERE c.Status = 'AVAILABLE'
GROUP BY c.Denomination
ORDER BY c.Denomination ASC;

-- 10. Used Cards History with Linked Transaction Details
SELECT 
    c.CardNumber,
    c.SerialNumber,
    o.Code AS Operator,
    c.Denomination,
    c.UsedTransactionId,
    c.UsedDate,
    t.MobileNumber AS RechargedMobile,
    t.Status AS TransactionStatus
FROM dbo.RechargeCards c
INNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id
LEFT JOIN dbo.RechargeTransactions t ON c.UsedTransactionId = t.TransactionId
WHERE c.Status = 'USED'
ORDER BY c.UsedDate DESC;

-- 11. Expired Cards
SELECT 
    c.CardNumber,
    c.SerialNumber,
    o.Code AS Operator,
    c.Denomination,
    c.ExpiryDate,
    c.Status
FROM dbo.RechargeCards c
INNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id
WHERE c.Status = 'EXPIRED' 
   OR (c.Status = 'AVAILABLE' AND c.ExpiryDate < CAST(SYSUTCDATETIME() AS DATE))
ORDER BY c.ExpiryDate ASC;

-- 12. Cards Imported in a Specific Batch (e.g. Batch Id = 1)
DECLARE @TargetBatchId INT = 1;

SELECT 
    b.FileName,
    b.ImportedDate,
    b.ImportedBy,
    c.CardNumber,
    c.SerialNumber,
    o.Code AS Operator,
    c.Denomination,
    c.ExpiryDate,
    c.Status
FROM dbo.RechargeCards c
INNER JOIN dbo.CardImportBatches b ON c.BatchId = b.Id
INNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id
WHERE c.BatchId = @TargetBatchId
ORDER BY c.Id ASC;

-- 13. Audit: Find Duplicate Card Numbers (Should be 0 due to unique constraint)
SELECT 
    CardNumber,
    COUNT(1) AS Occurrences
FROM dbo.RechargeCards
GROUP BY CardNumber
HAVING COUNT(1) > 1;

-- 14. Available Card Counts Matrix per Operator + Denomination
SELECT 
    o.Code AS OperatorCode,
    c.Denomination,
    COUNT(1) AS AvailableCount,
    MIN(c.ExpiryDate) AS EarliestExpiryDate
FROM dbo.RechargeCards c
INNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id
WHERE c.Status = 'AVAILABLE'
GROUP BY o.Code, c.Denomination
ORDER BY o.Code, c.Denomination;

-- 15. Cards Used Between Two Dates
DECLARE @CardUsageStart DATETIME2 = DATEADD(DAY, -30, SYSUTCDATETIME());
DECLARE @CardUsageEnd DATETIME2 = SYSUTCDATETIME();

SELECT 
    c.CardNumber,
    c.SerialNumber,
    o.Code AS Operator,
    c.Denomination,
    c.UsedTransactionId,
    c.UsedDate
FROM dbo.RechargeCards c
INNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id
WHERE c.UsedDate >= @CardUsageStart 
  AND c.UsedDate <= @CardUsageEnd
ORDER BY c.UsedDate DESC;
GO
