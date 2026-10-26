using CYHCare.Application.DTOs;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Repository interface for GPS breadcrumbs and route tracking (via Stored Procedures).
/// </summary>
public interface IGpsRepository
{
    /// <summary>
    /// Calls sp_Gps_Insert to record a GPS waypoint.
    /// </summary>
    Task<long> InsertLogAsync(int assignmentId, decimal lat, decimal lng, decimal? speedKmph = null);

    /// <summary>
    /// Calls sp_Gps_GetRoute to retrieve recorded trail for an assignment.
    /// </summary>
    Task<IEnumerable<GpsPointDto>> GetRouteAsync(int assignmentId);

    /// <summary>
    /// Calls sp_Gps_PurgeOld to clean up old telemetry.
    /// </summary>
    Task<int> PurgeOldAsync(int days = 30);
}
