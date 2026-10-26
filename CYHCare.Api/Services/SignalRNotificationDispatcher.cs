using Microsoft.AspNetCore.SignalR;
using CYHCare.Api.Hubs;
using CYHCare.Application.Common.Interfaces;

namespace CYHCare.Api.Services;

/// <summary>
/// Dispatches real-time SignalR notifications and job status transitions through TrackingHub.
/// </summary>
public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<TrackingHub> _hubContext;
    private readonly ILogger<SignalRNotificationDispatcher> _logger;

    public SignalRNotificationDispatcher(
        IHubContext<TrackingHub> hubContext,
        ILogger<SignalRNotificationDispatcher> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PushUserNotificationAsync(int userId, string message, int? jobId = null)
    {
        _logger.LogInformation("Pushing SignalR notification to User {UserId}: {Message}", userId, message);

        var payload = new
        {
            userId,
            jobId,
            message,
            timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", payload);
    }

    public async Task PushJobStatusUpdateAsync(int jobId, string newStatus, object? details = null)
    {
        _logger.LogInformation("Pushing SignalR status change for Job {JobId}: {NewStatus}", jobId, newStatus);

        var payload = new
        {
            jobId,
            status = newStatus,
            details,
            timestamp = DateTime.UtcNow
        };

        // Notify subscribers watching this specific job
        await _hubContext.Clients.Group($"Job_{jobId}").SendAsync("ReceiveJobStatusUpdate", payload);

        // Notify Admins
        await _hubContext.Clients.Group("AdminGroup").SendAsync("ReceiveJobStatusUpdate", payload);
    }

    public async Task PushAdminAlertAsync(string message, object? data = null)
    {
        _logger.LogInformation("Pushing SignalR Admin alert: {Message}", message);

        var payload = new
        {
            message,
            data,
            timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.Group("AdminGroup").SendAsync("ReceiveAdminAlert", payload);
    }
}
