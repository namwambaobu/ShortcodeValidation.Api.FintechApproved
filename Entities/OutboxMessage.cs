namespace ShortcodeValidation.Api.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Payload { get; set; } = default!;
    public bool Processed { get; set; }
    public DateTime CreatedOn { get; set; }
}