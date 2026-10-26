using CYHCare.Application.DTOs;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Service interface for Helper duty, location updates, privacy controls, and admin KYC approvals.
/// </summary>
public interface IHelperService
{
    /// <summary>
    /// Switches on/off duty for an approved helper.
    /// </summary>
    Task<HelperProfileDto> SetDutyAsync(int helperId, bool isOnDuty);

    /// <summary>
    /// Toggles location sharing for privacy control.
    /// </summary>
    Task<HelperProfileDto> SetLocationSharingAsync(int helperId, bool locationSharing);

    /// <summary>
    /// Updates live location coordinates for helper.
    /// </summary>
    Task<HelperProfileDto> UpdateLocationAsync(int helperId, decimal lat, decimal lng);

    /// <summary>
    /// Admin view of all helpers pending KYC approval.
    /// </summary>
    Task<IEnumerable<PendingKycHelperDto>> GetPendingKycAsync();

    /// <summary>
    /// Admin approval or rejection of helper KYC.
    /// </summary>
    Task<HelperProfileDto> ApproveKycAsync(int helperId, string kycStatus, int? adminUserId);
}
