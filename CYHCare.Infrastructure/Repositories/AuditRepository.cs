using System.Data;
using Dapper;
using Microsoft.Extensions.Logging;
using CYHCare.Application.Common.Interfaces;

namespace CYHCare.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of IAuditRepository executing Stored Procedures only.
/// </summary>
public class AuditRepository : IAuditRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<AuditRepository> _logger;

    public AuditRepository(IDbConnectionFactory connectionFactory, ILogger<AuditRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<long> InsertAsync(int? userId, string action, string? details)
    {
        using var connection = _connectionFactory.CreateConnection();
        var parameters = new DynamicParameters();
        parameters.Add("@UserId", userId);
        parameters.Add("@Action", action);
        parameters.Add("@Details", details);

        return await connection.ExecuteScalarAsync<long>(
            "sp_Audit_Insert",
            parameters,
            commandType: CommandType.StoredProcedure
        );
    }
}
