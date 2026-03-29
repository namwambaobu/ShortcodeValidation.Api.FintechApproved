public class Transaction
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }
    public string PhoneNumber { get; set; } = default!;
    public string Shortcode { get; set; } = default!;
    public string AccountNumber { get; set; } = default!;

    public string Status { get; set; } = "Pending"; // Pending, Success, Failed
    public string? FailureReason { get; set; }

    public string ExternalReference { get; set; } = default!;

    public DateTime CreatedOn { get; set; }
}