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
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ILogger<JobsController> _logger;

    public JobsController(IJobService jobService, ILogger<JobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new assistance job request (User/Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "User,Admin")]
    [ProducesResponseType(typeof(ApiResponse<JobResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<JobResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequestDto request)
    {
        var userId = GetCurrentUserId();
        var result = await _jobService.CreateJobAsync(userId, request);
        return Ok(ApiResponse<JobResponseDto>.Ok(result, "Job created successfully and nearby helpers notified."));
    }

    /// <summary>
    /// Retrieves job details by Job ID.
    /// </summary>
    [HttpGet("{jobId:int}")]
    [ProducesResponseType(typeof(ApiResponse<JobResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobById(int jobId)
    {
        var job = await _jobService.GetJobByIdAsync(jobId);
        return Ok(ApiResponse<JobResponseDto>.Ok(job));
    }

    /// <summary>
    /// Searches for nearby active approved helpers within radius (Helper/Admin only).
    /// </summary>
    [HttpGet("nearby")]
    [Authorize(Roles = "Helper,Admin")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<NearbyHelperDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNearbyHelpers([FromQuery] decimal lat, [FromQuery] decimal lng, [FromQuery] decimal radiusKm = 5.0m)
    {
        var helpers = await _jobService.GetNearbyHelpersAsync(lat, lng, radiusKm);
        return Ok(ApiResponse<IEnumerable<NearbyHelperDto>>.Ok(helpers));
    }

    /// <summary>
    /// Race-safely accepts a pending job (Helper only).
    /// </summary>
    [HttpPost("{jobId:int}/accept")]
    [Authorize(Roles = "Helper")]
    [ProducesResponseType(typeof(ApiResponse<AcceptJobResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AcceptJobResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptJob(int jobId)
    {
        var helperId = GetCurrentHelperId();
        var result = await _jobService.AcceptJobAsync(jobId, helperId);
        return Ok(ApiResponse<AcceptJobResponseDto>.Ok(result, "Job accepted successfully."));
    }

    /// <summary>
    /// Transitions job status (OnTheWay, Arrived with 100m geofence validation, InProgress).
    /// </summary>
    [HttpPut("{jobId:int}/status")]
    [Authorize(Roles = "Helper")]
    [ProducesResponseType(typeof(ApiResponse<JobResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<JobResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateJobStatus(int jobId, [FromBody] UpdateJobStatusRequestDto request)
    {
        var helperId = GetCurrentHelperId();
        var result = await _jobService.UpdateJobStatusAsync(jobId, helperId, request);
        return Ok(ApiResponse<JobResponseDto>.Ok(result, $"Job status changed to {request.NewStatus}."));
    }

    /// <summary>
    /// Completes the job, calculates working minutes and generates payment (Helper only).
    /// </summary>
    [HttpPost("{jobId:int}/complete")]
    [Authorize(Roles = "Helper")]
    [ProducesResponseType(typeof(ApiResponse<CompleteJobResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CompleteJobResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteJob(int jobId)
    {
        var helperId = GetCurrentHelperId();
        var result = await _jobService.CompleteJobAsync(jobId, helperId);
        return Ok(ApiResponse<CompleteJobResponseDto>.Ok(result, "Job completed and payment calculated successfully."));
    }

    /// <summary>
    /// Retrieves jobs related to the current authenticated caller (User or Helper).
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<JobResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyJobs()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("Role")?.Value ?? "User";

        if (role.Equals("Helper", StringComparison.OrdinalIgnoreCase))
        {
            var helperId = GetCurrentHelperId();
            var jobs = await _jobService.GetJobsByHelperAsync(helperId);
            return Ok(ApiResponse<IEnumerable<JobResponseDto>>.Ok(jobs));
        }
        else
        {
            var userId = GetCurrentUserId();
            var jobs = await _jobService.GetJobsByUserAsync(userId);
            return Ok(ApiResponse<IEnumerable<JobResponseDto>>.Ok(jobs));
        }
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
