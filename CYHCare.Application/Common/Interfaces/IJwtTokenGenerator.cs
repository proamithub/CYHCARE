using CYHCare.Domain.Entities;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Service interface for generating JSON Web Tokens (JWT).
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a signed JWT access token for an authenticated user.
    /// </summary>
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
