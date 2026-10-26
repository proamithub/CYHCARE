namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Service interface for hashing and verifying passwords securely using BCrypt.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes the raw password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies the plain password against the BCrypt hash.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
