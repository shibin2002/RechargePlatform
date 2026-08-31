-- ============================================================================
-- 01_schema.sql: Database and Table Schema Creation
-- Telecom Prepaid Recharge Platform
-- ============================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'RechargeDb')
BEGIN
    CREATE DATABASE RechargeDb;
END
GO

USE RechargeDb;
GO

-- 1. TelecomOperators Table
IF OBJECT_ID(N'dbo.TelecomOperators', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TelecomOperators (
        Id INT IDENTITY(1,1) NOT NULL,
        Code VARCHAR(20) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_TelecomOperators PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_TelecomOperators_Code UNIQUE (Code)
    );
END
GO

-- 2. CardImportBatches Table
IF OBJECT_ID(N'dbo.CardImportBatches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CardImportBatches (
        Id INT IDENTITY(1,1) NOT NULL,
        BatchGuid UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        FileName NVARCHAR(255) NOT NULL,
        TotalRows INT NOT NULL DEFAULT 0,
        SuccessfulRows INT NOT NULL DEFAULT 0,
        FailedRows INT NOT NULL DEFAULT 0,
        DuplicateRows INT NOT NULL DEFAULT 0,
        ImportedBy NVARCHAR(100) NOT NULL DEFAULT 'SYSTEM',
        ImportedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Status VARCHAR(20) NOT NULL DEFAULT 'PROCESSING',
        CONSTRAINT PK_CardImportBatches PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_CardImportBatches_BatchGuid UNIQUE (BatchGuid)
    );
END
GO

-- 3. Staging_RechargeCards Table (for high-throughput SqlBulkCopy)
IF OBJECT_ID(N'dbo.Staging_RechargeCards', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Staging_RechargeCards (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        BatchId INT NOT NULL,
        RowNumber INT NOT NULL,
        CardNumber VARCHAR(64) NULL,
        SerialNumber VARCHAR(64) NULL,
        OperatorCode VARCHAR(50) NULL,
        Denomination VARCHAR(50) NULL,
        ExpiryDateStr VARCHAR(50) NULL,
        ValidationStatus VARCHAR(20) NOT NULL DEFAULT 'PENDING',
        ErrorMessage NVARCHAR(500) NULL,
        CONSTRAINT PK_Staging_RechargeCards PRIMARY KEY CLUSTERED (Id)
    );
END
GO

-- 4. RechargeCards Table
IF OBJECT_ID(N'dbo.RechargeCards', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RechargeCards (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        BatchId INT NULL,
        CardNumber VARCHAR(64) NOT NULL,
        SerialNumber VARCHAR(64) NOT NULL,
        OperatorId INT NOT NULL,
        Denomination DECIMAL(18,2) NOT NULL,
        ExpiryDate DATE NOT NULL,
        Status VARCHAR(20) NOT NULL DEFAULT 'AVAILABLE', -- AVAILABLE, RESERVED, USED, EXPIRED, BLOCKED
        UsedTransactionId VARCHAR(50) NULL,
        ReservedDate DATETIME2 NULL,
        UsedDate DATETIME2 NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_RechargeCards PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RechargeCards_BatchId FOREIGN KEY (BatchId) REFERENCES dbo.CardImportBatches(Id),
        CONSTRAINT FK_RechargeCards_OperatorId FOREIGN KEY (OperatorId) REFERENCES dbo.TelecomOperators(Id)
    );
END
GO

-- 5. RechargeTransactions Table
IF OBJECT_ID(N'dbo.RechargeTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RechargeTransactions (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        TransactionId VARCHAR(50) NOT NULL,
        MobileNumber VARCHAR(15) NOT NULL,
        OperatorId INT NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Status VARCHAR(20) NOT NULL DEFAULT 'NEW', -- NEW, PROCESSING, SUCCESS, FAILED, PENDING
        ProviderReference VARCHAR(100) NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_RechargeTransactions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RechargeTransactions_OperatorId FOREIGN KEY (OperatorId) REFERENCES dbo.TelecomOperators(Id)
    );
END
GO

-- 6. TransactionStatusHistory Table
IF OBJECT_ID(N'dbo.TransactionStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransactionStatusHistory (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        TransactionId VARCHAR(50) NOT NULL,
        OldStatus VARCHAR(20) NULL,
        NewStatus VARCHAR(20) NOT NULL,
        Remarks NVARCHAR(500) NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_TransactionStatusHistory PRIMARY KEY CLUSTERED (Id)
    );
END
GO

-- 7. ProviderRequests Table (Audit logging for outbound calls)
IF OBJECT_ID(N'dbo.ProviderRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProviderRequests (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        TransactionId VARCHAR(50) NOT NULL,
        RequestUrl NVARCHAR(500) NOT NULL,
        RequestBody NVARCHAR(MAX) NULL,
        SentDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ProviderRequests PRIMARY KEY CLUSTERED (Id)
    );
END
GO

-- 8. ProviderResponses Table (Audit logging for inbound responses)
IF OBJECT_ID(N'dbo.ProviderResponses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProviderResponses (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        TransactionId VARCHAR(50) NOT NULL,
        HttpStatusCode INT NULL,
        ResponseBody NVARCHAR(MAX) NULL,
        LatencyMs INT NULL,
        ReceivedDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        ErrorMessage NVARCHAR(MAX) NULL,
        CONSTRAINT PK_ProviderResponses PRIMARY KEY CLUSTERED (Id)
    );
END
GO
