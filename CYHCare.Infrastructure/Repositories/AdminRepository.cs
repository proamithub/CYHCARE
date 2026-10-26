using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.DTOs;

namespace CYHCare.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of IAdminRepository executing Stored Procedures only.
/// </summary>
public class AdminRepository : IAdminRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<AdminRepository> _logger;

    public AdminRepository(IDbConnectionFactory connectionFactory, ILogger<AdminRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<AdminLiveSummaryDto?> GetLiveSummaryAsync()
    {
        _logger.LogInformation("Calling sp_Admin_LiveSummary");

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<AdminLiveSummaryDto>(
            "sp_Admin_LiveSummary",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<AdminActiveJobDto>> GetActiveJobsAsync()
    {
        _logger.LogInformation("Calling sp_Admin_ActiveJobs");

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<AdminActiveJobDto>(
            "sp_Admin_ActiveJobs",
            commandType: CommandType.StoredProcedure
        );
    }
}
