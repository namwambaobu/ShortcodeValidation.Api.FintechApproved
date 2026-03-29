using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

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
        var factory = new ConnectionFactory() { HostName = "localhost" };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare("transactions", durable: true, exclusive: false, autoDelete: false);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(body);

            var message = JsonSerializer.Deserialize<Dictionary<string, Guid>>(json);
            var transactionId = message["Id"];

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var transferClient = scope.ServiceProvider.GetRequiredService<FundTransferClient>();

            var tx = await db.Transactions.FirstOrDefaultAsync(x => x.Id == transactionId);

            if (tx == null) return;

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

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Processing failed");
            }
        };

        channel.BasicConsume(queue: "transactions", autoAck: true, consumer: consumer);

        return Task.CompletedTask;
    }
}