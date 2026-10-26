using CYHCare.Application.DTOs;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Service interface for authentication and registration business logic.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Registers a new user or helper.
    /// </summary>
    Task<AuthResponseDto> RegisterAsync(UserRegisterRequestDto request);

    /// <summary>
    /// Authenticates user credentials and returns a signed JWT token.
    /// </summary>
    Task<AuthResponseDto> LoginAsync(UserLoginRequestDto request);

    /// <summary>
    /// Gets current authenticated user details by user ID.
    /// </summary>
    Task<UserDto> GetCurrentUserAsync(int userId);
}
