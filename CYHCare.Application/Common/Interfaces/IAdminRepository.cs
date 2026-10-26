using CYHCare.Application.DTOs;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Admin Dashboard queries (via Stored Procedures).
/// </summary>
public interface IAdminRepository
{
    /// <summary>
    /// Calls sp_Admin_LiveSummary to fetch live system KPI metrics.
    /// </summary>
    Task<AdminLiveSummaryDto?> GetLiveSummaryAsync();

    /// <summary>
    /// Calls sp_Admin_ActiveJobs to list ongoing jobs with live helper coordinates.
    /// </summary>
    Task<IEnumerable<AdminActiveJobDto>> GetActiveJobsAsync();
}
