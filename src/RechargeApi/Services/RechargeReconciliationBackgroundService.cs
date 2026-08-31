using RechargePlatform.Data.Repositories;

namespace RechargeApi.Services;

public class RechargeReconciliationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RechargeReconciliationBackgroundService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(20);

    public RechargeReconciliationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RechargeReconciliationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recharge Reconciliation Background Service started. Polling every {Interval}s.", _pollInterval.TotalSeconds);

        // Initial brief delay to allow API to boot
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoReconciliationWorkAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error occurred during background recharge reconciliation cycle.");
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Recharge Reconciliation Background Service is stopping.");
    }

    private async Task DoReconciliationWorkAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var rechargeRepo = scope.ServiceProvider.GetRequiredService<IRechargeRepository>();
        var rechargeService = scope.ServiceProvider.GetRequiredService<IRechargeService>();

        // Query pending transactions older than 10 seconds
        var pendingTxns = await rechargeRepo.GetPendingTransactionsAsync(maxAgeMinutes: 1440, limit: 50);

        if (pendingTxns.Count > 0)
        {
            _logger.LogInformation("Background reconciler found {Count} PENDING transactions. Processing...", pendingTxns.Count);

            foreach (var txn in pendingTxns)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                try
                {
                    var result = await rechargeService.ReconcileTransactionAsync(txn.TransactionId, stoppingToken);
                    _logger.LogInformation("Background reconciliation for {TransactionId}: Status={Status}, Message={Message}",
                        txn.TransactionId, result.CurrentStatus, result.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed background reconciliation for transaction {TransactionId}", txn.TransactionId);
                }
            }
        }
    }
}
