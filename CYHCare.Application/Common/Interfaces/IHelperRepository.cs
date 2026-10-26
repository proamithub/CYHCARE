using CYHCare.Application.DTOs;
using CYHCare.Domain.Entities;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Repository interface for Helper management (via Stored Procedures).
/// </summary>
public interface IHelperRepository
{
    /// <summary>
    /// Calls sp_Helper_GetPendingKyc to list helpers awaiting KYC approval.
    /// </summary>
    Task<IEnumerable<PendingKycHelperDto>> GetPendingKycAsync();

    /// <summary>
    /// Calls sp_Helper_ApproveKyc to approve or reject a helper KYC.
    /// </summary>
    Task<HelperProfile?> ApproveKycAsync(int helperId, string kycStatus, int? adminUserId);

    /// <summary>
    /// Calls sp_Helper_SetDuty to switch on/off duty status.
    /// </summary>
    Task<HelperProfile?> SetDutyAsync(int helperId, bool isOnDuty);

    /// <summary>
    /// Calls sp_Helper_UpdateLocation to update current GPS coordinates.
    /// </summary>
    Task<HelperProfile?> UpdateLocationAsync(int helperId, decimal lat, decimal lng);
}
