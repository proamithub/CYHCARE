using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.DTOs;

namespace CYHCare.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of IGpsRepository executing Stored Procedures only.
/// </summary>
public class GpsRepository : IGpsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<GpsRepository> _logger;

    public GpsRepository(IDbConnectionFactory connectionFactory, ILogger<GpsRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<long> InsertLogAsync(int assignmentId, decimal lat, decimal lng, decimal? speedKmph = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@AssignmentId", assignmentId);
        parameters.Add("@Lat", lat);
        parameters.Add("@Lng", lng);
        parameters.Add("@SpeedKmph", speedKmph);

        return await connection.ExecuteScalarAsync<long>(
            "sp_Gps_Insert",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<GpsPointDto>> GetRouteAsync(int assignmentId)
    {
        _logger.LogInformation("Calling sp_Gps_GetRoute for AssignmentId: {AssignmentId}", assignmentId);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@AssignmentId", assignmentId);

        return await connection.QueryAsync<GpsPointDto>(
            "sp_Gps_GetRoute",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<int> PurgeOldAsync(int days = 30)
    {
        _logger.LogInformation("Calling sp_Gps_PurgeOld for Days: {Days}", days);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@Days", days);

        return await connection.ExecuteScalarAsync<int>(
            "sp_Gps_PurgeOld",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
