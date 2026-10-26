namespace CYHCare.Domain.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int AssignmentId { get; set; }
    public int WorkingMinutes { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? PaidAt { get; set; }
}
