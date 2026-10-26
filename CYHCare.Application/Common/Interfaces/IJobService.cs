using CYHCare.Application.DTOs;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Service interface for Job lifecycle orchestration, matching, geofencing, and notifications.
/// </summary>
public interface IJobService
{
    /// <summary>
    /// Creates a job and notifies nearby on-duty approved helpers.
    /// </summary>
    Task<JobResponseDto> CreateJobAsync(int requesterId, CreateJobRequestDto request);

    /// <summary>
    /// Retrieves a single job's full details.
    /// </summary>
    Task<JobResponseDto> GetJobByIdAsync(int jobId);

    /// <summary>
    /// Finds nearby helpers within the specified radius.
    /// </summary>
    Task<IEnumerable<NearbyHelperDto>> GetNearbyHelpersAsync(decimal jobLat, decimal jobLng, decimal radiusKm = 5.0m);

    /// <summary>
    /// Atomically accepts a job by a verified helper and sends notification to requester.
    /// </summary>
    Task<AcceptJobResponseDto> AcceptJobAsync(int jobId, int helperId);

    /// <summary>
    /// Updates status (OnTheWay, Arrived with 100m geofence validation, InProgress).
    /// </summary>
    Task<JobResponseDto> UpdateJobStatusAsync(int jobId, int helperId, UpdateJobStatusRequestDto request);

    /// <summary>
    /// Completes the job, calculates working minutes and payment, and issues notification.
    /// </summary>
    Task<CompleteJobResponseDto> CompleteJobAsync(int jobId, int helperId);

    /// <summary>
    /// Lists all jobs created by a requester.
    /// </summary>
    Task<IEnumerable<JobResponseDto>> GetJobsByUserAsync(int requesterId);

    /// <summary>
    /// Lists all jobs assigned to a helper.
    /// </summary>
    Task<IEnumerable<JobResponseDto>> GetJobsByHelperAsync(int helperId);
}
