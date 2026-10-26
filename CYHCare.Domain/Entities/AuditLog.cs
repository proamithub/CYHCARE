namespace CYHCare.Domain.Entities;

public class AuditLog
{
    public long AuditId { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
