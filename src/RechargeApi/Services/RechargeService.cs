using System.Text.RegularExpressions;
using RechargePlatform.Common.Constants;
using RechargePlatform.Common.DTOs;
using RechargePlatform.Data.Models;
using RechargePlatform.Data.Repositories;

namespace RechargeApi.Services;

public interface IRechargeService
{
    Task<RechargeResponseDto> ProcessRechargeAsync(RechargeRequestDto request, CancellationToken cancellationToken = default);
    Task<RechargeResponseDto?> GetTransactionAsync(string transactionId);
    Task<PagedResult<RechargeResponseDto>> GetFilteredTransactionsAsync(TransactionFilterDto filter);
    Task<ReconcileResponseDto> ReconcileTransactionAsync(string transactionId, CancellationToken cancellationToken = default);
}

public class RechargeService : IRechargeService
{
    private readonly IRechargeRepository _rechargeRepository;
    private readonly IProviderClient _providerClient;
    private readonly ILogger<RechargeService> _logger;

    private static readonly Regex MobileRegex = new(@"^[6-9]\d{9}$", RegexOptions.Compiled);

    public RechargeService(
        IRechargeRepository rechargeRepository,
        IProviderClient providerClient,
        ILogger<RechargeService> logger)
    {
        _rechargeRepository = rechargeRepository;
        _providerClient = providerClient;
        _logger = logger;
    }

    public async Task<RechargeResponseDto> ProcessRechargeAsync(RechargeRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(request.TransactionId))
            throw new ArgumentException("TransactionId is required.");

        if (string.IsNullOrWhiteSpace(request.MobileNumber) || !MobileRegex.IsMatch(request.MobileNumber))
            throw new ArgumentException("Invalid mobile number. Must be a 10-digit Indian mobile number starting with 6, 7, 8, or 9.");

        if (!SupportedOperators.IsValid(request.Operator))
            throw new ArgumentException($"Unsupported operator '{request.Operator}'. Must be one of: Jio, Airtel, Vi, BSNL.");

        if (request.Amount <= 0 || request.Amount > 50000)
            throw new ArgumentException("Amount must be a positive value between 1 and 50000 INR.");

        _logger.LogInformation("Processing recharge request: TransactionId={TransactionId}, Mobile={Mobile}, Operator={Operator}, Amount={Amount}",
            request.TransactionId, request.MobileNumber, request.Operator, request.Amount);

        // 2. Direct INSERT relying on SQL Unique Constraint (Catch 2627/2601)
        // sp_CreateRechargeTransaction sets initial state to PROCESSING and COMMITS immediately.
        var initialEntity = await _rechargeRepository.CreateTransactionAsync(
            request.TransactionId,
            request.MobileNumber,
            request.Operator,
            request.Amount
        );

        // If duplicate key was caught by stored procedure:
        if (initialEntity.IsDuplicate)
        {
            _logger.LogWarning("Duplicate transaction detected for TransactionId={TransactionId}. Existing status: {Status}. Returning existing record without re-calling provider.",
                request.TransactionId, initialEntity.Status);

            var (existingTxn, history) = await _rechargeRepository.GetByTransactionIdAsync(request.TransactionId);
            return MapToDto(existingTxn ?? initialEntity, history, isDuplicate: true);
        }

        // 3. ZERO DB TRANSACTION HELD OPEN OVER EXTERNAL I/O!
        // At this exact point:
        // - Initial DB transaction is committed and closed.
        // - Row exists in DB with Status = 'PROCESSING'.
        // - No locks or connections are held on SQL Server.
        // Now we make the external HTTP provider call:
        var (providerSuccess, providerResp, errorMessage, isTimeout) = await _providerClient.RechargeAsync(
            request.TransactionId,
            request.MobileNumber,
            request.Operator,
            request.Amount,
            cancellationToken
        );

        // 4. Open a NEW short DB transaction to record the final result
        RechargeTransactionEntity finalEntity;

        if (providerSuccess && providerResp?.Status == TransactionStatuses.Success)
        {
            _logger.LogInformation("Recharge SUCCESS for TransactionId={TransactionId}, ProviderRef={ProviderRef}",
                request.TransactionId, providerResp.ProviderReference);

            finalEntity = await _rechargeRepository.UpdateStatusAsync(
                request.TransactionId,
                TransactionStatuses.Success,
                providerResp.ProviderReference,
                null,
                "Provider recharge completed successfully."
            );
        }
        else if (isTimeout)
        {
            // On timeout or connection drop -> set to PENDING (do NOT retry automatically)
            _logger.LogWarning("Recharge call timed out or network dropped for TransactionId={TransactionId}. Setting status to PENDING for reconciliation.",
                request.TransactionId);

            finalEntity = await _rechargeRepository.UpdateStatusAsync(
                request.TransactionId,
                TransactionStatuses.Pending,
                null,
                errorMessage ?? "Provider timeout or network drop. Awaiting reconciliation.",
                "Transitioned to PENDING due to provider communication timeout/drop."
            );
        }
        else
        {
            // Provider explicit failure or HTTP 500 error
            _logger.LogWarning("Recharge FAILED for TransactionId={TransactionId}: {ErrorMessage}",
                request.TransactionId, errorMessage ?? providerResp?.ErrorMessage);

            finalEntity = await _rechargeRepository.UpdateStatusAsync(
                request.TransactionId,
                TransactionStatuses.Failed,
                providerResp?.ProviderReference,
                errorMessage ?? providerResp?.ErrorMessage ?? "Provider recharge failed.",
                "Provider returned failure status."
            );
        }

        // 5. Fetch full history and return response
        var (_, fullHistory) = await _rechargeRepository.GetByTransactionIdAsync(request.TransactionId);
        return MapToDto(finalEntity, fullHistory, isDuplicate: false);
    }

    public async Task<RechargeResponseDto?> GetTransactionAsync(string transactionId)
    {
        var (txn, history) = await _rechargeRepository.GetByTransactionIdAsync(transactionId);
        return txn == null ? null : MapToDto(txn, history);
    }

    public async Task<PagedResult<RechargeResponseDto>> GetFilteredTransactionsAsync(TransactionFilterDto filter)
    {
        var pagedEntities = await _rechargeRepository.GetFilteredTransactionsAsync(filter);

        return new PagedResult<RechargeResponseDto>
        {
            TotalCount = pagedEntities.TotalCount,
            PageNumber = pagedEntities.PageNumber,
            PageSize = pagedEntities.PageSize,
            Items = pagedEntities.Items.Select(e => MapToDto(e, null)).ToList()
        };
    }

    public async Task<ReconcileResponseDto> ReconcileTransactionAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting reconciliation for TransactionId={TransactionId}", transactionId);

        var (txn, _) = await _rechargeRepository.GetByTransactionIdAsync(transactionId);
        if (txn == null)
        {
            return new ReconcileResponseDto
            {
                TransactionId = transactionId,
                Message = "Transaction not found in database.",
                ReconciledAt = DateTime.UtcNow
            };
        }

        // If already in a terminal state, return current status
        if (txn.Status == TransactionStatuses.Success || txn.Status == TransactionStatuses.Failed)
        {
            return new ReconcileResponseDto
            {
                TransactionId = transactionId,
                PreviousStatus = txn.Status,
                CurrentStatus = txn.Status,
                ProviderReference = txn.ProviderReference,
                Message = $"Transaction is already in terminal status '{txn.Status}'.",
                ReconciledAt = DateTime.UtcNow
            };
        }

        // Query the provider's status enquiry source of truth
        var providerStatus = await _providerClient.GetStatusAsync(transactionId, cancellationToken);
        var previousStatus = txn.Status;

        if (providerStatus != null)
        {
            if (providerStatus.Status == TransactionStatuses.Success)
            {
                _logger.LogInformation("Reconciliation: Provider confirmed SUCCESS for TransactionId={TransactionId}, ProviderRef={ProviderRef}",
                    transactionId, providerStatus.ProviderReference);

                await _rechargeRepository.UpdateStatusAsync(
                    transactionId,
                    TransactionStatuses.Success,
                    providerStatus.ProviderReference,
                    null,
                    "Reconciled via Provider Status Enquiry: SUCCESS"
                );

                return new ReconcileResponseDto
                {
                    TransactionId = transactionId,
                    PreviousStatus = previousStatus,
                    CurrentStatus = TransactionStatuses.Success,
                    ProviderReference = providerStatus.ProviderReference,
                    Message = "Successfully reconciled: Transaction confirmed SUCCESS by provider.",
                    ReconciledAt = DateTime.UtcNow
                };
            }
            else if (providerStatus.Status == TransactionStatuses.Failed)
            {
                _logger.LogInformation("Reconciliation: Provider confirmed FAILED for TransactionId={TransactionId}", transactionId);

                await _rechargeRepository.UpdateStatusAsync(
                    transactionId,
                    TransactionStatuses.Failed,
                    providerStatus.ProviderReference,
                    providerStatus.ErrorMessage,
                    "Reconciled via Provider Status Enquiry: FAILED"
                );

                return new ReconcileResponseDto
                {
                    TransactionId = transactionId,
                    PreviousStatus = previousStatus,
                    CurrentStatus = TransactionStatuses.Failed,
                    ProviderReference = providerStatus.ProviderReference,
                    Message = $"Reconciled: Transaction marked FAILED by provider ({providerStatus.ErrorMessage}).",
                    ReconciledAt = DateTime.UtcNow
                };
            }
        }

        return new ReconcileResponseDto
        {
            TransactionId = transactionId,
            PreviousStatus = previousStatus,
            CurrentStatus = previousStatus,
            ProviderReference = txn.ProviderReference,
            Message = "Reconciliation attempted: Provider still has no definitive terminal record.",
            ReconciledAt = DateTime.UtcNow
        };
    }

    private static RechargeResponseDto MapToDto(RechargeTransactionEntity entity, List<TransactionStatusHistoryEntity>? history, bool isDuplicate = false)
    {
        return new RechargeResponseDto
        {
            Id = entity.Id,
            TransactionId = entity.TransactionId,
            MobileNumber = entity.MobileNumber,
            Operator = entity.OperatorCode,
            OperatorName = entity.OperatorName,
            Amount = entity.Amount,
            Status = entity.Status,
            ProviderReference = entity.ProviderReference,
            ErrorMessage = entity.ErrorMessage,
            CreatedDate = entity.CreatedDate,
            UpdatedDate = entity.UpdatedDate,
            IsDuplicate = isDuplicate,
            History = history?.Select(h => new StatusHistoryDto
            {
                Id = h.Id,
                TransactionId = h.TransactionId,
                OldStatus = h.OldStatus,
                NewStatus = h.NewStatus,
                Remarks = h.Remarks,
                CreatedDate = h.CreatedDate
            }).ToList()
        };
    }
}
