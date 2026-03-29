using Microsoft.EntityFrameworkCore;
using ShortcodeValidation.Api.Features.FundTransfer;
using ShortcodeValidation.Api.Infrastructure.Database;

public class TransactionProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransactionProcessor> _logger;

    public TransactionProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<TransactionProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var transferClient = scope.ServiceProvider.GetRequiredService<FundTransferClient>();

            var pendingTransactions = await db.Transactions
                .Where(x => x.Status == "Pending")
                .Take(10)
                .ToListAsync(stoppingToken);

            foreach (var tx in pendingTransactions)
            {
                try
                {
                    var response = await transferClient.TransferAsync(new FundTransferRequest
                    {
                        AccountNumber = tx.AccountNumber,
                        Amount = tx.Amount,
                        Reference = tx.ExternalReference
                    });

                    if (response?.Status == "SUCCESS")
                    {
                        tx.Status = "Success";
                    }
                    else
                    {
                        tx.Status = "Failed";
                        tx.FailureReason = response?.Message;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing transaction {Id}", tx.Id);
                }
            }

            await Task.Delay(5000, stoppingToken); // poll every 5s
        }
    }
}