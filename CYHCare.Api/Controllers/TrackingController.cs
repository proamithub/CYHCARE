using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.Common.Models;
using CYHCare.Application.DTOs;

namespace CYHCare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrackingController : ControllerBase
{
    private readonly IGpsRepository _gpsRepository;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(IGpsRepository gpsRepository, ILogger<TrackingController> logger)
    {
        _gpsRepository = gpsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves historical GPS route points for an assignment (ordered chronologically).
    /// </summary>
    [HttpGet("{assignmentId:int}/route")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<GpsPointDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoute(int assignmentId)
    {
        _logger.LogInformation("Fetching route history for Assignment {AssignmentId}", assignmentId);
        var route = await _gpsRepository.GetRouteAsync(assignmentId);
        return Ok(ApiResponse<IEnumerable<GpsPointDto>>.Ok(route));
    }

    /// <summary>
    /// Fallback REST endpoint for recording GPS location.
    /// </summary>
    [HttpPost("location")]
    [Authorize(Roles = "Helper")]
    [ProducesResponseType(typeof(ApiResponse<long>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LogLocation([FromBody] InsertGpsLogRequestDto request)
    {
        var logId = await _gpsRepository.InsertLogAsync(request.AssignmentId, request.Lat, request.Lng, request.SpeedKmph);
        return Ok(ApiResponse<long>.Ok(logId, "GPS waypoint logged successfully."));
    }
}
