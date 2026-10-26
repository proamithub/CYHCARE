using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Domain.Entities;

namespace CYHCare.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of IUserRepository executing Stored Procedures only.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(IDbConnectionFactory connectionFactory, ILogger<UserRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<User?> RegisterAsync(string fullName, string phone, string email, string passwordHash, string role, decimal hourlyRate = 100)
    {
        _logger.LogInformation("Calling sp_User_Register for email: {Email}, role: {Role}", email, role);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@FullName", fullName);
        parameters.Add("@Phone", phone);
        parameters.Add("@Email", email);
        parameters.Add("@PasswordHash", passwordHash);
        parameters.Add("@Role", role);
        parameters.Add("@HourlyRate", hourlyRate);

        return await connection.QuerySingleOrDefaultAsync<User>(
            "sp_User_Register",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        _logger.LogInformation("Calling sp_User_GetByEmail for email: {Email}", email);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Email", email);

        return await connection.QuerySingleOrDefaultAsync<User>(
            "sp_User_GetByEmail",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        _logger.LogInformation("Calling sp_User_GetById for userId: {UserId}", userId);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);

        return await connection.QuerySingleOrDefaultAsync<User>(
            "sp_User_GetById",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
