namespace CYHCare.Application.DTOs;

public class NotificationDto
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public int? JobId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
}

public class CreateNotificationDto
{
    public int UserId { get; set; }
    public int? JobId { get; set; }
    public string Message { get; set; } = string.Empty;
}
