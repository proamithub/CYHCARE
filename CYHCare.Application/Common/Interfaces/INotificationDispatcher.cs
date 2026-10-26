namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Service interface for dispatching real-time SignalR notifications and live events.
/// </summary>
public interface INotificationDispatcher
{
    /// <summary>
    /// Pushes a real-time notification to a specific user.
    /// </summary>
    Task PushUserNotificationAsync(int userId, string message, int? jobId = null);

    /// <summary>
    /// Pushes a job status update to all subscribers watching the job and admin live dashboard.
    /// </summary>
    Task PushJobStatusUpdateAsync(int jobId, string newStatus, object? details = null);

    /// <summary>
    /// Pushes a live map alert to all connected admin clients.
    /// </summary>
    Task PushAdminAlertAsync(string message, object? data = null);
}
