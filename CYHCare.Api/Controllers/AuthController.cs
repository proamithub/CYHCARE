using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.Common.Models;
using CYHCare.Application.DTOs;

namespace CYHCare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new User or Helper.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] UserRegisterRequestDto request)
    {
        _logger.LogInformation("Registration request received for email: {Email}, Role: {Role}", request.Email, request.Role);
        var response = await _authService.RegisterAsync(request);
        return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Registration successful."));
    }

    /// <summary>
    /// Authenticates user and returns JWT bearer token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] UserLoginRequestDto request)
    {
        _logger.LogInformation("Login request received for email: {Email}", request.Email);
        var response = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponseDto>.Ok(response, "Login successful."));
    }

    /// <summary>
    /// Returns authenticated user's profile based on claims in JWT token.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(ApiResponse<UserDto>.Fail("Invalid token claims."));
        }

        var user = await _authService.GetCurrentUserAsync(userId);
        return Ok(ApiResponse<UserDto>.Ok(user, "User profile retrieved successfully."));
    }
}
