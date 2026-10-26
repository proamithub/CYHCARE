namespace CYHCare.Domain.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public int? JobId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
