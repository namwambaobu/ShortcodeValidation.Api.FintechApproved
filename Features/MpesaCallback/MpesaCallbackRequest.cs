public class MpesaCallbackRequest
{
    public decimal Amount { get; set; }
    public string PhoneNumber { get; set; } = default!;
    public string Shortcode { get; set; } = default!;
    public string Reference { get; set; } = default!;
}