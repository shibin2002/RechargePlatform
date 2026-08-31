-- ============================================================================
-- 04_seed_data.sql: Initial Seed Data
-- Telecom Prepaid Recharge Platform
-- ============================================================================

USE RechargeDb;
GO

-- 1. Seed Telecom Operators (Jio, Airtel, Vi, BSNL)
IF NOT EXISTS (SELECT 1 FROM dbo.TelecomOperators WHERE Code = 'Jio')
    INSERT INTO dbo.TelecomOperators (Code, Name, IsActive) VALUES ('Jio', 'Reliance Jio Infocomm', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.TelecomOperators WHERE Code = 'Airtel')
    INSERT INTO dbo.TelecomOperators (Code, Name, IsActive) VALUES ('Airtel', 'Bharti Airtel Limited', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.TelecomOperators WHERE Code = 'Vi')
    INSERT INTO dbo.TelecomOperators (Code, Name, IsActive) VALUES ('Vi', 'Vodafone Idea Limited', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.TelecomOperators WHERE Code = 'BSNL')
    INSERT INTO dbo.TelecomOperators (Code, Name, IsActive) VALUES ('BSNL', 'Bharat Sanchar Nigam Limited', 1);

GO

-- 2. Seed Initial System Import Batch & Sample Recharge Cards
DECLARE @BatchId INT;
DECLARE @JioId INT = (SELECT Id FROM dbo.TelecomOperators WHERE Code = 'Jio');
DECLARE @AirtelId INT = (SELECT Id FROM dbo.TelecomOperators WHERE Code = 'Airtel');
DECLARE @ViId INT = (SELECT Id FROM dbo.TelecomOperators WHERE Code = 'Vi');
DECLARE @BsnlId INT = (SELECT Id FROM dbo.TelecomOperators WHERE Code = 'BSNL');

IF NOT EXISTS (SELECT 1 FROM dbo.CardImportBatches WHERE FileName = 'SEED_INITIAL_INVENTORY.csv')
BEGIN
    INSERT INTO dbo.CardImportBatches (
        FileName,
        TotalRows,
        SuccessfulRows,
        FailedRows,
        DuplicateRows,
        ImportedBy,
        Status
    )
    VALUES (
        'SEED_INITIAL_INVENTORY.csv',
        16,
        16,
        0,
        0,
        'SYSTEM_SEED',
        'COMPLETED'
    );

    SET @BatchId = SCOPE_IDENTITY();

    -- Seed Jio Cards
    INSERT INTO dbo.RechargeCards (BatchId, CardNumber, SerialNumber, OperatorId, Denomination, ExpiryDate, Status)
    VALUES 
        (@BatchId, 'JIO-CARD-100-001', 'SN-JIO-1001', @JioId, 100.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'JIO-CARD-100-002', 'SN-JIO-1002', @JioId, 100.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'JIO-CARD-299-001', 'SN-JIO-2001', @JioId, 299.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'JIO-CARD-499-001', 'SN-JIO-3001', @JioId, 499.00, '2027-12-31', 'AVAILABLE');

    -- Seed Airtel Cards
    INSERT INTO dbo.RechargeCards (BatchId, CardNumber, SerialNumber, OperatorId, Denomination, ExpiryDate, Status)
    VALUES 
        (@BatchId, 'AIRTEL-CARD-100-001', 'SN-AIR-1001', @AirtelId, 100.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'AIRTEL-CARD-100-002', 'SN-AIR-1002', @AirtelId, 100.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'AIRTEL-CARD-299-001', 'SN-AIR-2001', @AirtelId, 299.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'AIRTEL-CARD-499-001', 'SN-AIR-3001', @AirtelId, 499.00, '2027-12-31', 'AVAILABLE');

    -- Seed Vi Cards
    INSERT INTO dbo.RechargeCards (BatchId, CardNumber, SerialNumber, OperatorId, Denomination, ExpiryDate, Status)
    VALUES 
        (@BatchId, 'VI-CARD-100-001', 'SN-VI-1001', @ViId, 100.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'VI-CARD-299-001', 'SN-VI-2001', @ViId, 299.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'VI-CARD-499-001', 'SN-VI-3001', @ViId, 499.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'VI-CARD-100-002', 'SN-VI-1002', @ViId, 100.00, '2025-01-01', 'EXPIRED');

    -- Seed BSNL Cards
    INSERT INTO dbo.RechargeCards (BatchId, CardNumber, SerialNumber, OperatorId, Denomination, ExpiryDate, Status)
    VALUES 
        (@BatchId, 'BSNL-CARD-100-001', 'SN-BSNL-1001', @BsnlId, 100.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'BSNL-CARD-299-001', 'SN-BSNL-2001', @BsnlId, 299.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'BSNL-CARD-499-001', 'SN-BSNL-3001', @BsnlId, 499.00, '2027-12-31', 'AVAILABLE'),
        (@BatchId, 'BSNL-CARD-100-002', 'SN-BSNL-1002', @BsnlId, 100.00, '2027-12-31', 'BLOCKED');
END
GO
