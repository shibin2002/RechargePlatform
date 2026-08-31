using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using RechargePlatform.Common.DTOs;
using RechargePlatform.Data.Database;
using RechargePlatform.Data.Models;

namespace RechargePlatform.Data.Repositories;

public interface ICardRepository
{
    Task<(int BatchId, Guid BatchGuid)> CreateImportBatchAsync(string fileName, int totalRows, string importedBy);
    Task BulkInsertStagingCardsAsync(int batchId, IEnumerable<CardImportRowRaw> rows, CancellationToken cancellationToken = default);
    Task<CardImportResultDto> ProcessStagingCardsAsync(int batchId);
    Task<CardReservationResponseDto?> ReserveCardAtomicAsync(string cardNumber, string transactionId);
    Task<List<CardInventoryDto>> GetCardInventorySummaryAsync();
    Task<List<CardBatchSummaryDto>> GetImportBatchesAsync();
}

public class CardRepository : ICardRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public CardRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(int BatchId, Guid BatchGuid)> CreateImportBatchAsync(string fileName, int totalRows, string importedBy)
    {
        using var connection = _connectionFactory.CreateConnection();
        var batchGuid = Guid.NewGuid();

        const string sql = @"
            INSERT INTO dbo.CardImportBatches (
                BatchGuid, FileName, TotalRows, SuccessfulRows, FailedRows, DuplicateRows, ImportedBy, ImportedDate, Status
            )
            VALUES (
                @BatchGuid, @FileName, @TotalRows, 0, 0, 0, @ImportedBy, SYSUTCDATETIME(), 'PROCESSING'
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var batchId = await connection.ExecuteScalarAsync<int>(sql, new
        {
            BatchGuid = batchGuid,
            FileName = fileName,
            TotalRows = totalRows,
            ImportedBy = importedBy
        });

        return (batchId, batchGuid);
    }

    public async Task BulkInsertStagingCardsAsync(int batchId, IEnumerable<CardImportRowRaw> rows, CancellationToken cancellationToken = default)
    {
        using var table = new DataTable();
        table.Columns.Add("BatchId", typeof(int));
        table.Columns.Add("RowNumber", typeof(int));
        table.Columns.Add("CardNumber", typeof(string));
        table.Columns.Add("SerialNumber", typeof(string));
        table.Columns.Add("OperatorCode", typeof(string));
        table.Columns.Add("Denomination", typeof(string));
        table.Columns.Add("ExpiryDateStr", typeof(string));
        table.Columns.Add("ValidationStatus", typeof(string));
        table.Columns.Add("ErrorMessage", typeof(string));

        foreach (var r in rows)
        {
            table.Rows.Add(
                batchId,
                r.RowNumber,
                (object?)r.CardNumber ?? DBNull.Value,
                (object?)r.SerialNumber ?? DBNull.Value,
                (object?)r.Operator ?? DBNull.Value,
                (object?)r.Denomination ?? DBNull.Value,
                (object?)r.ExpiryDate ?? DBNull.Value,
                "PENDING",
                DBNull.Value
            );
        }

        using var sqlConn = _connectionFactory.CreateSqlConnection();
        await sqlConn.OpenAsync(cancellationToken);

        using var bulkCopy = new SqlBulkCopy(sqlConn)
        {
            DestinationTableName = "dbo.Staging_RechargeCards",
            BatchSize = 5000,
            BulkCopyTimeout = 120
        };

        bulkCopy.ColumnMappings.Add("BatchId", "BatchId");
        bulkCopy.ColumnMappings.Add("RowNumber", "RowNumber");
        bulkCopy.ColumnMappings.Add("CardNumber", "CardNumber");
        bulkCopy.ColumnMappings.Add("SerialNumber", "SerialNumber");
        bulkCopy.ColumnMappings.Add("OperatorCode", "OperatorCode");
        bulkCopy.ColumnMappings.Add("Denomination", "Denomination");
        bulkCopy.ColumnMappings.Add("ExpiryDateStr", "ExpiryDateStr");
        bulkCopy.ColumnMappings.Add("ValidationStatus", "ValidationStatus");
        bulkCopy.ColumnMappings.Add("ErrorMessage", "ErrorMessage");

        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }

    public async Task<CardImportResultDto> ProcessStagingCardsAsync(int batchId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@BatchId", batchId, DbType.Int32);

        using var multi = await connection.QueryMultipleAsync(
            "dbo.sp_ProcessStagingCards",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        // Result Set 1: Batch Summary
        var summary = await multi.ReadSingleAsync<dynamic>();

        // Result Set 2: Failed/Rejected Rows
        var failedRows = (await multi.ReadAsync<dynamic>()).Select(r => new FailedCardRowDto
        {
            RowNumber = (int)r.RowNumber,
            CardNumber = (string?)r.CardNumber,
            SerialNumber = (string?)r.SerialNumber,
            Operator = (string?)r.OperatorCode,
            Denomination = (string?)r.Denomination,
            ExpiryDate = (string?)r.ExpiryDateStr,
            ValidationStatus = (string)r.ValidationStatus,
            ErrorReason = (string)r.ErrorMessage
        }).ToList();

        return new CardImportResultDto
        {
            BatchId = (int)summary.BatchId,
            BatchGuid = (Guid)summary.BatchGuid,
            FileName = (string)summary.FileName,
            TotalRows = (int)summary.TotalRows,
            Imported = (int)summary.SuccessfulRows,
            Failed = (int)summary.FailedRows,
            Duplicates = (int)summary.DuplicateRows,
            Status = (string)summary.Status,
            FailedRows = failedRows
        };
    }

    public async Task<CardReservationResponseDto?> ReserveCardAtomicAsync(string cardNumber, string transactionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@CardNumber", cardNumber, DbType.String);
        parameters.Add("@TransactionId", transactionId, DbType.String);

        var result = await connection.QuerySingleOrDefaultAsync<CardReservationResponseDto>(
            "dbo.sp_ReserveCardAtomic",
            parameters,
            commandType: CommandType.StoredProcedure
        );

        return result;
    }

    public async Task<List<CardInventoryDto>> GetCardInventorySummaryAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        var result = await connection.QueryAsync<CardInventoryDto>(
            "dbo.sp_GetCardInventorySummary",
            commandType: CommandType.StoredProcedure
        );

        return result.AsList();
    }

    public async Task<List<CardBatchSummaryDto>> GetImportBatchesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        const string sql = @"
            SELECT 
                Id AS BatchId,
                BatchGuid,
                FileName,
                TotalRows,
                SuccessfulRows,
                FailedRows,
                DuplicateRows,
                Status,
                ImportedDate,
                ImportedBy
            FROM dbo.CardImportBatches
            ORDER BY ImportedDate DESC;";

        var result = await connection.QueryAsync<CardBatchSummaryDto>(sql);
        return result.AsList();
    }
}
