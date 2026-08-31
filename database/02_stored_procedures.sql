-- ============================================================================
-- 02_stored_procedures.sql: Stored Procedures
-- Telecom Prepaid Recharge Platform
-- ============================================================================

USE RechargeDb;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. sp_CreateRechargeTransaction
-- Concurrency Safe: Direct INSERT catching SQL Error 2627/2601 (Unique Violation)
-- Inserts record in PROCESSING status (NEW -> PROCESSING) and commits immediately.
CREATE OR ALTER PROCEDURE dbo.sp_CreateRechargeTransaction
    @TransactionId VARCHAR(50),
    @MobileNumber VARCHAR(15),
    @OperatorCode VARCHAR(20),
    @Amount DECIMAL(18,2)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OperatorId INT;
    DECLARE @IsDuplicate BIT = 0;
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    -- Resolve Operator
    SELECT @OperatorId = Id 
    FROM dbo.TelecomOperators 
    WHERE Code = @OperatorCode AND IsActive = 1;

    IF @OperatorId IS NULL
    BEGIN
        THROW 50001, 'Invalid or inactive telecom operator code.', 1;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Direct INSERT attempting row creation in PROCESSING status
        INSERT INTO dbo.RechargeTransactions (
            TransactionId,
            MobileNumber,
            OperatorId,
            Amount,
            Status,
            ProviderReference,
            ErrorMessage,
            CreatedDate,
            UpdatedDate
        )
        VALUES (
            @TransactionId,
            @MobileNumber,
            @OperatorId,
            @Amount,
            'PROCESSING',
            NULL,
            NULL,
            @Now,
            @Now
        );

        -- Record initial state transitions: NEW -> PROCESSING
        INSERT INTO dbo.TransactionStatusHistory (
            TransactionId,
            OldStatus,
            NewStatus,
            Remarks,
            CreatedDate
        )
        VALUES (
            @TransactionId,
            'NEW',
            'PROCESSING',
            'Transaction initialized and moving to provider dispatch',
            @Now
        );

        COMMIT TRANSACTION;

        -- Return newly created transaction
        SELECT 
            t.Id,
            t.TransactionId,
            t.MobileNumber,
            o.Code AS OperatorCode,
            o.Name AS OperatorName,
            t.Amount,
            t.Status,
            t.ProviderReference,
            t.ErrorMessage,
            t.CreatedDate,
            t.UpdatedDate,
            CAST(0 AS BIT) AS IsDuplicate
        FROM dbo.RechargeTransactions t
        INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
        WHERE t.TransactionId = @TransactionId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        -- Catch Unique Constraint / Duplicate Key Violations (2627: Unique Constraint, 2601: Duplicate Index)
        IF ERROR_NUMBER() IN (2627, 2601)
        BEGIN
            -- Retrieve existing transaction state without re-calling provider
            SELECT 
                t.Id,
                t.TransactionId,
                t.MobileNumber,
                o.Code AS OperatorCode,
                o.Name AS OperatorName,
                t.Amount,
                t.Status,
                t.ProviderReference,
                t.ErrorMessage,
                t.CreatedDate,
                t.UpdatedDate,
                CAST(1 AS BIT) AS IsDuplicate
            FROM dbo.RechargeTransactions t
            INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
            WHERE t.TransactionId = @TransactionId;
        END
        ELSE
        BEGIN
            -- Rethrow unexpected SQL errors
            THROW;
        END
    END CATCH
END
GO

-- 2. sp_UpdateRechargeStatus
-- Opens a short, isolated DB transaction to record the final result and history.
CREATE OR ALTER PROCEDURE dbo.sp_UpdateRechargeStatus
    @TransactionId VARCHAR(50),
    @Status VARCHAR(20), -- SUCCESS, FAILED, PENDING, PROCESSING
    @ProviderReference VARCHAR(100) = NULL,
    @ErrorMessage NVARCHAR(MAX) = NULL,
    @Remarks NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @OldStatus VARCHAR(20);
    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Get current status with UPDLOCK
        SELECT @OldStatus = Status
        FROM dbo.RechargeTransactions WITH (UPDLOCK, ROWLOCK)
        WHERE TransactionId = @TransactionId;

        IF @OldStatus IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 50002, 'Transaction not found.', 1;
        END

        -- Update transaction
        UPDATE dbo.RechargeTransactions
        SET 
            Status = @Status,
            ProviderReference = COALESCE(@ProviderReference, ProviderReference),
            ErrorMessage = @ErrorMessage,
            UpdatedDate = @Now
        WHERE TransactionId = @TransactionId;

        -- Record status history
        INSERT INTO dbo.TransactionStatusHistory (
            TransactionId,
            OldStatus,
            NewStatus,
            Remarks,
            CreatedDate
        )
        VALUES (
            @TransactionId,
            @OldStatus,
            @Status,
            COALESCE(@Remarks, 'Status updated to ' + @Status),
            @Now
        );

        COMMIT TRANSACTION;

        -- Return updated transaction
        SELECT 
            t.Id,
            t.TransactionId,
            t.MobileNumber,
            o.Code AS OperatorCode,
            o.Name AS OperatorName,
            t.Amount,
            t.Status,
            t.ProviderReference,
            t.ErrorMessage,
            t.CreatedDate,
            t.UpdatedDate
        FROM dbo.RechargeTransactions t
        INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
        WHERE t.TransactionId = @TransactionId;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- 3. sp_GetTransactionByTransactionId
CREATE OR ALTER PROCEDURE dbo.sp_GetTransactionByTransactionId
    @TransactionId VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Transaction Details
    SELECT 
        t.Id,
        t.TransactionId,
        t.MobileNumber,
        o.Code AS OperatorCode,
        o.Name AS OperatorName,
        t.Amount,
        t.Status,
        t.ProviderReference,
        t.ErrorMessage,
        t.CreatedDate,
        t.UpdatedDate
    FROM dbo.RechargeTransactions t
    INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
    WHERE t.TransactionId = @TransactionId;

    -- Result Set 2: Status History
    SELECT 
        Id,
        TransactionId,
        OldStatus,
        NewStatus,
        Remarks,
        CreatedDate
    FROM dbo.TransactionStatusHistory
    WHERE TransactionId = @TransactionId
    ORDER BY Id ASC;
END
GO

-- 4. sp_GetTransactionByProviderReference
CREATE OR ALTER PROCEDURE dbo.sp_GetTransactionByProviderReference
    @ProviderReference VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.Id,
        t.TransactionId,
        t.MobileNumber,
        o.Code AS OperatorCode,
        o.Name AS OperatorName,
        t.Amount,
        t.Status,
        t.ProviderReference,
        t.ErrorMessage,
        t.CreatedDate,
        t.UpdatedDate
    FROM dbo.RechargeTransactions t
    INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
    WHERE t.ProviderReference = @ProviderReference;
END
GO

-- 5. sp_GetTransactionsFiltered
CREATE OR ALTER PROCEDURE dbo.sp_GetTransactionsFiltered
    @Status VARCHAR(20) = NULL,
    @OperatorCode VARCHAR(20) = NULL,
    @MobileNumber VARCHAR(15) = NULL,
    @FromDate DATETIME2 = NULL,
    @ToDate DATETIME2 = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Offset INT = (@PageNumber - 1) * @PageSize;

    -- Total Count
    SELECT COUNT(1) AS TotalCount
    FROM dbo.RechargeTransactions t
    INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
    WHERE (@Status IS NULL OR t.Status = @Status)
      AND (@OperatorCode IS NULL OR o.Code = @OperatorCode)
      AND (@MobileNumber IS NULL OR t.MobileNumber LIKE '%' + @MobileNumber + '%')
      AND (@FromDate IS NULL OR t.CreatedDate >= @FromDate)
      AND (@ToDate IS NULL OR t.CreatedDate <= @ToDate);

    -- Paged Items
    SELECT 
        t.Id,
        t.TransactionId,
        t.MobileNumber,
        o.Code AS OperatorCode,
        o.Name AS OperatorName,
        t.Amount,
        t.Status,
        t.ProviderReference,
        t.ErrorMessage,
        t.CreatedDate,
        t.UpdatedDate
    FROM dbo.RechargeTransactions t
    INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
    WHERE (@Status IS NULL OR t.Status = @Status)
      AND (@OperatorCode IS NULL OR o.Code = @OperatorCode)
      AND (@MobileNumber IS NULL OR t.MobileNumber LIKE '%' + @MobileNumber + '%')
      AND (@FromDate IS NULL OR t.CreatedDate >= @FromDate)
      AND (@ToDate IS NULL OR t.CreatedDate <= @ToDate)
    ORDER BY t.CreatedDate DESC
    OFFSET @Offset ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- 6. sp_GetPendingTransactions
CREATE OR ALTER PROCEDURE dbo.sp_GetPendingTransactions
    @MaxAgeMinutes INT = 1440,
    @Limit INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Limit)
        t.Id,
        t.TransactionId,
        t.MobileNumber,
        o.Code AS OperatorCode,
        o.Name AS OperatorName,
        t.Amount,
        t.Status,
        t.ProviderReference,
        t.ErrorMessage,
        t.CreatedDate,
        t.UpdatedDate
    FROM dbo.RechargeTransactions t
    INNER JOIN dbo.TelecomOperators o ON t.OperatorId = o.Id
    WHERE t.Status = 'PENDING'
      AND t.CreatedDate >= DATEADD(MINUTE, -@MaxAgeMinutes, SYSUTCDATETIME())
    ORDER BY t.UpdatedDate ASC;
END
GO

-- 7. sp_ProcessStagingCards
-- Processes rows from Staging_RechargeCards for a specific batch into RechargeCards
CREATE OR ALTER PROCEDURE dbo.sp_ProcessStagingCards
    @BatchId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();
    DECLARE @TotalRows INT = 0;
    DECLARE @SuccessfulRows INT = 0;
    DECLARE @FailedRows INT = 0;
    DECLARE @DuplicateRows INT = 0;

    SELECT @TotalRows = COUNT(1) FROM dbo.Staging_RechargeCards WHERE BatchId = @BatchId;

    -- Step 1: Flag invalid operators
    UPDATE s
    SET 
        ValidationStatus = 'INVALID',
        ErrorMessage = 'Invalid or unsupported operator: ' + COALESCE(s.OperatorCode, 'NULL')
    FROM dbo.Staging_RechargeCards s
    LEFT JOIN dbo.TelecomOperators o ON s.OperatorCode = o.Code AND o.IsActive = 1
    WHERE s.BatchId = @BatchId
      AND s.ValidationStatus = 'PENDING'
      AND o.Id IS NULL;

    -- Step 2: Flag invalid denominations
    UPDATE s
    SET 
        ValidationStatus = 'INVALID',
        ErrorMessage = 'Invalid denomination amount: ' + COALESCE(s.Denomination, 'NULL')
    FROM dbo.Staging_RechargeCards s
    WHERE s.BatchId = @BatchId
      AND s.ValidationStatus = 'PENDING'
      AND (TRY_CAST(s.Denomination AS DECIMAL(18,2)) IS NULL OR TRY_CAST(s.Denomination AS DECIMAL(18,2)) <= 0);

    -- Step 3: Flag invalid expiry dates
    UPDATE s
    SET 
        ValidationStatus = 'INVALID',
        ErrorMessage = 'Invalid or expired expiry date: ' + COALESCE(s.ExpiryDateStr, 'NULL')
    FROM dbo.Staging_RechargeCards s
    WHERE s.BatchId = @BatchId
      AND s.ValidationStatus = 'PENDING'
      AND (TRY_CAST(s.ExpiryDateStr AS DATE) IS NULL);

    -- Step 4: Flag duplicates within the same staging batch (first occurrence wins)
    ;WITH StagingDuplicates AS (
        SELECT 
            Id,
            ROW_NUMBER() OVER (PARTITION BY CardNumber ORDER BY Id ASC) AS CardNumRank,
            ROW_NUMBER() OVER (PARTITION BY SerialNumber ORDER BY Id ASC) AS SerialNumRank
        FROM dbo.Staging_RechargeCards
        WHERE BatchId = @BatchId AND ValidationStatus = 'PENDING'
    )
    UPDATE s
    SET 
        ValidationStatus = 'DUPLICATE',
        ErrorMessage = CASE 
            WHEN d.CardNumRank > 1 THEN 'Duplicate CardNumber within import file'
            ELSE 'Duplicate SerialNumber within import file'
        END
    FROM dbo.Staging_RechargeCards s
    INNER JOIN StagingDuplicates d ON s.Id = d.Id
    WHERE d.CardNumRank > 1 OR d.SerialNumRank > 1;

    -- Step 5: Flag duplicates against existing RechargeCards
    UPDATE s
    SET 
        ValidationStatus = 'DUPLICATE',
        ErrorMessage = 'CardNumber already exists in database'
    FROM dbo.Staging_RechargeCards s
    INNER JOIN dbo.RechargeCards c ON s.CardNumber = c.CardNumber
    WHERE s.BatchId = @BatchId AND s.ValidationStatus = 'PENDING';

    UPDATE s
    SET 
        ValidationStatus = 'DUPLICATE',
        ErrorMessage = 'SerialNumber already exists in database'
    FROM dbo.Staging_RechargeCards s
    INNER JOIN dbo.RechargeCards c ON s.SerialNumber = c.SerialNumber
    WHERE s.BatchId = @BatchId AND s.ValidationStatus = 'PENDING';

    -- Step 6: Mark remaining valid rows
    UPDATE dbo.Staging_RechargeCards
    SET ValidationStatus = 'VALID'
    WHERE BatchId = @BatchId AND ValidationStatus = 'PENDING';

    -- Step 7: Set-based Bulk INSERT of valid cards into RechargeCards
    INSERT INTO dbo.RechargeCards (
        BatchId,
        CardNumber,
        SerialNumber,
        OperatorId,
        Denomination,
        ExpiryDate,
        Status,
        CreatedDate,
        UpdatedDate
    )
    SELECT 
        s.BatchId,
        s.CardNumber,
        s.SerialNumber,
        o.Id AS OperatorId,
        CAST(s.Denomination AS DECIMAL(18,2)),
        CAST(s.ExpiryDateStr AS DATE),
        'AVAILABLE',
        @Now,
        @Now
    FROM dbo.Staging_RechargeCards s
    INNER JOIN dbo.TelecomOperators o ON s.OperatorCode = o.Code
    WHERE s.BatchId = @BatchId AND s.ValidationStatus = 'VALID';

    SET @SuccessfulRows = @@ROWCOUNT;

    SELECT @DuplicateRows = COUNT(1) 
    FROM dbo.Staging_RechargeCards 
    WHERE BatchId = @BatchId AND ValidationStatus = 'DUPLICATE';

    SELECT @FailedRows = COUNT(1) 
    FROM dbo.Staging_RechargeCards 
    WHERE BatchId = @BatchId AND ValidationStatus = 'INVALID';

    -- Step 8: Update batch statistics
    UPDATE dbo.CardImportBatches
    SET 
        TotalRows = @TotalRows,
        SuccessfulRows = @SuccessfulRows,
        FailedRows = @FailedRows,
        DuplicateRows = @DuplicateRows,
        Status = 'COMPLETED'
    WHERE Id = @BatchId;

    -- Return Batch Summary Result Set
    SELECT 
        Id AS BatchId,
        BatchGuid,
        FileName,
        TotalRows,
        SuccessfulRows,
        FailedRows,
        DuplicateRows,
        Status,
        ImportedDate
    FROM dbo.CardImportBatches
    WHERE Id = @BatchId;

    -- Return Failed/Rejected Rows Details
    SELECT 
        RowNumber,
        CardNumber,
        SerialNumber,
        OperatorCode,
        Denomination,
        ExpiryDateStr,
        ValidationStatus,
        ErrorMessage
    FROM dbo.Staging_RechargeCards
    WHERE BatchId = @BatchId AND ValidationStatus IN ('INVALID', 'DUPLICATE')
    ORDER BY RowNumber ASC;
END
GO

-- 8. sp_ReserveCardAtomic
-- Concurrency Safe: Single atomic conditional UPDATE with OUTPUT clause
-- Never uses separate SELECT then UPDATE. If 0 rows affected, card was already claimed/unavailable.
CREATE OR ALTER PROCEDURE dbo.sp_ReserveCardAtomic
    @CardNumber VARCHAR(64),
    @TransactionId VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2 = SYSUTCDATETIME();

    -- Single Atomic Statement with OUTPUT
    UPDATE dbo.RechargeCards
    SET 
        Status = 'RESERVED',
        UsedTransactionId = @TransactionId,
        ReservedDate = @Now,
        UpdatedDate = @Now
    OUTPUT 
        INSERTED.Id,
        INSERTED.CardNumber,
        INSERTED.SerialNumber,
        INSERTED.OperatorId,
        INSERTED.Denomination,
        INSERTED.ExpiryDate,
        INSERTED.Status,
        INSERTED.UsedTransactionId,
        INSERTED.ReservedDate
    WHERE CardNumber = @CardNumber 
      AND Status = 'AVAILABLE';
END
GO

-- 9. sp_GetCardInventorySummary
CREATE OR ALTER PROCEDURE dbo.sp_GetCardInventorySummary
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        o.Code AS OperatorCode,
        o.Name AS OperatorName,
        c.Denomination,
        SUM(CASE WHEN c.Status = 'AVAILABLE' THEN 1 ELSE 0 END) AS AvailableCount,
        SUM(CASE WHEN c.Status = 'RESERVED' THEN 1 ELSE 0 END) AS ReservedCount,
        SUM(CASE WHEN c.Status = 'USED' THEN 1 ELSE 0 END) AS UsedCount,
        SUM(CASE WHEN c.Status = 'EXPIRED' THEN 1 ELSE 0 END) AS ExpiredCount,
        SUM(CASE WHEN c.Status = 'BLOCKED' THEN 1 ELSE 0 END) AS BlockedCount,
        COUNT(1) AS TotalCount
    FROM dbo.RechargeCards c
    INNER JOIN dbo.TelecomOperators o ON c.OperatorId = o.Id
    GROUP BY o.Code, o.Name, c.Denomination
    ORDER BY o.Code, c.Denomination;
END
GO
