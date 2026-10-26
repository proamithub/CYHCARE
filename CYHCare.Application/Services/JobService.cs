using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.Common.Utilities;
using CYHCare.Application.DTOs;

namespace CYHCare.Application.Services;

/// <summary>
/// Orchestrates complete job flow: posting, nearby search & notifications,
/// race-safe acceptance, geofenced status updates, and completion payouts.
/// </summary>
public class JobService : IJobService
{
    private readonly IJobRepository _jobRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<JobService> _logger;

    public JobService(
        IJobRepository jobRepository,
        INotificationRepository notificationRepository,
        ILogger<JobService> logger)
    {
        _jobRepository = jobRepository;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a job and notifies nearby on-duty approved helpers within 5 km.
    /// </summary>
    public async Task<JobResponseDto> CreateJobAsync(int requesterId, CreateJobRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ServiceType))
            throw new ArgumentException("Service type is required.", nameof(request.ServiceType));
        if (string.IsNullOrWhiteSpace(request.Address))
            throw new ArgumentException("Address is required.", nameof(request.Address));
        if (request.Lat == 0 || request.Lng == 0)
            throw new ArgumentException("Valid GPS coordinates are required.", nameof(request.Lat));

        _logger.LogInformation("Creating job for Requester: {RequesterId}, Service: {ServiceType}", requesterId, request.ServiceType);

        var job = await _jobRepository.CreateAsync(
            requesterId,
            request.ServiceType.Trim(),
            request.Address.Trim(),
            request.Lat,
            request.Lng,
            request.ScheduledAt == default ? DateTime.UtcNow : request.ScheduledAt
        );

        if (job == null)
            throw new InvalidOperationException("Failed to create job.");

        // Find nearby helpers within 5km radius to dispatch notifications
        try
        {
            var nearbyHelpers = await _jobRepository.GetNearbyHelpersAsync(job.Lat, job.Lng, 5.0m);
            foreach (var helper in nearbyHelpers)
            {
                await _notificationRepository.CreateAsync(
                    helper.UserId,
                    job.JobId,
                    $"New {job.ServiceType} job available near you ({helper.DistanceKm} km away) at {job.Address}."
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notifications to nearby helpers for JobId {JobId}", job.JobId);
        }

        return new JobResponseDto
        {
            JobId = job.JobId,
            RequesterId = job.RequesterId,
            ServiceType = job.ServiceType,
            Address = job.Address,
            Lat = job.Lat,
            Lng = job.Lng,
            ScheduledAt = job.ScheduledAt,
            Status = job.Status,
            CreatedAt = job.CreatedAt
        };
    }

    /// <summary>
    /// Retrieves a single job's full details.
    /// </summary>
    public async Task<JobResponseDto> GetJobByIdAsync(int jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null)
            throw new KeyNotFoundException($"Job with ID {jobId} not found.");

        return job;
    }

    /// <summary>
    /// Finds nearby helpers within the specified radius using Haversine formula.
    /// </summary>
    public async Task<IEnumerable<NearbyHelperDto>> GetNearbyHelpersAsync(decimal jobLat, decimal jobLng, decimal radiusKm = 5.0m)
    {
        return await _jobRepository.GetNearbyHelpersAsync(jobLat, jobLng, radiusKm);
    }

    /// <summary>
    /// Atomically accepts a job by a verified helper and sends notification to requester.
    /// </summary>
    public async Task<AcceptJobResponseDto> AcceptJobAsync(int jobId, int helperId)
    {
        _logger.LogInformation("Helper {HelperId} accepting Job {JobId}", helperId, jobId);

        var result = await _jobRepository.AcceptAsync(jobId, helperId);
        if (result == null)
            throw new InvalidOperationException("Failed to accept job.");

        // Notify requester that a helper is assigned
        try
        {
            await _notificationRepository.CreateAsync(
                result.RequesterId,
                jobId,
                "A verified helper has accepted your job request and will be on the way shortly."
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send acceptance notification for JobId {JobId}", jobId);
        }

        return result;
    }

    /// <summary>
    /// Updates status with transition rules and enforces 100-meter geofence check on 'Arrived'.
    /// </summary>
    public async Task<JobResponseDto> UpdateJobStatusAsync(int jobId, int helperId, UpdateJobStatusRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.NewStatus))
            throw new ArgumentException("NewStatus is required.", nameof(request.NewStatus));

        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job == null)
            throw new KeyNotFoundException($"Job with ID {jobId} not found.");

        // 100 m Geofence check when transitioning to 'Arrived'
        if (request.NewStatus.Equals("Arrived", StringComparison.OrdinalIgnoreCase))
        {
            if (!request.CurrentLat.HasValue || !request.CurrentLng.HasValue)
            {
                throw new ArgumentException("Current GPS coordinates (CurrentLat and CurrentLng) are required to verify arrival at the job location.");
            }

            var distanceMeters = GeofenceHelper.CalculateDistanceMeters(
                request.CurrentLat.Value,
                request.CurrentLng.Value,
                job.Lat,
                job.Lng
            );

            _logger.LogInformation("Geofence check for Job {JobId}: Helper is {Meters}m away from target", jobId, Math.Round(distanceMeters, 1));

            if (distanceMeters > 100.0)
            {
                throw new InvalidOperationException($"Arrival rejected: You are {Math.Round(distanceMeters, 1)} meters away from the job address. You must be within 100 meters to mark Arrived.");
            }
        }

        var updated = await _jobRepository.UpdateStatusAsync(jobId, request.NewStatus, helperId);
        if (updated == null)
            throw new InvalidOperationException("Failed to update job status.");

        // Send notifications on key state changes
        try
        {
            if (request.NewStatus.Equals("Arrived", StringComparison.OrdinalIgnoreCase))
            {
                await _notificationRepository.CreateAsync(job.RequesterId, jobId, "Your helper has arrived at the destination.");
            }
            else if (request.NewStatus.Equals("InProgress", StringComparison.OrdinalIgnoreCase))
            {
                await _notificationRepository.CreateAsync(job.RequesterId, jobId, "Your assistance session has started.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send status notification for JobId {JobId}", jobId);
        }

        return updated;
    }

    /// <summary>
    /// Completes the job, calculates working minutes and payment, and issues notification.
    /// </summary>
    public async Task<CompleteJobResponseDto> CompleteJobAsync(int jobId, int helperId)
    {
        _logger.LogInformation("Completing Job {JobId} by Helper {HelperId}", jobId, helperId);

        var result = await _jobRepository.CompleteAsync(jobId, helperId);
        if (result == null)
            throw new InvalidOperationException("Failed to complete job.");

        // Notify requester with calculated payout amount
        try
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job != null)
            {
                await _notificationRepository.CreateAsync(
                    job.RequesterId,
                    jobId,
                    $"Job completed! Total duration: {result.WorkingMinutes} minutes. Amount: ₹{result.Amount:N2}."
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send completion notification for JobId {JobId}", jobId);
        }

        return result;
    }

    /// <summary>
    /// Lists all jobs created by a requester.
    /// </summary>
    public async Task<IEnumerable<JobResponseDto>> GetJobsByUserAsync(int requesterId)
    {
        return await _jobRepository.GetByUserAsync(requesterId);
    }

    /// <summary>
    /// Lists all jobs assigned to a helper.
    /// </summary>
    public async Task<IEnumerable<JobResponseDto>> GetJobsByHelperAsync(int helperId)
    {
        return await _jobRepository.GetByHelperAsync(helperId);
    }
}
