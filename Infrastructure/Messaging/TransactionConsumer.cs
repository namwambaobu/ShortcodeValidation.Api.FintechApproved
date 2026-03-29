using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata;
using ShortcodeValidation.Api.Features.FundTransfer;
using ShortcodeValidation.Api.Infrastructure.Database;

public class TransactionConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TransactionConsumer> _logger;
    private IConnection _connection = default!;
    private IModel _channel = default!;

    public TransactionConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<TransactionConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        InitializeRabbitMq();
    }

    private void InitializeRabbitMq()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // ✅ Main queue
        _channel.QueueDeclare(
            queue: "transactions",
            durable: true,
            exclusive: false,
            autoDelete: false);

        // ✅ Dead Letter Queue
        _channel.QueueDeclare(
            queue: "transactions_dlq",
            durable: true,
            exclusive: false,
            autoDelete: false);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (model, ea) =>
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var transferClient = scope.ServiceProvider.GetRequiredService<FundTransferClient>();

            try
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                var message = JsonSerializer.Deserialize<Dictionary<string, Guid>>(json);
                var transactionId = message!["TransactionId"];

                var tx = await db.Transactions
                    .FirstOrDefaultAsync(x => x.Id == transactionId);

                if (tx == null)
                {
                    _logger.LogWarning("Transaction not found: {TransactionId}", transactionId);

                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                // 🔒 Idempotency at processing level
                if (tx.Status == "Success")
                {
                    _logger.LogInformation("Transaction already processed: {TransactionId}", tx.Id);

                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                // 🔥 Call external API
                var response = await transferClient.TransferAsync(new FundTransferRequest
                {
                    AccountNumber = tx.AccountNumber,
                    Amount = tx.Amount,
                    Reference = tx.ExternalReference
                });

                if (response?.Status == "SUCCESS")
                {
                    tx.Status = "Success";
                    tx.FailureReason = null;

                    _logger.LogInformation("Transaction successful: {TransactionId}", tx.Id);
                }
                else
                {
                    tx.RetryCount++;

                    _logger.LogWarning("Transaction failed attempt {RetryCount}: {TransactionId}",
                        tx.RetryCount, tx.Id);

                    if (tx.RetryCount >= tx.MaxRetries)
                    {
                        tx.Status = "Failed";
                        tx.FailureReason = response?.Message ?? "Max retries reached";

                        // 🚨 Send to DLQ
                        var dlqBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
                        {
                            tx.Id
                        }));

                        _channel.BasicPublish("", "transactions_dlq", null, dlqBody);

                        _logger.LogError("Transaction moved to DLQ: {TransactionId}", tx.Id);
                    }
                }

                await db.SaveChangesAsync();

                // ✅ ACK ONLY AFTER SUCCESSFUL PROCESSING
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message");

                // ❌ Do NOT ACK → message will be retried
            }
        };

        _channel.BasicConsume(
            queue: "transactions",
            autoAck: false, // 🔥 CRITICAL
            consumer: consumer);

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}