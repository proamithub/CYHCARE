namespace CYHCare.Application.DTOs;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int AssignmentId { get; set; }
    public int WorkingMinutes { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? PaidAt { get; set; }
}

public class HelperEarningsDto
{
    public int PaymentId { get; set; }
    public int AssignmentId { get; set; }
    public int JobId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public int WorkingMinutes { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime? PaidAt { get; set; }
    public DateTime? JobCompletedAt { get; set; }
}
