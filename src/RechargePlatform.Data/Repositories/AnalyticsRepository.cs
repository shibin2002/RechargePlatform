using Dapper;
using RechargePlatform.Data.Database;

namespace RechargePlatform.Data.Repositories;

public interface IAnalyticsRepository
{
    Task<IEnumerable<dynamic>> GetSuccessfulTransactionsTodayAsync();
    Task<IEnumerable<dynamic>> GetFailedTransactionsTodayAsync();
    Task<IEnumerable<dynamic>> GetPendingTransactionsAsync();
    Task<IEnumerable<dynamic>> GetTotalAmountByOperatorAsync();
    Task<IEnumerable<dynamic>> GetDuplicateMobileRechargesAsync();
    Task<IEnumerable<dynamic>> GetTop10MobilesByAmountAsync();
    Task<IEnumerable<dynamic>> GetTransactionsBetweenDatesAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<dynamic>> GetAvailableCardsByOperatorAsync();
    Task<IEnumerable<dynamic>> GetAvailableCardsByDenominationAsync();
    Task<IEnumerable<dynamic>> GetUsedCardsHistoryAsync();
    Task<IEnumerable<dynamic>> GetExpiredCardsAsync();
    Task<IEnumerable<dynamic>> GetCardsUsedBetweenDatesAsync(DateTime startDate, DateTime endDate);
}

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AnalyticsRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<dynamic>> GetSuccessfulTransactionsTodayAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
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
            ORDER BY t.CreatedDate DESC;";
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetFailedTransactionsTodayAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
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
            ORDER BY t.CreatedDate DESC;";
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetPendingTransactionsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
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
            ORDER BY t.CreatedDate ASC;";
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetTotalAmountByOperatorAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
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
            ORDER BY TotalSuccessfulAmount DESC;";
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetDuplicateMobileRechargesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
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
            ORDER BY TotalRechargeAttempts DESC;";
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetTop10MobilesByAmountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT TOP 10
                t.MobileNumber,
                COUNT(1) AS RechargeCount,
                SUM(t.Amount) AS TotalAmountSpent,
                MAX(t.CreatedDate) AS LastRechargedAt
            FROM dbo.RechargeTransactions t
            WHERE t.Status = 'SUCCESS'
            GROUP BY t.MobileNumber
            ORDER BY TotalAmountSpent DESC;";
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetTransactionsBetweenDatesAsync(DateTime startDate, DateTime endDate)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
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
            ORDER BY t.CreatedDate DESC;";
        return await connection.QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
    }

    public async Task<IEnumerable<dynamic>> GetAvailableCardsByOperatorAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                o.Code AS OperatorCode,
                o.Name AS OperatorName,
                COUNT(1) AS AvailableCardsCount,
                SUM(c.Denomination) AS TotalInventoryValue
            FROM dbo.RechargeCards c
            INNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id
            WHERE c.Status = 'AVAILABLE'
            GROUP BY o.Code, o.Name
            ORDER BY AvailableCardsCount DESC;";
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetAvailableCardsByDenominationAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                c.Denomination,
                COUNT(1) AS AvailableCardsCount,
                SUM(c.Denomination) AS TotalStockValue
            FROM dbo.RechargeCards c
            WHERE c.Status = 'AVAILABLE'
            GROUP BY c.Denomination
            ORDER BY c.Denomination ASC;";
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetUsedCardsHistoryAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
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
            ORDER BY c.UsedDate DESC;";
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetExpiredCardsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
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
            ORDER BY c.ExpiryDate ASC;";
        return await connection.QueryAsync(sql);
    }

    public async Task<IEnumerable<dynamic>> GetCardsUsedBetweenDatesAsync(DateTime startDate, DateTime endDate)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                c.CardNumber,
                c.SerialNumber,
                o.Code AS Operator,
                c.Denomination,
                c.UsedTransactionId,
                c.UsedDate
            FROM dbo.RechargeCards c
            INNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id
            WHERE c.UsedDate >= @StartDate 
              AND c.UsedDate <= @EndDate
            ORDER BY c.UsedDate DESC;";
        return await connection.QueryAsync(sql, new { StartDate = startDate, EndDate = endDate });
    }
}
