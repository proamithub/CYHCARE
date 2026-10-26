using CYHCare.Domain.Entities;

namespace CYHCare.Application.Common.Interfaces;

/// <summary>
/// Repository interface for User management (via Stored Procedures).
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Calls sp_User_Register to register a new User (and HelperProfile if role is Helper).
    /// </summary>
    Task<User?> RegisterAsync(string fullName, string phone, string email, string passwordHash, string role, decimal hourlyRate = 100.00m);

    /// <summary>
    /// Calls sp_User_GetByEmail to fetch user authentication details.
    /// </summary>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Calls sp_User_GetById to fetch user profile details.
    /// </summary>
    Task<User?> GetByIdAsync(int userId);
}
