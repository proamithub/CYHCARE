namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Repository interface for In-App Notifications (via Stored Procedures).
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Calls sp_Notification_Create to store a notification for a user.
    /// </summary>
    Task<int> CreateAsync(int userId, int? jobId, string message);
}
