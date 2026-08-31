-- ============================================================================
-- 03_indexes_constraints.sql: Indexes and Constraints
-- Telecom Prepaid Recharge Platform
-- ============================================================================

USE RechargeDb;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. Unique Constraints & Indexes on RechargeTransactions
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_RechargeTransactions_TransactionId' AND object_id = OBJECT_ID(N'dbo.RechargeTransactions'))
BEGIN
    ALTER TABLE dbo.RechargeTransactions
    ADD CONSTRAINT UQ_RechargeTransactions_TransactionId UNIQUE NONCLUSTERED (TransactionId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RechargeTransactions_MobileNumber' AND object_id = OBJECT_ID(N'dbo.RechargeTransactions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RechargeTransactions_MobileNumber
    ON dbo.RechargeTransactions (MobileNumber)
    INCLUDE (Status, Amount, OperatorId, CreatedDate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RechargeTransactions_Status_CreatedDate' AND object_id = OBJECT_ID(N'dbo.RechargeTransactions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RechargeTransactions_Status_CreatedDate
    ON dbo.RechargeTransactions (Status, CreatedDate)
    INCLUDE (TransactionId, MobileNumber, OperatorId, Amount, ProviderReference);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RechargeTransactions_Operator_CreatedDate' AND object_id = OBJECT_ID(N'dbo.RechargeTransactions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RechargeTransactions_Operator_CreatedDate
    ON dbo.RechargeTransactions (OperatorId, CreatedDate)
    INCLUDE (Status, Amount);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RechargeTransactions_ProviderReference' AND object_id = OBJECT_ID(N'dbo.RechargeTransactions'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RechargeTransactions_ProviderReference
    ON dbo.RechargeTransactions (ProviderReference)
    WHERE ProviderReference IS NOT NULL;
END
GO

-- 2. Unique Constraints & Indexes on RechargeCards
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_RechargeCards_CardNumber' AND object_id = OBJECT_ID(N'dbo.RechargeCards'))
BEGIN
    ALTER TABLE dbo.RechargeCards
    ADD CONSTRAINT UQ_RechargeCards_CardNumber UNIQUE NONCLUSTERED (CardNumber);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_RechargeCards_SerialNumber' AND object_id = OBJECT_ID(N'dbo.RechargeCards'))
BEGIN
    ALTER TABLE dbo.RechargeCards
    ADD CONSTRAINT UQ_RechargeCards_SerialNumber UNIQUE NONCLUSTERED (SerialNumber);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RechargeCards_Operator_Denom_Status' AND object_id = OBJECT_ID(N'dbo.RechargeCards'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RechargeCards_Operator_Denom_Status
    ON dbo.RechargeCards (OperatorId, Denomination, Status)
    INCLUDE (CardNumber, ExpiryDate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RechargeCards_Status_ExpiryDate' AND object_id = OBJECT_ID(N'dbo.RechargeCards'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RechargeCards_Status_ExpiryDate
    ON dbo.RechargeCards (Status, ExpiryDate);
END
GO

-- 3. Indexes on TransactionStatusHistory & Auditing
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TransactionStatusHistory_TransactionId' AND object_id = OBJECT_ID(N'dbo.TransactionStatusHistory'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TransactionStatusHistory_TransactionId
    ON dbo.TransactionStatusHistory (TransactionId, CreatedDate);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProviderRequests_TransactionId' AND object_id = OBJECT_ID(N'dbo.ProviderRequests'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProviderRequests_TransactionId
    ON dbo.ProviderRequests (TransactionId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProviderResponses_TransactionId' AND object_id = OBJECT_ID(N'dbo.ProviderResponses'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProviderResponses_TransactionId
    ON dbo.ProviderResponses (TransactionId);
END
GO

-- 4. Staging Index
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Staging_RechargeCards_Batch_Status' AND object_id = OBJECT_ID(N'dbo.Staging_RechargeCards'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_Staging_RechargeCards_Batch_Status
    ON dbo.Staging_RechargeCards (BatchId, ValidationStatus)
    INCLUDE (RowNumber, CardNumber, SerialNumber);
END
GO
