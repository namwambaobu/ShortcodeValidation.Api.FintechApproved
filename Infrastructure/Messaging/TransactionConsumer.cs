using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

public class TransactionConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransactionConsumer> _logger;

    public TransactionConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<TransactionConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {

        consumer.Received += async (model, ea) =>
        {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                var message = JsonSerializer.Deserialize<Dictionary<string, Guid>>(json);




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
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
            }
        };


        return Task.CompletedTask;
    }
}