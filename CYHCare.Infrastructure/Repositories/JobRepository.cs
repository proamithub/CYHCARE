using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;
using CYHCare.Application.DTOs;
using CYHCare.Domain.Entities;

namespace CYHCare.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of IJobRepository executing Stored Procedures only.
/// </summary>
public class JobRepository : IJobRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<JobRepository> _logger;

    public JobRepository(IDbConnectionFactory connectionFactory, ILogger<JobRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Job?> CreateAsync(int requesterId, string serviceType, string address, decimal lat, decimal lng, DateTime scheduledAt)
    {
        _logger.LogInformation("Calling sp_Job_Create for requesterId: {RequesterId}", requesterId);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@RequesterId", requesterId);
        parameters.Add("@ServiceType", serviceType);
        parameters.Add("@Address", address);
        parameters.Add("@Lat", lat);
        parameters.Add("@Lng", lng);
        parameters.Add("@ScheduledAt", scheduledAt);

        return await connection.QuerySingleOrDefaultAsync<Job>(
            "sp_Job_Create",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<NearbyHelperDto>> GetNearbyHelpersAsync(decimal jobLat, decimal jobLng, decimal radiusKm = 5.0m)
    {
        _logger.LogInformation("Calling sp_Job_GetNearbyHelpers for Lat: {Lat}, Lng: {Lng}, Radius: {Radius}km", jobLat, jobLng, radiusKm);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@JobLat", jobLat);
        parameters.Add("@JobLng", jobLng);
        parameters.Add("@RadiusKm", radiusKm);

        return await connection.QueryAsync<NearbyHelperDto>(
            "sp_Job_GetNearbyHelpers",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<AcceptJobResponseDto?> AcceptAsync(int jobId, int helperId)
    {
        _logger.LogInformation("Calling sp_Job_Accept for JobId: {JobId}, HelperId: {HelperId}", jobId, helperId);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@JobId", jobId);
        parameters.Add("@HelperId", helperId);

        return await connection.QuerySingleOrDefaultAsync<AcceptJobResponseDto>(
            "sp_Job_Accept",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<JobResponseDto?> UpdateStatusAsync(int jobId, string newStatus, int? helperId = null)
    {
        _logger.LogInformation("Calling sp_Job_UpdateStatus for JobId: {JobId}, NewStatus: {NewStatus}", jobId, newStatus);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@JobId", jobId);
        parameters.Add("@NewStatus", newStatus);
        parameters.Add("@HelperId", helperId);

        return await connection.QuerySingleOrDefaultAsync<JobResponseDto>(
            "sp_Job_UpdateStatus",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<CompleteJobResponseDto?> CompleteAsync(int jobId, int helperId)
    {
        _logger.LogInformation("Calling sp_Job_Complete for JobId: {JobId}, HelperId: {HelperId}", jobId, helperId);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@JobId", jobId);
        parameters.Add("@HelperId", helperId);

        return await connection.QuerySingleOrDefaultAsync<CompleteJobResponseDto>(
            "sp_Job_Complete",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<JobResponseDto>> GetByUserAsync(int requesterId)
    {
        _logger.LogInformation("Calling sp_Job_GetByUser for RequesterId: {RequesterId}", requesterId);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@RequesterId", requesterId);

        return await connection.QueryAsync<JobResponseDto>(
            "sp_Job_GetByUser",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IEnumerable<JobResponseDto>> GetByHelperAsync(int helperId)
    {
        _logger.LogInformation("Calling sp_Job_GetByHelper for HelperId: {HelperId}", helperId);

        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@HelperId", helperId);

        return await connection.QueryAsync<JobResponseDto>(
            "sp_Job_GetByHelper",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
