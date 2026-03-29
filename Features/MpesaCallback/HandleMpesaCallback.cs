using Microsoft.EntityFrameworkCore;
using ShortcodeValidation.Api.Infrastructure.Database;

public class HandleMpesaCallback
{
    public static async Task<IResult> Handle(
        MpesaCallbackRequest request,
        AppDbContext db,
        ILogger<HandleMpesaCallback> logger)
    {
        var existing = await db.Transactions
            .FirstOrDefaultAsync(x => x.ExternalReference == request.Reference);

        if (existing != null)
            return Results.Ok("Already processed");

        var shortcode = await db.Shortcodes
            .FirstOrDefaultAsync(x => x.Code == request.Shortcode);

        if (shortcode == null)
            return Results.BadRequest("Shortcode does not exist");

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            Amount = request.Amount,
            PhoneNumber = request.PhoneNumber,
            Shortcode = request.Shortcode,
            AccountNumber = shortcode.AccountNumber,
            ExternalReference = request.Reference,
            Status = "Pending",
            CreatedOn = DateTime.UtcNow
        };

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();


        return Results.Ok("Accepted");
    }
}