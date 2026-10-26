using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.DTOs;
using CYHCare.Domain.Entities;

namespace CYHCare.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of IHelperRepository executing Stored Procedures only.
/// </summary>
public class HelperRepository : IHelperRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<HelperRepository> _logger;

    public HelperRepository(IDbConnectionFactory connectionFactory, ILogger<HelperRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<PendingKycHelperDto>> GetPendingKycAsync()
    {
        _logger.LogInformation("Calling sp_Helper_GetPendingKyc");

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<PendingKycHelperDto>(
            "sp_Helper_GetPendingKyc",
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<HelperProfile?> ApproveKycAsync(int helperId, string kycStatus, int? adminUserId)
    {
        _logger.LogInformation("Calling sp_Helper_ApproveKyc for HelperId: {HelperId}, Status: {KycStatus}", helperId, kycStatus);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@HelperId", helperId);
        parameters.Add("@KycStatus", kycStatus);
        parameters.Add("@AdminUserId", adminUserId);

        return await connection.QuerySingleOrDefaultAsync<HelperProfile>(
            "sp_Helper_ApproveKyc",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<HelperProfile?> SetDutyAsync(int helperId, bool isOnDuty)
    {
        _logger.LogInformation("Calling sp_Helper_SetDuty for HelperId: {HelperId}, IsOnDuty: {IsOnDuty}", helperId, isOnDuty);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@HelperId", helperId);
        parameters.Add("@IsOnDuty", isOnDuty);

        return await connection.QuerySingleOrDefaultAsync<HelperProfile>(
            "sp_Helper_SetDuty",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<HelperProfile?> SetLocationSharingAsync(int helperId, bool locationSharing)
    {
        _logger.LogInformation("Calling sp_Helper_SetLocationSharing for HelperId: {HelperId}, LocationSharing: {LocationSharing}", helperId, locationSharing);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@HelperId", helperId);
        parameters.Add("@LocationSharing", locationSharing);

        return await connection.QuerySingleOrDefaultAsync<HelperProfile>(
            "sp_Helper_SetLocationSharing",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<HelperProfile?> UpdateLocationAsync(int helperId, decimal lat, decimal lng)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@HelperId", helperId);
        parameters.Add("@Lat", lat);
        parameters.Add("@Lng", lng);

        return await connection.QuerySingleOrDefaultAsync<HelperProfile>(
            "sp_Helper_UpdateLocation",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
