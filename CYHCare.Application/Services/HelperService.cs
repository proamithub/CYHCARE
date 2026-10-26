using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.DTOs;

namespace CYHCare.Application.Services;

/// <summary>
/// Handles Helper profile operations: duty toggling, live coordinates, and KYC approval workflows.
/// </summary>
public class HelperService : IHelperService
{
    private readonly IHelperRepository _helperRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILogger<HelperService> _logger;

    public HelperService(
        IHelperRepository helperRepository,
        INotificationRepository notificationRepository,
        ILogger<HelperService> logger)
    {
        _helperRepository = helperRepository;
        _notificationRepository = notificationRepository;
        _logger = logger;
    }

    /// <summary>
    /// Switches on/off duty for an approved helper.
    /// </summary>
    public async Task<HelperProfileDto> SetDutyAsync(int helperId, bool isOnDuty)
    {
        _logger.LogInformation("Setting duty for Helper {HelperId} to {IsOnDuty}", helperId, isOnDuty);

        var profile = await _helperRepository.SetDutyAsync(helperId, isOnDuty);
        if (profile == null)
            throw new InvalidOperationException("Failed to update helper duty status.");

        return new HelperProfileDto
        {
            HelperId = profile.HelperId,
            UserId = profile.UserId,
            KycStatus = profile.KycStatus,
            IsOnDuty = profile.IsOnDuty,
            LocationSharing = profile.LocationSharing,
            LastLat = profile.LastLat,
            LastLng = profile.LastLng,
            LastUpdate = profile.LastUpdate,
            HourlyRate = profile.HourlyRate,
            Rating = profile.Rating
        };
    }

    /// <summary>
    /// Updates live location coordinates for helper.
    /// </summary>
    public async Task<HelperProfileDto> UpdateLocationAsync(int helperId, decimal lat, decimal lng)
    {
        var profile = await _helperRepository.UpdateLocationAsync(helperId, lat, lng);
        if (profile == null)
            throw new InvalidOperationException("Failed to update helper location.");

        return new HelperProfileDto
        {
            HelperId = profile.HelperId,
            UserId = profile.UserId,
            KycStatus = profile.KycStatus,
            IsOnDuty = profile.IsOnDuty,
            LocationSharing = profile.LocationSharing,
            LastLat = profile.LastLat,
            LastLng = profile.LastLng,
            LastUpdate = profile.LastUpdate,
            HourlyRate = profile.HourlyRate,
            Rating = profile.Rating
        };
    }

    /// <summary>
    /// Admin view of all helpers pending KYC approval.
    /// </summary>
    public async Task<IEnumerable<PendingKycHelperDto>> GetPendingKycAsync()
    {
        return await _helperRepository.GetPendingKycAsync();
    }

    /// <summary>
    /// Admin approval or rejection of helper KYC.
    /// </summary>
    public async Task<HelperProfileDto> ApproveKycAsync(int helperId, string kycStatus, int? adminUserId)
    {
        _logger.LogInformation("Admin {AdminUserId} updating KYC for Helper {HelperId} to {Status}", adminUserId, helperId, kycStatus);

        var profile = await _helperRepository.ApproveKycAsync(helperId, kycStatus, adminUserId);
        if (profile == null)
            throw new InvalidOperationException("Failed to update KYC status.");

        // Notify helper
        try
        {
            await _notificationRepository.CreateAsync(
                profile.UserId,
                null,
                $"Your Helper KYC status has been updated to '{kycStatus}'."
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send KYC notification to user {UserId}", profile.UserId);
        }

        return new HelperProfileDto
        {
            HelperId = profile.HelperId,
            UserId = profile.UserId,
            KycStatus = profile.KycStatus,
            IsOnDuty = profile.IsOnDuty,
            LocationSharing = profile.LocationSharing,
            HourlyRate = profile.HourlyRate,
            Rating = profile.Rating
        };
    }
}
