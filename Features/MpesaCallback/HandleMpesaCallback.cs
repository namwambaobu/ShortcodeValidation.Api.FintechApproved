using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ShortcodeValidation.Api.Entities;
using ShortcodeValidation.Api.Infrastructure.Database;

public class HandleMpesaCallback
{
    public static async Task<IResult> Handle(
        MpesaCallbackRequest request,
        AppDbContext db,
        ILogger<HandleMpesaCallback> logger)
    {
        logger.LogInformation("Received M-Pesa callback: {@Request}", request);

        // 🔒 1. Idempotency check
        var existing = await db.Transactions
            .FirstOrDefaultAsync(x => x.ExternalReference == request.Reference);

        if (existing != null)
        {
            logger.LogWarning("Duplicate callback received: {Reference}", request.Reference);
            return Results.Ok("Already processed");
        }

        // 🔍 2. Validate shortcode
        var shortcode = await db.Shortcodes
            .FirstOrDefaultAsync(x => x.Code == request.Shortcode);

        if (shortcode == null)
        {
            logger.LogError("Shortcode not found: {Shortcode}", request.Shortcode);
            return Results.BadRequest("Shortcode does not exist");
        }

        if (!shortcode.IsActive || !shortcode.IsApproved)
        {
            logger.LogWarning("Invalid shortcode state: {Shortcode}", request.Shortcode);
            return Results.BadRequest("Shortcode is not active or approved");
        }

        // 🧾 3. Create transaction
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            PhoneNumber = request.PhoneNumber,
            Shortcode = request.Shortcode,
            AccountNumber = shortcode.AccountNumber,
            ExternalReference = request.Reference,
            Status = "Pending",
            RetryCount = 0,
            MaxRetries = 3,
            CreatedOn = DateTime.UtcNow
        };

        db.Transactions.Add(transaction);

        // 📦 4. OUTBOX MESSAGE (atomic with transaction)
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Payload = JsonSerializer.Serialize(new
            {
                TransactionId = transaction.Id
            }),
            Processed = false,
            CreatedOn = DateTime.UtcNow
        };

        db.OutboxMessages.Add(outboxMessage);

        // 💾 5. Single commit (VERY IMPORTANT)
        await db.SaveChangesAsync();

        logger.LogInformation("Transaction created and queued: {TransactionId}", transaction.Id);

        // ⚡ 6. Fast response to Safaricom
        return Results.Ok("Accepted");
    }
}