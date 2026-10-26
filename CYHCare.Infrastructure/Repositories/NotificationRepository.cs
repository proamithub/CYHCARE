using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;

namespace CYHCare.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of INotificationRepository executing Stored Procedures only.
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<NotificationRepository> _logger;

    public NotificationRepository(IDbConnectionFactory connectionFactory, ILogger<NotificationRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> CreateAsync(int userId, int? jobId, string message)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@JobId", jobId);
        parameters.Add("@Message", message);

        return await connection.ExecuteScalarAsync<int>(
            "sp_Notification_Create",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
