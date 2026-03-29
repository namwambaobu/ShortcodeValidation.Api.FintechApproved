using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using ShortcodeValidation.Api.Infrastructure.Database;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly IModel _channel;

    public OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var factory = new ConnectionFactory() { HostName = "localhost" };
        var connection = factory.CreateConnection();
        _channel = connection.CreateModel();

        _channel.QueueDeclare("transactions", durable: true, exclusive: false, autoDelete: false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var messages = await db.OutboxMessages
                .Where(x => !x.Processed)
                .Take(10)
                .ToListAsync(stoppingToken);

            foreach (var msg in messages)
            {
                try
                {
                    var body = Encoding.UTF8.GetBytes(msg.Payload);

                    _channel.BasicPublish("", "transactions", null, body);

                    msg.Processed = true;

                    _logger.LogInformation("Outbox message published: {MessageId}", msg.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox publish failed");
                }
            }

            await db.SaveChangesAsync(stoppingToken);

            await Task.Delay(3000, stoppingToken);
        }
    }
}