using CYHCare.Application.Common.Interfaces;

namespace CYHCare.Application.Services;

/// <summary>
/// Fallback no-op dispatcher when real-time transport is not configured.
/// </summary>
public class NullNotificationDispatcher : INotificationDispatcher
{
    public Task PushUserNotificationAsync(int userId, string message, int? jobId = null) => Task.CompletedTask;
    public Task PushJobStatusUpdateAsync(int jobId, string newStatus, object? details = null) => Task.CompletedTask;
    public Task PushAdminAlertAsync(string message, object? data = null) => Task.CompletedTask;
}
