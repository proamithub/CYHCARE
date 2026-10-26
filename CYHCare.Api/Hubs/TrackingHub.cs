using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using CYHCare.Application.Common.Interfaces;

namespace CYHCare.Api.Hubs;

[Authorize]
public class TrackingHub : Hub
{
    private readonly IGpsRepository _gpsRepository;
    private readonly IHelperRepository _helperRepository;
    private readonly ILogger<TrackingHub> _logger;

    public TrackingHub(
        IGpsRepository gpsRepository,
        IHelperRepository helperRepository,
        ILogger<TrackingHub> logger)
    {
        _gpsRepository = gpsRepository;
        _helperRepository = helperRepository;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        var role = GetUserRole();

        _logger.LogInformation("SignalR Connection established: {ConnectionId}, User: {UserId}, Role: {Role}", 
            Context.ConnectionId, userId, role);

        if (userId.HasValue)
        {
            // Join personal notification channel
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId.Value}");
        }

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            // Join admin live monitor group
            await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR Connection disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// User or Admin joins real-time channel for a specific job.
    /// </summary>
    public async Task JoinJob(int jobId)
    {
        var groupName = $"Job_{jobId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} joined {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// User or Admin leaves real-time channel for a specific job.
    /// </summary>
    public async Task LeaveJob(int jobId)
    {
        var groupName = $"Job_{jobId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left {GroupName}", Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Helper sends high-frequency (10-15s) GPS updates during an active assignment.
    /// Saves via sp_Gps_Insert and pushes to Job Requester and Admin live map.
    /// </summary>
    [Authorize(Roles = "Helper")]
    public async Task SendLocationUpdate(int assignmentId, int jobId, decimal lat, decimal lng, decimal? speedKmph = null)
    {
        var helperId = GetHelperId();
        _logger.LogDebug("Received location update: Assignment {AssignmentId}, Job {JobId}, Helper {HelperId}, ({Lat}, {Lng})",
            assignmentId, jobId, helperId, lat, lng);

        // 1. Save telemetry to DB via sp_Gps_Insert
        var logId = await _gpsRepository.InsertLogAsync(assignmentId, lat, lng, speedKmph);

        var payload = new
        {
            logId,
            assignmentId,
            jobId,
            helperId,
            lat,
            lng,
            speedKmph,
            recordedAt = DateTime.UtcNow
        };

        // 2. Broadcast to Requester watching this Job
        await Clients.Group($"Job_{jobId}").SendAsync("ReceiveLocationUpdate", payload);

        // 3. Broadcast to Admins watching live system map
        await Clients.Group("AdminGroup").SendAsync("ReceiveLocationUpdate", payload);
    }

    /// <summary>
    /// Helper updates location while on duty (without active assignment).
    /// </summary>
    [Authorize(Roles = "Helper")]
    public async Task SendDutyLocation(decimal lat, decimal lng)
    {
        var helperId = GetHelperId();
        if (helperId.HasValue)
        {
            await _helperRepository.UpdateLocationAsync(helperId.Value, lat, lng);

            var payload = new
            {
                helperId = helperId.Value,
                lat,
                lng,
                lastUpdate = DateTime.UtcNow
            };

            await Clients.Group("AdminGroup").SendAsync("ReceiveDutyLocation", payload);
        }
    }

    private int? GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? Context.User?.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetUserRole()
    {
        return Context.User?.FindFirst(ClaimTypes.Role)?.Value 
            ?? Context.User?.FindFirst("Role")?.Value;
    }

    private int? GetHelperId()
    {
        var claim = Context.User?.FindFirst("HelperId")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
