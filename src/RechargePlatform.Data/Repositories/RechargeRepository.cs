using System.Data;
using Dapper;
using RechargePlatform.Common.DTOs;
using RechargePlatform.Data.Database;
using RechargePlatform.Data.Models;

namespace RechargePlatform.Data.Repositories;

public interface IRechargeRepository
{
    Task<RechargeTransactionEntity> CreateTransactionAsync(string transactionId, string mobileNumber, string operatorCode, decimal amount);
    Task<RechargeTransactionEntity> UpdateStatusAsync(string transactionId, string status, string? providerReference = null, string? errorMessage = null, string? remarks = null);
    Task<(RechargeTransactionEntity? Transaction, List<TransactionStatusHistoryEntity> History)> GetByTransactionIdAsync(string transactionId);
    Task<RechargeTransactionEntity?> GetByProviderReferenceAsync(string providerReference);
    Task<PagedResult<RechargeTransactionEntity>> GetFilteredTransactionsAsync(TransactionFilterDto filter);
    Task<List<RechargeTransactionEntity>> GetPendingTransactionsAsync(int maxAgeMinutes = 1440, int limit = 100);
    Task LogProviderRequestAsync(string transactionId, string requestUrl, string? requestBody);
    Task LogProviderResponseAsync(string transactionId, int? statusCode, string? responseBody, int? latencyMs, string? errorMessage = null);
}

public class RechargeRepository : IRechargeRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public RechargeRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<RechargeTransactionEntity> CreateTransactionAsync(string transactionId, string mobileNumber, string operatorCode, decimal amount)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@TransactionId", transactionId, DbType.String);
        parameters.Add("@MobileNumber", mobileNumber, DbType.String);
        parameters.Add("@OperatorCode", operatorCode, DbType.String);
        parameters.Add("@Amount", amount, DbType.Decimal);

        var result = await connection.QuerySingleAsync<RechargeTransactionEntity>(
            "dbo.sp_CreateRechargeTransaction",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return result;
    }

    public async Task<RechargeTransactionEntity> UpdateStatusAsync(string transactionId, string status, string? providerReference = null, string? errorMessage = null, string? remarks = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@TransactionId", transactionId, DbType.String);
        parameters.Add("@Status", status, DbType.String);
        parameters.Add("@ProviderReference", providerReference, DbType.String);
        parameters.Add("@ErrorMessage", errorMessage, DbType.String);
        parameters.Add("@Remarks", remarks, DbType.String);

        var result = await connection.QuerySingleAsync<RechargeTransactionEntity>(
            "dbo.sp_UpdateRechargeStatus",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return result;
    }

    public async Task<(RechargeTransactionEntity? Transaction, List<TransactionStatusHistoryEntity> History)> GetByTransactionIdAsync(string transactionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@TransactionId", transactionId, DbType.String);

        using var multi = await connection.QueryMultipleAsync(
            "dbo.sp_GetTransactionByTransactionId",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        var transaction = await multi.ReadSingleOrDefaultAsync<RechargeTransactionEntity>();
        var history = (await multi.ReadAsync<TransactionStatusHistoryEntity>()).AsList();

        return (transaction, history);
    }

    public async Task<RechargeTransactionEntity?> GetByProviderReferenceAsync(string providerReference)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@ProviderReference", providerReference, DbType.String);

        return await connection.QuerySingleOrDefaultAsync<RechargeTransactionEntity>(
            "dbo.sp_GetTransactionByProviderReference",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<PagedResult<RechargeTransactionEntity>> GetFilteredTransactionsAsync(TransactionFilterDto filter)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Status", string.IsNullOrWhiteSpace(filter.Status) ? null : filter.Status, DbType.String);
        parameters.Add("@OperatorCode", string.IsNullOrWhiteSpace(filter.Operator) ? null : filter.Operator, DbType.String);
        parameters.Add("@MobileNumber", string.IsNullOrWhiteSpace(filter.MobileNumber) ? null : filter.MobileNumber, DbType.String);
        parameters.Add("@FromDate", filter.FromDate, DbType.DateTime2);
        parameters.Add("@ToDate", filter.ToDate, DbType.DateTime2);
        parameters.Add("@PageNumber", filter.PageNumber < 1 ? 1 : filter.PageNumber, DbType.Int32);
        parameters.Add("@PageSize", filter.PageSize < 1 ? 50 : filter.PageSize, DbType.Int32);

        using var multi = await connection.QueryMultipleAsync(
            "dbo.sp_GetTransactionsFiltered",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<RechargeTransactionEntity>()).AsList();

        return new PagedResult<RechargeTransactionEntity>
        {
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            Items = items
        };
    }

    public async Task<List<RechargeTransactionEntity>> GetPendingTransactionsAsync(int maxAgeMinutes = 1440, int limit = 100)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@MaxAgeMinutes", maxAgeMinutes, DbType.Int32);
        parameters.Add("@Limit", limit, DbType.Int32);

        var result = await connection.QueryAsync<RechargeTransactionEntity>(
            "dbo.sp_GetPendingTransactions",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return result.AsList();
    }

    public async Task LogProviderRequestAsync(string transactionId, string requestUrl, string? requestBody)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.ProviderRequests (TransactionId, RequestUrl, RequestBody, SentDate)
            VALUES (@TransactionId, @RequestUrl, @RequestBody, SYSUTCDATETIME());";

        await connection.ExecuteAsync(sql, new
        {
            TransactionId = transactionId,
            RequestUrl = requestUrl,
            RequestBody = requestBody
        });
    }

    public async Task LogProviderResponseAsync(string transactionId, int? statusCode, string? responseBody, int? latencyMs, string? errorMessage = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            INSERT INTO dbo.ProviderResponses (TransactionId, HttpStatusCode, ResponseBody, LatencyMs, ReceivedDate, ErrorMessage)
            VALUES (@TransactionId, @StatusCode, @ResponseBody, @LatencyMs, SYSUTCDATETIME(), @ErrorMessage);";

        await connection.ExecuteAsync(sql, new
        {
            TransactionId = transactionId,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            LatencyMs = latencyMs,
            ErrorMessage = errorMessage
        });
    }
}
