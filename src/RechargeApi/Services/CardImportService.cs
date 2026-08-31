using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using RechargePlatform.Common.DTOs;
using RechargePlatform.Data.Repositories;

namespace RechargeApi.Services;

public interface ICardImportService
{
    Task<CardImportResultDto> ImportCardsCsvAsync(IFormFile file, string importedBy = "SYSTEM", CancellationToken cancellationToken = default);
    Task<CardReservationResponseDto?> ReserveCardAsync(CardReservationRequestDto request);
    Task<List<CardInventoryDto>> GetInventorySummaryAsync();
    Task<List<CardBatchSummaryDto>> GetImportBatchesAsync();
}

public class CardImportService : ICardImportService
{
    private readonly ICardRepository _cardRepository;
    private readonly ILogger<CardImportService> _logger;

    public CardImportService(ICardRepository cardRepository, ILogger<CardImportService> logger)
    {
        _cardRepository = cardRepository;
        _logger = logger;
    }

    public async Task<CardImportResultDto> ImportCardsCsvAsync(IFormFile file, string importedBy = "SYSTEM", CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("No file provided or file is empty.");
        }

        _logger.LogInformation("Starting CSV card batch import for file '{FileName}', size: {Size} bytes", file.FileName, file.Length);

        var rawRows = new List<CardImportRowRaw>();
        var rowNumber = 1; // Header is row 1, data starts at row 2

        using (var reader = new StreamReader(file.OpenReadStream()))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            IgnoreBlankLines = true,
            MissingFieldFound = null,
            BadDataFound = null
        }))
        {
            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                rowNumber++;
                
                var cardNumber = csv.GetField<string>("CardNumber")?.Trim();
                var serialNumber = csv.GetField<string>("SerialNumber")?.Trim();
                var @operator = csv.GetField<string>("Operator")?.Trim();
                var denomination = csv.GetField<string>("Denomination")?.Trim();
                var expiryDate = csv.GetField<string>("ExpiryDate")?.Trim();

                rawRows.Add(new CardImportRowRaw
                {
                    RowNumber = rowNumber,
                    CardNumber = cardNumber,
                    SerialNumber = serialNumber,
                    Operator = @operator,
                    Denomination = denomination,
                    ExpiryDate = expiryDate
                });
            }
        }

        _logger.LogInformation("Parsed {Count} rows from CSV file '{FileName}'. Creating batch and bulk loading into staging...",
            rawRows.Count, file.FileName);

        // 1. Create Import Batch Record
        var (batchId, batchGuid) = await _cardRepository.CreateImportBatchAsync(file.FileName, rawRows.Count, importedBy);

        // 2. High-speed SqlBulkCopy into Staging_RechargeCards
        await _cardRepository.BulkInsertStagingCardsAsync(batchId, rawRows, cancellationToken);

        // 3. Execute set-based merge / validation stored procedure
        var result = await _cardRepository.ProcessStagingCardsAsync(batchId);

        _logger.LogInformation("Batch import {BatchId} completed: Total={Total}, Imported={Imported}, Duplicates={Duplicates}, Failed={Failed}",
            result.BatchId, result.TotalRows, result.Imported, result.Duplicates, result.Failed);

        return result;
    }

    public async Task<CardReservationResponseDto?> ReserveCardAsync(CardReservationRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.CardNumber))
            throw new ArgumentException("CardNumber is required.");

        if (string.IsNullOrWhiteSpace(request.TransactionId))
            throw new ArgumentException("TransactionId is required.");

        _logger.LogInformation("Attempting atomic card reservation: CardNumber={CardNumber}, TransactionId={TransactionId}",
            request.CardNumber, request.TransactionId);

        return await _cardRepository.ReserveCardAtomicAsync(request.CardNumber, request.TransactionId);
    }

    public async Task<List<CardInventoryDto>> GetInventorySummaryAsync()
    {
        return await _cardRepository.GetCardInventorySummaryAsync();
    }

    public async Task<List<CardBatchSummaryDto>> GetImportBatchesAsync()
    {
        return await _cardRepository.GetImportBatchesAsync();
    }
}
