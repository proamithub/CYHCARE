using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.Common.Models;
using CYHCare.Application.DTOs;

namespace CYHCare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HelpersController : ControllerBase
{
    private readonly IHelperService _helperService;
    private readonly ILogger<HelpersController> _logger;

    public HelpersController(IHelperService helperService, ILogger<HelpersController> logger)
    {
        _helperService = helperService;
        _logger = logger;
    }

    /// <summary>
    /// Switches on/off duty status for approved helper (Helper only).
    /// </summary>
    [HttpPut("duty")]
    [Authorize(Roles = "Helper")]
    [ProducesResponseType(typeof(ApiResponse<HelperProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HelperProfileDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetDuty([FromBody] bool isOnDuty)
    {
        var helperId = GetCurrentHelperId();
        var result = await _helperService.SetDutyAsync(helperId, isOnDuty);
        return Ok(ApiResponse<HelperProfileDto>.Ok(result, $"Duty status set to {(isOnDuty ? "ON" : "OFF")}."));
    }

    /// <summary>
    /// Toggles location sharing privacy setting for helper (Helper only).
    /// </summary>
    [HttpPut("location-sharing")]
    [Authorize(Roles = "Helper")]
    [ProducesResponseType(typeof(ApiResponse<HelperProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetLocationSharing([FromBody] bool locationSharing)
    {
        var helperId = GetCurrentHelperId();
        var result = await _helperService.SetLocationSharingAsync(helperId, locationSharing);
        return Ok(ApiResponse<HelperProfileDto>.Ok(result, $"Location sharing privacy set to {(locationSharing ? "Enabled" : "Disabled")}."));
    }

    /// <summary>
    /// Updates live GPS coordinates for helper (Helper only).
    /// </summary>
    [HttpPut("location")]
    [Authorize(Roles = "Helper")]
    [ProducesResponseType(typeof(ApiResponse<HelperProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateLocation([FromQuery] decimal lat, [FromQuery] decimal lng)
    {
        var helperId = GetCurrentHelperId();
        var result = await _helperService.UpdateLocationAsync(helperId, lat, lng);
        return Ok(ApiResponse<HelperProfileDto>.Ok(result, "Location updated successfully."));
    }

    /// <summary>
    /// Admin lists all helpers awaiting KYC verification (Admin only).
    /// </summary>
    [HttpGet("pending-kyc")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PendingKycHelperDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingKyc()
    {
        var result = await _helperService.GetPendingKycAsync();
        return Ok(ApiResponse<IEnumerable<PendingKycHelperDto>>.Ok(result));
    }

    /// <summary>
    /// Admin approves or rejects helper KYC application (Admin only).
    /// </summary>
    [HttpPut("{helperId:int}/approve-kyc")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<HelperProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HelperProfileDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveKyc(int helperId, [FromBody] ApproveKycRequestDto request)
    {
        var adminUserId = GetCurrentUserId();
        var result = await _helperService.ApproveKycAsync(helperId, request.KycStatus, adminUserId);
        return Ok(ApiResponse<HelperProfileDto>.Ok(result, $"Helper KYC updated to {request.KycStatus}."));
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;
        if (int.TryParse(claim, out var id)) return id;
        throw new UnauthorizedAccessException("Invalid user identity in token.");
    }

    private int GetCurrentHelperId()
    {
        var claim = User.FindFirst("HelperId")?.Value;
        if (int.TryParse(claim, out var id)) return id;
        throw new UnauthorizedAccessException("Helper profile ID not associated with current account.");
    }
}
