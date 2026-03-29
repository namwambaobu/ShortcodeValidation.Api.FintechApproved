namespace ShortcodeValidation.Api.Features.FundTransfer;

public class FundTransferRequest
{
    public string AccountNumber { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Reference { get; set; } = default!;
}