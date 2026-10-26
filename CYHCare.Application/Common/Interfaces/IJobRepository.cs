using CYHCare.Application.DTOs;
using CYHCare.Domain.Entities;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Job operations (via Stored Procedures).
/// </summary>
public interface IJobRepository
{
    /// <summary>
    /// Calls sp_Job_Create to post a new assistance request.
    /// </summary>
    Task<Job?> CreateAsync(int requesterId, string serviceType, string address, decimal lat, decimal lng, DateTime scheduledAt);

    /// <summary>
    /// Calls sp_Job_GetNearbyHelpers to locate active helpers within given radius using Haversine formula.
    /// </summary>
    Task<IEnumerable<NearbyHelperDto>> GetNearbyHelpersAsync(decimal jobLat, decimal jobLng, decimal radiusKm = 5.0m);

    /// <summary>
    /// Calls sp_Job_Accept to atomically assign job to helper.
    /// </summary>
    Task<AcceptJobResponseDto?> AcceptAsync(int jobId, int helperId);

    /// <summary>
    /// Calls sp_Job_UpdateStatus to transition job through lifecycle.
    /// </summary>
    Task<JobResponseDto?> UpdateStatusAsync(int jobId, string newStatus, int? helperId = null);

    /// <summary>
    /// Calls sp_Job_Complete to complete job and compute payment.
    /// </summary>
    Task<CompleteJobResponseDto?> CompleteAsync(int jobId, int helperId);

    /// <summary>
    /// Calls sp_Job_GetByUser to fetch jobs requested by user.
    /// </summary>
    Task<IEnumerable<JobResponseDto>> GetByUserAsync(int requesterId);

    /// <summary>
    /// Calls sp_Job_GetByHelper to fetch jobs assigned to helper.
    /// </summary>
    Task<IEnumerable<JobResponseDto>> GetByHelperAsync(int helperId);
}
