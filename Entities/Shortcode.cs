public class Shortcode
{
    public Guid Id { get; set; }

    public string Code { get; set; } = default!;
    public string AccountNumber { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public DateTime DateOfBirth { get; set; }

    public bool IsSupervised { get; set; }
    public bool IsActive { get; set; }
    public bool IsApproved { get; set; }

    public string BranchId { get; set; } = default!;

    public DateTime CreatedOn { get; set; }
    public string CreatedBy { get; set; } = default!;
}