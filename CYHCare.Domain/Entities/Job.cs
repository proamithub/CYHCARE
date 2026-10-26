namespace CYHCare.Domain.Entities;

public class Job
{
    public int JobId { get; set; }
    public int RequesterId { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lng { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Joined fields
    public string? RequesterName { get; set; }
    public string? RequesterPhone { get; set; }
}
